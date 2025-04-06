using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class Memory
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    public required Guid TopicId { get; init; }

    public Guid? ContextId { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset? Removed { get; init; }

    public string? RemovalReason { get; init; }

    public static Faker<Memory> Faker() =>
        new Faker<Memory>()
            .RuleFor(x => x.Id, Guid.CreateVersion7)
            .RuleFor(x => x.Created, f => f.Date.PastOffset())
            .RuleFor(x => x.Content, f => f.Lorem.Sentence());
}

public static partial class FakerExtensions
{
    public static Faker<Memory> WithTopic(this Faker<Memory> faker, Topic topic) =>
        faker.RuleFor(x => x.TopicId, topic.Id);

    public static Faker<Memory> WithContext(this Faker<Memory> faker, MemoryContext context) =>
        faker.RuleFor(x => x.ContextId, context.Id);
}

public static partial class DbConnectionExtensions
{
    public static void SeedMemories(this IDbConnection connection, IEnumerable<Memory> memories)
    {
        connection.Execute(
            """
            insert into memories (id, created, topic_id, context_id, content, removed, removal_reason)
            values (@Id, @Created, @TopicId, @ContextId, @Content, @Removed, @RemovalReason)
            """,
            memories
        );
    }

    public static void SeedMemory(this IDbConnection connection, Memory memory) => connection.SeedMemories([memory]);

    public static IReadOnlyList<Memory> GetMemories(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select id, created, topic_id, context_id, content, removed, removal_reason
            from memories
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<Memory>(sql, param).AsList();
    }
}
