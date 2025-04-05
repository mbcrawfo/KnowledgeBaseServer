using System.Linq;
using System.Text.Json;
using Bogus;
using Dapper;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class AddMemoryToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void AddMemories_ShouldCreateTopicAndMemory_WhenTopicDoesNotExist()
    {
        // arrange
        var expectedTopic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        // act
        _ = AddMemoryTool.AddMemories(
            expectedTopic,
            [expectedMemory],
            expectedContext,
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var actualTopics = connection.GetTopics();
        var actualMemories = connection.GetMemories();
        var actualContexts = connection.GetMemoryContexts();

        // assert
        actualTopics.ShouldSatisfyAllConditions(
            () => actualTopics.ShouldHaveSingleItem(),
            () => actualTopics[0].Name.ShouldBe(expectedTopic)
        );
        actualMemories.ShouldSatisfyAllConditions(
            () => actualMemories.ShouldHaveSingleItem(),
            () => actualMemories[0].Content.ShouldBe(expectedMemory)
        );
        actualContexts.ShouldSatisfyAllConditions(
            () => actualContexts.ShouldHaveSingleItem(),
            () => actualContexts[0].Value.ShouldBe(expectedContext)
        );
    }

    [Fact]
    public void AddMemories_ShouldCreateNewMemoryAndContext_WhenTopicExists()
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
        _ = AddMemoryTool.AddMemories(
            expectedTopic.Name,
            [expectedMemory],
            expectedContext,
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var actualTopics = connection.GetTopics();
        var actualMemories = connection.GetMemories();
        var actualContexts = connection.GetMemoryContexts();

        // assert
        actualTopics.ShouldSatisfyAllConditions(
            () => actualTopics.ShouldHaveSingleItem(),
            () => actualTopics[0].Name.ShouldBe(expectedTopic.Name)
        );
        actualMemories.ShouldSatisfyAllConditions(
            () => actualMemories.ShouldHaveSingleItem(),
            () => actualMemories[0].Content.ShouldBe(expectedMemory)
        );
        actualContexts.ShouldSatisfyAllConditions(
            () => actualContexts.ShouldHaveSingleItem(),
            () => actualContexts[0].Value.ShouldBe(expectedContext)
        );
    }

    [Fact]
    public void AddMemories_ShouldUpdateSearchIndex()
    {
        // arrange
        var topic = _faker.Lorem.Sentence();
        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        var searchWord = _faker.PickRandom(expectedMemory.Split(' '));

        // act
        _ = AddMemoryTool.AddMemories(
            topic,
            [expectedMemory],
            expectedContext,
            ConnectionString,
            JsonSerializerOptions.Default
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
    public void AddMemories_ShouldReturnNewMemoriesWithIds()
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
        var result = AddMemoryTool.AddMemories(
            topic.Name,
            [memories],
            context,
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var expectedMemories = connection.GetMemories().Select(m => new CreatedMemoryDto(m.Id, m.Content));
        var actualMemories = JsonSerializer.Deserialize<AddMemoriesResponseDto>(result);

        // assert
        actualMemories.ShouldNotBeNull().Memories.ShouldBe(expectedMemories);
    }
}
