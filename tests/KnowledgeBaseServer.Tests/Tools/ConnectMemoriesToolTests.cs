using System.Diagnostics;
using System.Text.Json;
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
        result.ShouldBe("Invalid memory ids provided.");
        connection.GetMemoryLinks().ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReturnNotModifyDatabase_WhenLinkExists()
    {
        // arrange
        var memories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                _faker.Lorem.Word(),
                _faker.Lorem.Words(2)
            )
        );

        Debug.Assert(memories is { Length: 2 });
        var parentMemoryId = memories[0].Id;
        var childMemoryId = memories[1].Id;

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedMemoryLink(
                new MemoryLink
                {
                    Created = _faker.Date.PastOffset(),
                    FromMemoryId = childMemoryId,
                    ToMemoryId = parentMemoryId,
                }
            );
        }

        // act
        var result = ConnectMemoriesTool.Handle(ConnectionString, parentMemoryId, [childMemoryId]);

        // assert
        using var connection = ConnectionString.CreateConnection();
        result.ShouldContain("already linked");
        connection.GetMemoryLinks().ShouldHaveSingleItem();
    }

    [Fact]
    public void ShouldCreateLink()
    {
        // arrange
        var memories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                _faker.Lorem.Word(),
                _faker.Lorem.Words()
            )
        );

        Debug.Assert(memories is { Length: 3 });
        var parentMemoryId = memories[0].Id;
        var child1MemoryId = memories[1].Id;
        var child2MemoryId = memories[2].Id;

        // act
        var result = ConnectMemoriesTool.Handle(ConnectionString, parentMemoryId, [child1MemoryId, child2MemoryId]);

        using var connection = ConnectionString.CreateConnection();
        var actualMemoryLinks = connection.GetMemoryLinks();

        // assert
        result.ShouldBe("Memories linked successfully.");
        actualMemoryLinks.ShouldSatisfyAllConditions(
            () => actualMemoryLinks.ShouldContain(ml => ml.ToMemoryId == parentMemoryId, expectedCount: 2),
            () => actualMemoryLinks.ShouldContain(ml => ml.FromMemoryId == child1MemoryId, expectedCount: 1),
            () => actualMemoryLinks.ShouldContain(ml => ml.FromMemoryId == child2MemoryId, expectedCount: 1)
        );
    }
}
