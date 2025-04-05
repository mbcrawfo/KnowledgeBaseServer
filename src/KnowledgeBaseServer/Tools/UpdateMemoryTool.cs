using System;
using System.ComponentModel;
using System.Text.Json;
using Dapper;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class UpdateMemoryTool
{
    [McpServerTool(Name = "UpdateMemory", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Updates the knowledge base by replacing an existing memory with a new one.")]
    public static string UpdateMemory(
        [Description("The id of the memory to replace.")] Guid previousMemoryId,
        [Description("The text of the updated memory.")] string newMemory,
        [Description("Context information for the new memory.")] string context,
        ConnectionString connectionString,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        using var connection = connectionString.CreateConnection();

        var existingMemory = connection.QuerySingleOrDefault<ExistingMemory>(
            sql: """
            select topic_id, replaced_by_memory_id
            from memories
            where id = @Id
            """,
            new { Id = previousMemoryId }
        );

        if (existingMemory is null)
        {
            return "Previous memory not found.";
        }

        if (existingMemory.ReplacedByMemoryId is not null)
        {
            return $"Previous memory has already been replaced by memory {existingMemory.ReplacedByMemoryId}.";
        }

        var now = DateTimeOffset.UtcNow;
        var transaction = connection.BeginTransaction();

        var memoryContext = new
        {
            Id = Guid.CreateVersion7(),
            Created = now,
            Value = context,
        };
        connection.Execute(
            sql: """
            insert into memory_contexts (id, created, value) values
            (@Id, @Created, @Value)
            """,
            memoryContext,
            transaction
        );

        var createdMemory = new
        {
            Id = Guid.CreateVersion7(),
            Created = now,
            existingMemory.TopicId,
            ContextId = memoryContext.Id,
            Content = newMemory,
        };
        connection.Execute(
            sql: """
            insert into memories (id, created, topic_id, context_id, content) values
            (@Id, @Created, @TopicId, @ContextId, @Content)
            """,
            createdMemory,
            transaction
        );

        connection.Execute(
            sql: """
            insert into memory_search (memory_id, content, context) values
            (@MemoryId, @Content, @Context)
            """,
            new
            {
                MemoryId = createdMemory.Id,
                createdMemory.Content,
                Context = context,
            },
            transaction
        );

        connection.Execute(
            sql: """
            update memories
            set replaced_by_memory_id = @ReplacedByMemoryId
            where id = @Id
            """,
            new { ReplacedByMemoryId = createdMemory.Id, Id = previousMemoryId },
            transaction
        );

        transaction.Commit();

        return JsonSerializer.Serialize(
            new CreatedMemoryDto(createdMemory.Id, createdMemory.Content),
            jsonSerializerOptions
        );
    }

    private sealed record ExistingMemory(Guid TopicId, Guid? ReplacedByMemoryId);
}
