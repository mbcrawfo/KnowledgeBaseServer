using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class Topic
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    public required string Name { get; init; }

    public static Faker<Topic> Faker() =>
        new Faker<Topic>()
            .RuleFor(x => x.Id, Guid.CreateVersion7)
            .RuleFor(x => x.Created, f => f.Date.PastOffset())
            .RuleFor(x => x.Name, f => f.Lorem.Sentence());
}

public static partial class DbConnectionExtensions
{
    public static void SeedTopics(this IDbConnection connection, IEnumerable<Topic> topics)
    {
        connection.Execute(
            """
            insert into topics (id, created, name)
            values (@Id, @Created, @Name)
            """,
            topics
        );
    }

    public static void SeedTopic(this IDbConnection connection, Topic topic) => connection.SeedTopics([topic]);

    public static IReadOnlyList<Topic> GetTopics(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select id, created, name
            from topics
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<Topic>(sql, param).AsList();
    }
}
