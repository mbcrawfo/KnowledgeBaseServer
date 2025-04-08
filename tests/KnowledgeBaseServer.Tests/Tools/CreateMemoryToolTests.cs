using Bogus;
using Dapper;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Extensions;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class CreateMemoryToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ShouldReturnError_WhenImportanceIsOutOfRange(int importance)
    {
        // arrange

        // act
        var result = CreateMemoryTool.Handle(
            ConnectionString,
            _faker.Lorem.Word(),
            _faker.Lorem.Sentence(),
            importance
        );

        // assert
        result.ShouldBe("importance must be between 0 and 100.");
    }

    [Fact]
    public void ShouldCreateTopicAndMemory_WhenTopicDoesNotExist()
    {
        // arrange
        var expectedTopic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();
        var expectedImportance = _faker.Random.Int(0, 100);

        // act
        _ = CreateMemoryTool.Handle(
            ConnectionString,
            expectedTopic,
            expectedMemory,
            expectedImportance,
            expectedContext
        );

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection
            .GetMemoryNodes()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Content.ShouldBe(expectedMemory),
                x => x.Importance.ShouldBe(expectedImportance)
            );
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
        var expectedImportance = _faker.Random.Int(0, 100);

        // act
        _ = CreateMemoryTool.Handle(
            ConnectionString,
            expectedTopic.Name,
            expectedMemory,
            expectedImportance,
            expectedContext
        );

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic.Name);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection
            .GetMemoryNodes()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.Content.ShouldBe(expectedMemory),
                x => x.Importance.ShouldBe(expectedImportance)
            );
    }

    [Fact]
    public void ShouldCreateMemoryWithoutContext_WhenContextIsNull()
    {
        // arrange
        var topic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();

        // act
        _ = CreateMemoryTool.Handle(ConnectionString, topic, expectedMemory);

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
        _ = CreateMemoryTool.Handle(ConnectionString, topic, expectedMemory, context: expectedContext);

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
        var result = CreateMemoryTool.Handle(
            ConnectionString,
            _faker.Lorem.Sentence(),
            _faker.Lorem.Sentence(),
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
    public void ShouldLinkNewMemory_WhenSourceMemoryIdProvided()
    {
        // arrange
        var sourceMemoryNodeId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(
                CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Sentence(), _faker.Lorem.Sentence())
            )
            .Id;

        // act
        var targetMemoryNodeId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(
                CreateMemoryTool.Handle(
                    ConnectionString,
                    _faker.Lorem.Sentence(),
                    _faker.Lorem.Sentence(),
                    sourceMemoryNodeId: sourceMemoryNodeId
                )
            )
            .Id;

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection
            .GetMemoryEdges()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                x => x.SourceMemoryNodeId.ShouldBe(sourceMemoryNodeId),
                x => x.TargetMemoryNodeId.ShouldBe(targetMemoryNodeId)
            );
    }

    [Fact]
    public void ShouldReturnNewMemoryWithId()
    {
        // arrange
        var topic = _topicFaker.Generate();
        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
        }

        var memory = _faker.Lorem.Sentence();
        var context = _faker.Lorem.Sentence();

        // act
        var actualMemory = AppJsonSerializer.Deserialize<CreatedMemoryDto>(
            CreateMemoryTool.Handle(ConnectionString, topic.Name, memory, context: context)
        );

        // assert
        using var connection = ConnectionString.CreateConnection();
        var expectedMemoryNode = connection.GetMemoryNodes().ShouldHaveSingleItem();
        actualMemory.ShouldSatisfyAllConditions(
            m => m.Id.ShouldBe(expectedMemoryNode.Id),
            m => m.Content.ShouldBe(memory)
        );
    }
}
