using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Dapper;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class CreateMemoriesTool
{
    [McpServerTool(
        Name = "CreateMemories",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false
    )]
    [Description("Create new memories in the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        JsonSerializerOptions jsonSerializerOptions,
        [Description("The topic to use for the memories.")] string topic,
        [Description("The text of the memories.")] string[] memories,
        [Description("Optional information to provide context for these memories.")] string? context = null
    )
    {
        var now = DateTimeOffset.UtcNow;
        using var connection = connectionString.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var topicId = connection.QuerySingleOrDefault<Guid>(
            sql: """
            select id
            from topics
            where name = @Name
            """,
            new { Name = topic }
        );
        if (topicId == Guid.Empty)
        {
            topicId = Guid.CreateVersion7();
            connection.Execute(
                sql: """
                insert into topics (id, created, name) values
                (@Id, @Created, @Name)
                """,
                new
                {
                    Id = topicId,
                    Created = now,
                    Name = topic,
                },
                transaction
            );
        }

        Guid? contextId = null;
        if (!string.IsNullOrEmpty(context))
        {
            contextId = Guid.CreateVersion7();
            connection.Execute(
                sql: """
                insert into memory_contexts (id, created, value) values
                (@Id, @Created, @Value)
                """,
                new
                {
                    Id = contextId,
                    Created = now,
                    Value = context,
                },
                transaction
            );
        }

        var createdMemories = memories
            .Select(m => new
            {
                Id = Guid.CreateVersion7(),
                Created = now,
                TopicId = topicId,
                ContextId = contextId,
                Content = m,
            })
            .ToArray();
        connection.Execute(
            sql: """
            insert into memories (id, created, topic_id, context_id, content) values
            (@Id, @Created, @TopicId, @ContextId, @Content)
            """,
            createdMemories,
            transaction
        );

        connection.Execute(
            sql: """
            insert into memory_search (memory_id, content, context) values
            (@MemoryId, @Content, @Context)
            """,
            createdMemories.Select(m => new
            {
                MemoryId = m.Id,
                m.Content,
                Context = context,
            }),
            transaction
        );

        transaction.Commit();

        return JsonSerializer.Serialize(
            createdMemories.Select(m => new CreatedMemoryDto(m.Id, m.Content)).ToArray(),
            jsonSerializerOptions
        );
    }
}
