using System;
using System.ComponentModel;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class ConnectMemoriesTool
{
    [McpServerTool(
        Name = "ConnectMemories",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false
    )]
    [Description("Connects memories together to help find related memories in the future.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("Id of the parent memory (the older memory).")] Guid parentMemoryId,
        [Description("Id of the child memories (the newer memories).")] Guid[] childMemories
    )
    {
        var now = DateTimeOffset.UtcNow;
        var data = childMemories
            .Select(id => new
            {
                FromMemoryId = id,
                ToMemoryId = parentMemoryId,
                Created = now,
            })
            .ToArray();

        using var connection = connectionString.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            connection.Execute(
                sql: """
                insert into memory_links (from_memory_id, to_memory_id, created) values
                (@FromMemoryId, @ToMemoryId, @Created)
                """,
                data
            );
            transaction.Commit();
        }
        // FK constraint violation
        catch (SqliteException ex) when (ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 787 })
        {
            return "Invalid memory ids provided.";
        }
        // PK constraint violation
        catch (SqliteException ex) when (ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 })
        {
            return "Some of the requested memories are already linked.";
        }

        return "Memories linked successfully.";
    }
}
