using System;
using System.Collections.Generic;
using System.Data;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemorySearch
{
    public required Guid MemoryNodeId { get; init; }

    public required string MemoryContent { get; init; }

    public string? MemoryContext { get; init; }
}

public static partial class DbConnectionExtensions
{
    public static IReadOnlyList<MemorySearch> GetMemorySearches(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select memory_node_id, memory_content, memory_context
            from memory_search
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<MemorySearch>(sql, param).AsList();
    }
}
