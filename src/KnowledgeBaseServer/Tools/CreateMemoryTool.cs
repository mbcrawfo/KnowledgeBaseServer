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

        var newMemoryNodeId = connection.QuerySingle<Guid>(
            sql: """
            insert into topics (id, created, name) values
                (@TopicId, @Now, @Topic)
            on conflict (name) do nothing;

            insert into memory_nodes (id, created, topic_id, content, context, importance) values
                (@MemoryNodeId, @Now, (select id from topics where name = @Topic), @Content, @Context, @Importance)
            returning id;

            insert into memory_search (memory_node_id, memory_content, memory_context) values
                (@MemoryNodeId, @Content, @Context);
            """,
            new
            {
                TopicId = Guid.CreateVersion7(),
                Now = now,
                Topic = topic,
                MemoryNodeId = Guid.CreateVersion7(),
                Content = memory,
                Context = context,
                Importance = importance,
            },
            transaction
        );

        if (sourceMemoryNodeId is not null)
        {
            try
            {
                _ = connection.ConnectMemoriesInternal(transaction, sourceMemoryNodeId.Value, [newMemoryNodeId], now);
            }
            catch (SqliteException ex) when (ex.IsForeignKeyConstraintViolation())
            {
                return $"Invalid {nameof(sourceMemoryNodeId)} provided.";
            }
        }

        transaction.Commit();

        return AppJsonSerializer.Serialize(new CreatedMemoryDto(newMemoryNodeId, memory));
    }
}
