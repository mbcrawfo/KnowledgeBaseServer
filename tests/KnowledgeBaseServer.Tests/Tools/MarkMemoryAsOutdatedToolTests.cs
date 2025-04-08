using System.Linq;
using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class MarkMemoryAsOutdatedToolTests : DatabaseTest
{
    private readonly Faker _faker = new();

    [Fact]
    public void ShouldReturnError_WhenMemoryNodeIdIsInvalid()
    {
        // arrange

        // act
        var result = MarkMemoryAsOutdatedTool.Handle(ConnectionString, _faker.Random.Guid(), _faker.Lorem.Sentence());

        // assert
        result.ShouldBe("Invalid memory node ID.");
    }

    [Fact]
    public void ShouldUpdateMemoryNode()
    {
        // arrange
        var memories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, _faker.Lorem.Sentence(), [_faker.Lorem.Sentence()])
        );
        var memoryNodeId = memories[0].Id;
        var expectedReason = _faker.Lorem.Sentence();

        // act
        var result = MarkMemoryAsOutdatedTool.Handle(ConnectionString, memoryNodeId, expectedReason);

        // assert
        result.ShouldBe("Memory marked as outdated.");
        using var connection = ConnectionString.CreateConnection();
        var memoryNode = connection.GetMemoryNodes().ShouldHaveSingleItem();
        memoryNode.Outdated.ShouldNotBeNull();
        memoryNode.OutdatedReason.ShouldBe(expectedReason);
    }

    [Fact]
    public void ShouldNotModifyMemoryNode_WhenNodeIsAlreadyOutdated()
    {
        // arrange
        var memories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, _faker.Lorem.Sentence(), [_faker.Lorem.Sentence()])
        );
        var memoryNodeId = memories[0].Id;

        _ = MarkMemoryAsOutdatedTool.Handle(ConnectionString, memoryNodeId, _faker.Lorem.Sentence());
        MemoryNode? expectedMemoryNode;
        using (var arrangeConnection = ConnectionString.CreateConnection())
        {
            expectedMemoryNode = arrangeConnection.GetMemoryNodes().Single();
        }

        // act
        var result = MarkMemoryAsOutdatedTool.Handle(ConnectionString, memoryNodeId, _faker.Lorem.Sentence());

        // assert
        result.ShouldBe("Memory is already marked as outdated.");
        using var connection = ConnectionString.CreateConnection();
        connection.GetMemoryNodes().ShouldHaveSingleItem().ShouldBeEquivalentTo(expectedMemoryNode);
    }
}
