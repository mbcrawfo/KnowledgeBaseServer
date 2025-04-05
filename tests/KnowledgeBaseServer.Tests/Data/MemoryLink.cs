using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemoryLink
{
    public required Guid FromMemoryId { get; init; }

    public required Guid ToMemoryId { get; init; }

    public required DateTimeOffset Created { get; init; }

    public static Faker<MemoryLink> Faker() =>
        new Faker<MemoryLink>().RuleFor(x => x.Created, f => f.Date.PastOffset());
}

public static partial class FakerExtensions
{
    public static Faker<MemoryLink> MemoryLinkFaker() =>
        new Faker<MemoryLink>()
            .RuleFor(x => x.FromMemoryId, Guid.CreateVersion7)
            .RuleFor(x => x.ToMemoryId, Guid.CreateVersion7)
            .RuleFor(x => x.Created, f => f.Date.PastOffset());

    public static Faker<MemoryLink> WithMemories(this Faker<MemoryLink> faker, Memory from, Memory to) =>
        faker.RuleFor(x => x.FromMemoryId, from.Id).RuleFor(x => x.ToMemoryId, to.Id);
}

public static partial class DbConnectionExtensions
{
    public static void SeedMemoryLinks(this IDbConnection connection, IEnumerable<MemoryLink> memoryLinks)
    {
        connection.Execute(
            """
            insert into memory_links (from_memory_id, to_memory_id, created)
            values (@FromMemoryId, @ToMemoryId, @Created)
            """,
            memoryLinks
        );
    }

    public static void SeedMemoryLink(this IDbConnection connection, MemoryLink memoryLink) =>
        connection.SeedMemoryLinks([memoryLink]);

    public static IReadOnlyList<MemoryLink> GetMemoryLinks(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select from_memory_id, to_memory_id, created
            from memory_links
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<MemoryLink>(sql, param).AsList();
    }
}
