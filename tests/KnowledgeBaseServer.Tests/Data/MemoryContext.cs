using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemoryContext
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    public required string Value { get; init; }

    public static Faker<MemoryContext> Faker() =>
        new Faker<MemoryContext>()
            .RuleFor(x => x.Id, Guid.CreateVersion7)
            .RuleFor(x => x.Created, f => f.Date.PastOffset())
            .RuleFor(x => x.Value, f => f.Lorem.Sentence());
}

public static partial class DbConnectionExtensions
{
    public static void SeedMemoryContexts(this IDbConnection connection, IEnumerable<MemoryContext> contexts)
    {
        connection.Execute(
            """
            insert into memory_contexts (id, created, value)
            values (@Id, @Created, @Value)
            """,
            contexts
        );
    }

    public static void SeedMemoryContext(this IDbConnection connection, MemoryContext context) =>
        connection.SeedMemoryContexts([context]);

    public static IReadOnlyList<MemoryContext> GetMemoryContexts(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select id, created, value
            from memory_contexts
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<MemoryContext>(sql, param).AsList();
    }
}
