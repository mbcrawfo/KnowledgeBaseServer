using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Dapper;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class AddMemoriesTool
{
    [McpServerTool(Name = "AddMemories", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Adds memories to the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        JsonSerializerOptions jsonSerializerOptions,
        [Description("The topic to use for the memories.")] string topic,
        [Description("The text of the memories.")] string[] memories,
        [Description("Context information associated with the memories.")] string context
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

        var createdMemories = memories
            .Select(m => new
            {
                Id = Guid.CreateVersion7(),
                Created = now,
                TopicId = topicId,
                ContextId = memoryContext.Id,
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
                Context = memoryContext.Value,
            }),
            transaction
        );

        transaction.Commit();

        var response = new AddMemoriesResponseDto(
            createdMemories.Select(m => new CreatedMemoryDto(m.Id, m.Content)).ToArray()
        );
        return JsonSerializer.Serialize(response, jsonSerializerOptions);
    }
}

public sealed record AddMemoriesResponseDto(IReadOnlyCollection<CreatedMemoryDto> Memories);
