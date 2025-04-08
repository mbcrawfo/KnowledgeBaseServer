using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class ConnectMemoriesToolTests : DatabaseTest
{
    private readonly Faker _faker = new();

    [Fact]
    public void ShouldReturnError_WhenIdsAreNotValid()
    {
        // arrange

        // act
        var result = ConnectMemoriesTool.Handle(ConnectionString, _faker.Random.Guid(), [_faker.Random.Guid()]);

        // assert
        using var connection = ConnectionString.CreateConnection();
        result.ShouldBe("Invalid ids provided.");
        connection.GetMemoryEdges().ShouldBeEmpty();
    }

    [Fact]
    public void ShouldDoNothing_WhenEdgeExists()
    {
        // arrange
        var memories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, _faker.Lorem.Word(), _faker.Lorem.Words(2))
        );
        var sourceMemoryNodeId = memories[0].Id;
        var targetMemoryNodeId = memories[1].Id;

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedMemoryEdge(
                new MemoryEdge
                {
                    Created = _faker.Date.PastOffset(),
                    SourceMemoryNodeId = sourceMemoryNodeId,
                    TargetMemoryNodeId = targetMemoryNodeId,
                }
            );
        }

        // act
        var result = ConnectMemoriesTool.Handle(ConnectionString, sourceMemoryNodeId, [targetMemoryNodeId]);

        // assert
        using var connection = ConnectionString.CreateConnection();
        result.ShouldContain("Memories linked successfully.");
        connection.GetMemoryEdges().ShouldHaveSingleItem();
    }

    [Fact]
    public void ShouldCreateEdge()
    {
        // arrange
        var memories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, _faker.Lorem.Word(), _faker.Lorem.Words())
        );
        var sourceMemoryNodeId = memories[0].Id;
        var targetMemoryNodeId1 = memories[1].Id;
        var targetMemoryNodeId2 = memories[2].Id;

        // act
        var result = ConnectMemoriesTool.Handle(
            ConnectionString,
            sourceMemoryNodeId,
            [targetMemoryNodeId1, targetMemoryNodeId2]
        );

        using var connection = ConnectionString.CreateConnection();
        var actualMemoryLinks = connection.GetMemoryEdges();

        // assert
        result.ShouldBe("Memories linked successfully.");
        actualMemoryLinks.ShouldSatisfyAllConditions(
            () => actualMemoryLinks.ShouldContain(ml => ml.SourceMemoryNodeId == sourceMemoryNodeId, expectedCount: 2),
            () => actualMemoryLinks.ShouldContain(ml => ml.TargetMemoryNodeId == targetMemoryNodeId1, expectedCount: 1),
            () => actualMemoryLinks.ShouldContain(ml => ml.TargetMemoryNodeId == targetMemoryNodeId2, expectedCount: 1)
        );
    }
}
