using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using KnowledgeBaseServer.Tests.Data;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.DatabaseMigrationTests;

// ReSharper disable once InconsistentNaming
public class MigrationTests_V0_4_0 : MigrationTest
{
    /// <inheritdoc />
    public MigrationTests_V0_4_0(ITestOutputHelper outputHelper)
        : base("v0.4.0", outputHelper) { }

    [Fact(DisplayName = "v0.4.0 database should migrate to latest version")]
    public void ShouldMigrateToLatest()
    {
        // arrange
        // Query data in the original v0.4.0 format.
        List<TopicV040> originalTopics;
        List<MemoryNodeV040> originalMemoryNodes;
        List<MemoryEdgeV040> originalMemoryEdges;
        List<MemorySearchV040> originalMemorySearches;

        using (var arrangeConnection = ConnectionString.CreateConnection())
        {
            originalTopics = arrangeConnection.Query<TopicV040>("select * from topics").AsList();
            originalMemoryNodes = arrangeConnection.Query<MemoryNodeV040>("select * from memory_nodes").AsList();
            originalMemoryEdges = arrangeConnection.Query<MemoryEdgeV040>("select * from memory_edges").AsList();
            originalMemorySearches = arrangeConnection.Query<MemorySearchV040>("select * from memory_search").AsList();
        }

        // act
        var result = Migrator.ApplyMigrations(LogFactory, ConnectionString);

        // assert
        result.ShouldBeTrue();

        originalTopics.ShouldNotBeEmpty();
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

        var migratedMemoryNodes = connection.GetMemoryNodes();
        foreach (var originalMemoryNode in originalMemoryNodes)
        {
            migratedMemoryNodes
                .FirstOrDefault(x => x.Id == originalMemoryNode.Id)
                .ShouldNotBeNull()
                .ShouldSatisfyAllConditions(
                    x => x.Created.ShouldBe(originalMemoryNode.Created),
                    x => x.TopicId.ShouldBe(originalMemoryNode.TopicId),
                    x => x.Content.ShouldBe(originalMemoryNode.Content),
                    x => x.Context.ShouldBe(originalMemoryNode.Context),
                    x => x.Importance.ShouldBe(originalMemoryNode.Importance),
                    x => x.Outdated.ShouldBe(originalMemoryNode.Outdated),
                    x => x.OutdatedReason.ShouldBe(originalMemoryNode.OutdatedReason)
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

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed

    private sealed record TopicV040(Guid Id, DateTimeOffset Created, string Name);

    private sealed class MemoryNodeV040
    {
        public Guid Id { get; init; }

        public DateTimeOffset Created { get; init; }

        public Guid TopicId { get; init; }

        public required string Content { get; init; }

        public string? Context { get; init; }

        public DateTimeOffset? Outdated { get; init; }

        public string? OutdatedReason { get; init; }

        public double Importance { get; init; }
    }

    private sealed record MemoryEdgeV040(Guid SourceMemoryNodeId, Guid TargetMemoryNodeId, DateTimeOffset Created);

    private sealed class MemorySearchV040
    {
        public required Guid MemoryNodeId { get; init; }

        public required string MemoryContent { get; init; }

        public string? MemoryContext { get; init; }
    }

#pragma warning restore S1144, S3459
}
