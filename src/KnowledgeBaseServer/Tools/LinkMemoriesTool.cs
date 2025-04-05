using System;
using System.ComponentModel;
using Dapper;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class LinkMemoriesTool
{
    [McpServerTool(Name = "LinkMemories", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Links two memories together to help find related memories in the future.")]
    public static string LinkMemories(
        [Description("The id of the first memory.")] Guid fromMemoryId,
        [Description("The id of the second memory.")] Guid toMemoryId,
        ConnectionString connectionString
    )
    {
        using var connection = connectionString.CreateConnection();

        try
        {
            connection.Execute(
                sql: """
                insert into memory_links (from_memory_id, to_memory_id, created) values
                (@FromMemoryId, @ToMemoryId, @Created)
                """,
                new
                {
                    FromMemoryId = fromMemoryId,
                    ToMemoryId = toMemoryId,
                    Created = DateTimeOffset.UtcNow,
                }
            );
        }
        catch (SqliteException ex) when (ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 787 })
        {
            return "Invalid memory id provided.";
        }
        catch (SqliteException ex) when (ex is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 1555 })
        {
            return "The requested memories are already linked.";
        }

        return "Memories linked successfully.";
    }
}
