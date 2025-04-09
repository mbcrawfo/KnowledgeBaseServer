using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dapper;
using KnowledgeBaseServer.Tests.Data;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.DatabaseMigrationTests;

// ReSharper disable once InconsistentNaming
public class MigrationTests_V0_2_0 : MigrationTest
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b =>
        b.AddConsole().SetMinimumLevel(LogLevel.Trace)
    );

    /// <inheritdoc />
    public MigrationTests_V0_2_0()
        : base("v0.2.0") { }

    [Fact(DisplayName = "v0.2.0 database should migrate to latest version")]
    public void ShouldMigrateToLatest()
    {
        // arrange
        // Query data in the original v0.1.0 format.
        List<TopicV020> originalTopics;
        List<MemoryContextV020> originalMemoryContexts;
        List<MemoryNodeV020> originalMemoryNodes;
        List<MemoryEdgeV020> originalMemoryEdges;
        List<MemorySearchV020> originalMemorySearches;

        using (var arrangeConnection = ConnectionString.CreateConnection())
        {
            originalTopics = arrangeConnection.Query<TopicV020>("select * from topics").AsList();
            originalMemoryContexts = arrangeConnection
                .Query<MemoryContextV020>("select * from memory_contexts")
                .AsList();
            originalMemoryNodes = arrangeConnection.Query<MemoryNodeV020>("select * from memory_nodes").AsList();
            originalMemoryEdges = arrangeConnection.Query<MemoryEdgeV020>("select * from memory_edges").AsList();
            originalMemorySearches = arrangeConnection.Query<MemorySearchV020>("select * from memory_search").AsList();
        }

        // act
        var result = Migrator.ApplyMigrations(_loggerFactory, ConnectionString);

        // assert
        result.ShouldBeTrue();

        originalTopics.ShouldNotBeEmpty();
        originalMemoryContexts.ShouldNotBeEmpty();
        originalMemoryNodes.ShouldNotBeEmpty();
        originalMemoryEdges.ShouldNotBeEmpty();
        originalMemorySearches.ShouldNotBeEmpty();

        // Validate that the data migrated correctly.
        using var connection = ConnectionString.CreateConnection();

        var migratedTopics = connection.GetTopics();
        foreach (var originalTopic in originalTopics)
        {
            migratedTopics
                .FirstOrDefault(x => x.Id == originalTopic.Id)
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(
                    x => x.Created.ShouldBe(originalTopic.Created),
                    x => x.Name.ShouldBe(originalTopic.Name)
                );
        }

        var migratedMemoryContexts = connection.GetMemoryContexts();
        foreach (var originalMemoryContext in originalMemoryContexts)
        {
            migratedMemoryContexts
                .FirstOrDefault(x => x.Id == originalMemoryContext.Id)
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(
                    x => x.Created.ShouldBe(originalMemoryContext.Created),
                    x => x.Value.ShouldBe(originalMemoryContext.Value)
                );
        }

        var migratedMemoryNodes = connection.GetMemoryNodes();
        foreach (var originalMemoryNode in originalMemoryNodes)
        {
            migratedMemoryNodes
                .FirstOrDefault(x => x.Id == originalMemoryNode.Id)
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(
                    x => x.Created.ShouldBe(originalMemoryNode.Created),
                    x => x.TopicId.ShouldBe(originalMemoryNode.TopicId),
                    x => x.ContextId.ShouldBe(originalMemoryNode.ContextId),
                    x => x.Content.ShouldBe(originalMemoryNode.Content),
                    x => x.Outdated.ShouldBe(originalMemoryNode.Outdated),
                    x => x.OutdatedReason.ShouldBe(originalMemoryNode.OutdatedReason),
                    x => x.Importance.ShouldBe(originalMemoryNode.Importance)
                );
        }

        // Validate that the data migrated correctly for memory edges.
        var migratedMemoryEdges = connection.GetMemoryEdges();
        foreach (var originalMemoryEdge in originalMemoryEdges)
        {
            migratedMemoryEdges
                .FirstOrDefault(x =>
                    x.SourceMemoryNodeId == originalMemoryEdge.SourceMemoryNodeId
                    && x.TargetMemoryNodeId == originalMemoryEdge.TargetMemoryNodeId
                )
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(x => x.Created.ShouldBe(originalMemoryEdge.Created));
        }

        // Validate that the data migrated correctly for memory searches.
        var migratedMemorySearches = connection.GetMemorySearches();
        foreach (var originalMemorySearch in originalMemorySearches)
        {
            migratedMemorySearches
                .FirstOrDefault(x => x.MemoryNodeId == originalMemorySearch.MemoryNodeId)
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(
                    x => x.MemoryContent.ShouldBe(originalMemorySearch.MemoryContent),
                    x => x.MemoryContext.ShouldBe(originalMemorySearch.MemoryContext)
                );
        }
    }

    private sealed record TopicV020(Guid Id, DateTimeOffset Created, string Name);

    private sealed record MemoryContextV020(Guid Id, DateTimeOffset Created, string Value);

    private sealed record MemoryNodeV020(
        Guid Id,
        DateTimeOffset Created,
        Guid TopicId,
        Guid? ContextId,
        string Content,
        DateTimeOffset? Outdated,
        string? OutdatedReason,
        double Importance
    );

    private sealed record MemoryEdgeV020(Guid SourceMemoryNodeId, Guid TargetMemoryNodeId, DateTimeOffset Created);

    // For some reason Dapper can't deserialize this correctly as a record.
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed")]
    [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed")]
    private sealed class MemorySearchV020
    {
        public required Guid MemoryNodeId { get; init; }

        public required string MemoryContent { get; init; }

        public string? MemoryContext { get; init; }
    }
}
