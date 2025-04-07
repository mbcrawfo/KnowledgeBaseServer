using System;
using System.Collections.Generic;
using System.Data;
using Bogus;
using Dapper;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemoryNode
{
    public required Guid Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    public required Guid TopicId { get; init; }

    public Guid? ContextId { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset? Outdated { get; init; }

    public string? OutdatedReason { get; init; }

    public static Faker<MemoryNode> Faker() =>
        new Faker<MemoryNode>()
            .RuleFor(x => x.Id, Guid.CreateVersion7)
            .RuleFor(x => x.Created, f => f.Date.PastOffset())
            .RuleFor(x => x.Content, f => f.Lorem.Sentence());
}

public static partial class FakerExtensions
{
    public static Faker<MemoryNode> WithTopic(this Faker<MemoryNode> faker, Topic topic) =>
        faker.RuleFor(x => x.TopicId, topic.Id);

    public static Faker<MemoryNode> WithContext(this Faker<MemoryNode> faker, MemoryContext context) =>
        faker.RuleFor(x => x.ContextId, context.Id);

    public static Faker<MemoryNode> WithOutdated(this Faker<MemoryNode> faker) =>
        faker
            .RuleFor(x => x.Outdated, f => f.Date.PastOffset())
            .RuleFor(x => x.OutdatedReason, f => f.Lorem.Sentence());
}

public static partial class DbConnectionExtensions
{
    public static void SeedMemoryNodes(this IDbConnection connection, IEnumerable<MemoryNode> memoryNodes)
    {
        connection.Execute(
            """
            insert into memory_nodes (id, created, topic_id, context_id, content, outdated, outdated_reason)
            values (@Id, @Created, @TopicId, @ContextId, @Content, @Outdated, @OutdatedReason)
            """,
            memoryNodes
        );
    }

    public static void SeedMemoryNode(this IDbConnection connection, MemoryNode memoryNode) =>
        connection.SeedMemoryNodes([memoryNode]);

    public static IReadOnlyList<MemoryNode> GetMemoryNodes(
        this IDbConnection connection,
        string? where = null,
        object? param = null
    )
    {
        var sql = """
            select id, created, topic_id, context_id, content, outdated, outdated_reason
            from memory_nodes
            """;
        if (where is not null)
        {
            sql += "\n where " + where;
        }

        return connection.Query<MemoryNode>(sql, param).AsList();
    }
}
