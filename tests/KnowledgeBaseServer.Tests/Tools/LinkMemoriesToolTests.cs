using Bogus;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class LinkMemoriesToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();
    private readonly Faker<MemoryContext> _memoryContextFaker = MemoryContext.Faker();
    private readonly Faker<Memory> _memoryFaker = Memory.Faker();
    private readonly Faker<MemoryLink> _memoryLinkFaker = MemoryLink.Faker();

    [Fact]
    public void LinkMemories_ShouldReturnError_WhenIdsAreNotValid()
    {
        // arrange

        // act
        var result = LinkMemoriesTool.LinkMemories(ConnectionString, _faker.Random.Guid(), _faker.Random.Guid());

        using var connection = ConnectionString.CreateConnection();
        var memoryLinks = connection.GetMemoryLinks();

        // assert
        result.ShouldBe("Invalid memory id provided.");
        memoryLinks.ShouldBeEmpty();
    }

    [Fact]
    public void LinkMemories_ShouldReturnNotModifyDatabase_WhenLinkExists()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var memories = _memoryFaker.WithTopic(topic).WithContext(context).Generate(2);
        var memoryLink = _memoryLinkFaker.WithMemories(memories[0], memories[1]).Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemories(memories);
            seedConnection.SeedMemoryLink(memoryLink);
        }

        // act
        var result = LinkMemoriesTool.LinkMemories(ConnectionString, _faker.Random.Guid(), _faker.Random.Guid());

        using var connection = ConnectionString.CreateConnection();
        var actualMemoryLinks = connection.GetMemoryLinks();

        // assert
        result.ShouldBe("Invalid memory id provided.");
        actualMemoryLinks.ShouldHaveSingleItem();
    }

    [Fact]
    public void LinkMemories_ShouldCreateLink()
    {
        // arrange

        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var memories = _memoryFaker.WithTopic(topic).WithContext(context).Generate(2);

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemories(memories);
        }

        // act
        var result = LinkMemoriesTool.LinkMemories(ConnectionString, memories[0].Id, memories[1].Id);

        using var connection = ConnectionString.CreateConnection();
        var actualMemoryLinks = connection.GetMemoryLinks();

        // assert
        result.ShouldBe("Memories linked successfully.");
        actualMemoryLinks
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                () => actualMemoryLinks[0].FromMemoryId.ShouldBe(memories[0].Id),
                () => actualMemoryLinks[0].ToMemoryId.ShouldBe(memories[1].Id)
            );
    }
}
