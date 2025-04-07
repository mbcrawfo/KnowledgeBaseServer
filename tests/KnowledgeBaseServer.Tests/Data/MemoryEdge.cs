using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemoryEdge
{
    public required Guid SourceMemoryNodeId { get; init; }

    public required Guid TargetMemoryNodeId { get; init; }

    public required DateTimeOffset Created { get; init; }

    public static Faker<MemoryEdge> Faker() =>
        new Faker<MemoryEdge>().RuleFor(x => x.Created, f => f.Date.PastOffset());
}

public static partial class FakerExtensions
{
    public static Faker<MemoryEdge> WithNodes(this Faker<MemoryEdge> faker, MemoryNode source, MemoryNode target) =>
        faker.RuleFor(x => x.SourceMemoryNodeId, source.Id).RuleFor(x => x.TargetMemoryNodeId, target.Id);
}

public static partial class DbConnectionExtensions
{
    public static void SeedMemoryEdges(this IDbConnection connection, IEnumerable<MemoryEdge> memoryEdges)
    {
        connection.Execute(
            """
            insert into memory_edges (source_memory_node_id, target_memory_node_id, created)
            values (@SourceMemoryNodeId, @TargetMemoryNodeId, @Created)
            """,
            memoryEdges
        );
    }

    public static void SeedMemoryEdge(this IDbConnection connection, MemoryEdge memoryEdge) =>
        connection.SeedMemoryEdges([memoryEdge]);

    public static IReadOnlyList<MemoryEdge> GetMemoryEdges(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select source_memory_node_id, target_memory_node_id, created
            from memory_edges
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<MemoryEdge>(sql, param).AsList();
    }
}
