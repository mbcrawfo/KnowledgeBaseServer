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
    [Description("Connects memory nodes together to help find related memories in the future.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("Id of the source memory node (the older memory).")] Guid sourceMemoryNodeId,
        [Description("Ids of the target memory nodes (the newer memories).")] Guid[] targetMemoryNodeIds
    )
    {
        using var connection = connectionString.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            _ = connection.ConnectMemoriesInternal(transaction, sourceMemoryNodeId, targetMemoryNodeIds);
            transaction.Commit();
        }
        catch (SqliteException ex) when (ex.IsForeignKeyConstraintViolation())
        {
            return "Invalid ids provided.";
        }
        catch (SqliteException ex) when (ex.IsPrimaryKeyConstraintViolation())
        {
            return "Some of the requested memory nodes are already linked.";
        }

        return "Memories linked successfully.";
    }

    internal static int ConnectMemoriesInternal(
        this IDbConnection connection,
        IDbTransaction transaction,
        Guid sourceMemoryNodeId,
        IEnumerable<Guid> targetMemoryNodeIds,
        DateTimeOffset? now = null
    )
    {
        now ??= DateTimeOffset.UtcNow;

        var data = targetMemoryNodeIds.Select(id => new
        {
            SourceMemoryNodeId = sourceMemoryNodeId,
            TargetMemoryNodeId = id,
            Created = now,
        });

        return connection.Execute(
            sql: """
            insert into memory_edges (source_memory_node_id, target_memory_node_id, created) values
            (@SourceMemoryNodeId, @TargetMemoryNodeId, @Created)
            """,
            data,
            transaction
        );
    }
}
