using System.Linq;
using Bogus;
using Dapper;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Extensions;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class CreateMemoriesToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void ShouldCreateTopicAndMemory_WhenTopicDoesNotExist()
    {
        // arrange
        var expectedTopic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        // act
        _ = CreateMemoriesTool.Handle(ConnectionString, expectedTopic, [expectedMemory], expectedContext);

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection.GetMemoryNodes().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
    }

    [Fact]
    public void ShouldCreateNewMemoryAndContext_WhenTopicExists()
    {
        // arrange
        var expectedTopic = _topicFaker.Generate();
        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(expectedTopic);
        }

        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        // act
        _ = CreateMemoriesTool.Handle(ConnectionString, expectedTopic.Name, [expectedMemory], expectedContext);

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic.Name);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection.GetMemoryNodes().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
    }

    [Fact]
    public void ShouldCreateMemoryWithoutContext_WhenContextIsNull()
    {
        // arrange
        var topic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();

        // act
        _ = CreateMemoriesTool.Handle(ConnectionString, topic, [expectedMemory]);

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(topic);
        connection.GetMemoryContexts().ShouldBeEmpty();
        connection.GetMemoryEdges().ShouldBeEmpty();
        connection.GetMemoryNodes().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
    }

    [Fact]
    public void ShouldUpdateSearchIndex()
    {
        // arrange
        var topic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        var searchWord = _faker.PickRandom(expectedMemory.Split(' ')).RemovePunctuation();

        // act
        _ = CreateMemoriesTool.Handle(ConnectionString, topic, [expectedMemory], expectedContext);

        using var connection = ConnectionString.CreateConnection();
        var memorySearches = connection
            .Query<MemorySearch>(
                sql: """
                select memory_node_id, memory_content, memory_context
                from memory_search
                where memory_content match @Word
                """,
                new { Word = searchWord }
            )
            .AsList();

        // assert
        memorySearches
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                () => memorySearches[0].MemoryContent.ShouldBe(expectedMemory),
                () => memorySearches[0].MemoryContext.ShouldBe(expectedContext)
            );
    }

    [Fact]
    public void ShouldReturnError_WhenParentMemoryIdIsNotValid()
    {
        // arrange

        // act
        var result = CreateMemoriesTool.Handle(
            ConnectionString,
            _faker.Lorem.Sentence(),
            [_faker.Lorem.Sentence()],
            sourceMemoryNodeId: _faker.Random.Guid()
        );

        // assert
        result.ShouldBe("Invalid sourceMemoryNodeId provided.");
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldBeEmpty();
        connection.GetMemoryContexts().ShouldBeEmpty();
        connection.GetMemoryNodes().ShouldBeEmpty();
        connection.GetMemoryEdges().ShouldBeEmpty();
    }

    [Fact]
    public void ShouldLinkNewMemories_WhenParentMemoryIdProvided()
    {
        // arrange
        var sourceMemories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, _faker.Lorem.Sentence(), [_faker.Lorem.Sentence()])
        );
        var sourceMemoryNodeId = sourceMemories[0].Id;

        // act
        var targetMemories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                _faker.Lorem.Sentence(),
                [_faker.Lorem.Sentence()],
                sourceMemoryNodeId: sourceMemoryNodeId
            )
        );

        // assert
        var targetMemoryNodeId = targetMemories.ShouldNotBeNull().ShouldHaveSingleItem().Id;
        using var connection = ConnectionString.CreateConnection();
        var memoryEdge = connection.GetMemoryEdges().ShouldHaveSingleItem();
        memoryEdge.SourceMemoryNodeId.ShouldBe(sourceMemoryNodeId);
        memoryEdge.TargetMemoryNodeId.ShouldBe(targetMemoryNodeId);
    }

    [Fact]
    public void ShouldReturnNewMemoriesWithIds()
    {
        // arrange
        var topic = _topicFaker.Generate();
        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
        }

        var memories = _faker.Lorem.Sentences(3);
        var context = _faker.Lorem.Sentence();

        // act
        var actualMemories = AppJsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, topic.Name, [memories], context)
        );

        using var connection = ConnectionString.CreateConnection();
        var expectedMemories = connection.GetMemoryNodes().Select(m => new CreatedMemoryDto(m.Id, m.Content));

        // assert
        actualMemories.ShouldBe(expectedMemories);
    }
}
