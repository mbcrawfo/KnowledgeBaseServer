using System;
using System.ComponentModel;
using Dapper;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Extensions;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class CreateMemoryTool
{
    [McpServerTool(Name = "CreateMemory", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Creates a new memory in the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("The topic to use for the memory.")] string topic,
        [Description("The text of the memory.")] string memory,
        [Description("The importance of the memory, between 0 and 1.  Default: 0.5")] double importance = 0.5,
        [Description("Optional information to provide context for the memory.")] string? context = null,
        [Description("Optionally connect the new memory to an existing memory node.")] Guid? sourceMemoryNodeId = null
    )
    {
        if (importance is < 0 or > 1)
        {
            return $"{nameof(importance)} must be between 0 and 1.";
        }

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
            _ = connection.Execute(
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
            _ = connection.Execute(
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

        var createdNode = new
        {
            Id = Guid.CreateVersion7(),
            Created = now,
            TopicId = topicId,
            ContextId = contextId,
            Content = memory,
            Importance = importance,
        };
        _ = connection.Execute(
            sql: """
            insert into memory_nodes (id, created, topic_id, context_id, content, importance) values
            (@Id, @Created, @TopicId, @ContextId, @Content, @Importance)
            """,
            createdNode,
            transaction
        );

        _ = connection.Execute(
            sql: """
            insert into memory_search (memory_node_id, memory_content, memory_context) values
            (@MemoryId, @Content, @Context)
            """,
            new
            {
                MemoryId = createdNode.Id,
                Content = memory,
                Context = context,
            },
            transaction
        );

        if (sourceMemoryNodeId is not null)
        {
            try
            {
                _ = connection.ConnectMemoriesInternal(transaction, sourceMemoryNodeId.Value, [createdNode.Id], now);
            }
            catch (SqliteException ex) when (ex.IsForeignKeyConstraintViolation())
            {
                return $"Invalid {nameof(sourceMemoryNodeId)} provided.";
            }
        }

        transaction.Commit();

        return AppJsonSerializer.Serialize(new CreatedMemoryDto(createdNode.Id, createdNode.Content));
    }
}
