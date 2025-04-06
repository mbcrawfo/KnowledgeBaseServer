using System.Diagnostics;
using System.Linq;
using System.Text.Json;
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
        _ = CreateMemoriesTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            expectedTopic,
            [expectedMemory],
            expectedContext
        );

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection.GetMemories().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
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
        _ = CreateMemoriesTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            expectedTopic.Name,
            [expectedMemory],
            expectedContext
        );

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(expectedTopic.Name);
        connection.GetMemoryContexts().ShouldHaveSingleItem().Value.ShouldBe(expectedContext);
        connection.GetMemories().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
    }

    [Fact]
    public void ShouldCreateMemoryWithoutContext_WhenContextIsNull()
    {
        // arrange
        var topic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();

        // act

        _ = CreateMemoriesTool.Handle(ConnectionString, JsonSerializerOptions.Default, topic, [expectedMemory]);

        // assert
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldHaveSingleItem().Name.ShouldBe(topic);
        connection.GetMemoryContexts().ShouldBeEmpty();
        connection.GetMemoryLinks().ShouldBeEmpty();
        connection.GetMemories().ShouldHaveSingleItem().Content.ShouldBe(expectedMemory);
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
        _ = CreateMemoriesTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            topic,
            [expectedMemory],
            expectedContext
        );

        using var connection = ConnectionString.CreateConnection();
        var memorySearches = connection
            .Query<MemorySearch>(
                """
                select memory_id, content, context
                from memory_search
                where content match @Word
                """,
                new { Word = searchWord }
            )
            .AsList();

        // assert
        memorySearches
            .ShouldNotBeNull()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(
                () => memorySearches[0].Content.ShouldBe(expectedMemory),
                () => memorySearches[0].Context.ShouldBe(expectedContext)
            );
    }

    [Fact]
    public void ShouldReturnError_WhenParentMemoryIdIsNotValid()
    {
        // arrange

        // act
        var result = CreateMemoriesTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            _faker.Lorem.Sentence(),
            [_faker.Lorem.Sentence()],
            parentMemoryId: _faker.Random.Guid()
        );

        // assert
        result.ShouldBe("Invalid parentMemoryId provided.");
        using var connection = ConnectionString.CreateConnection();
        connection.GetTopics().ShouldBeEmpty();
        connection.GetMemoryContexts().ShouldBeEmpty();
        connection.GetMemories().ShouldBeEmpty();
        connection.GetMemoryLinks().ShouldBeEmpty();
    }

    [Fact]
    public void ShouldLinkNewMemories_WhenParentMemoryIdProvided()
    {
        // arrange
        var parentMemories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                _faker.Lorem.Sentence(),
                [_faker.Lorem.Sentence()]
            )
        );
        Debug.Assert(parentMemories is { Length: 1 });
        var parentMemoryId = parentMemories[0].Id;

        // act
        var childMemories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                _faker.Lorem.Sentence(),
                [_faker.Lorem.Sentence()],
                parentMemoryId: parentMemoryId
            )
        );

        // assert
        var childMemoryId = childMemories.ShouldNotBeNull().ShouldHaveSingleItem().Id;
        using var connection = ConnectionString.CreateConnection();
        var memoryLink = connection.GetMemoryLinks().ShouldHaveSingleItem();
        memoryLink.FromMemoryId.ShouldBe(childMemoryId);
        memoryLink.ToMemoryId.ShouldBe(parentMemoryId);
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
        var actualMemories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, JsonSerializerOptions.Default, topic.Name, [memories], context)
        );

        using var connection = ConnectionString.CreateConnection();
        var expectedMemories = connection.GetMemories().Select(m => new CreatedMemoryDto(m.Id, m.Content));

        // assert
        actualMemories.ShouldBe(expectedMemories);
    }
}
