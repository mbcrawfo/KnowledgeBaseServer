using System.Linq;
using System.Text.Json;
using Bogus;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.ToolsTests;

public class AddMemoryToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void AddMemories_ShouldCreateTopicAndMemory_WhenTopicDoesNotExist()
    {
        // arrange
        var expectedTopic = _topicFaker.Generate();
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
