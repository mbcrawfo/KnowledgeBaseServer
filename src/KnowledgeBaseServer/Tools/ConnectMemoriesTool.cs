using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using Dapper;
using KnowledgeBaseServer.Extensions;
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
        [Description("Id of the child memories (the newer memories).")] Guid[] childMemoryIds
    )
    {
        using var connection = connectionString.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            _ = connection.ConnectMemoriesInternal(transaction, parentMemoryId, childMemoryIds);
            transaction.Commit();
        }
        catch (SqliteException ex) when (ex.IsForeignKeyConstraintViolation())
        {
            return "Invalid memory ids provided.";
        }
        catch (SqliteException ex) when (ex.IsPrimaryKeyConstraintViolation())
        {
            return "Some of the requested memories are already linked.";
        }

        return "Memories linked successfully.";
    }

    internal static int ConnectMemoriesInternal(
        this IDbConnection connection,
        IDbTransaction transaction,
        Guid parentMemoryId,
        IEnumerable<Guid> childMemoryIds,
        DateTimeOffset? now = null
    )
    {
        now ??= DateTimeOffset.UtcNow;

        var data = childMemoryIds.Select(id => new
        {
            FromMemoryId = id,
            ToMemoryId = parentMemoryId,
            Created = now,
        });

        return connection.Execute(
            sql: """
            insert into memory_links (from_memory_id, to_memory_id, created) values
            (@FromMemoryId, @ToMemoryId, @Created)
            """,
            data,
            transaction
        );
    }
}
