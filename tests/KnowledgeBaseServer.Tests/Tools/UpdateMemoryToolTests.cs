using System.Linq;
using System.Text.Json;
using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class UpdateMemoryToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();
    private readonly Faker<MemoryContext> _memoryContextFaker = MemoryContext.Faker();
    private readonly Faker<Memory> _memoryFaker = Memory.Faker();

    [Fact]
    public void UpdateMemory_ShouldReturnError_WhenPreviousMemoryDoesNotExist()
    {
        // arrange

        // act
        var result = UpdateMemoryTool.UpdateMemory(
            _faker.Random.Guid(),
            _faker.Lorem.Sentence(),
            _faker.Lorem.Sentence(),
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var memories = connection.GetMemories();

        // assert
        result.ShouldBe("Previous memory not found.");
        memories.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateMemory_ShouldReturnError_WhenPreviousMemoryHasBeenReplaced()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var replacementMemory = _memoryFaker.WithTopic(topic).WithContext(context).Generate();
        var previousMemory = _memoryFaker
            .WithTopic(topic)
            .WithContext(context)
            .WithReplacementMemory(replacementMemory)
            .Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemories([replacementMemory, previousMemory]);
        }

        // act
        var result = UpdateMemoryTool.UpdateMemory(
            previousMemory.Id,
            _faker.Lorem.Sentence(),
            _faker.Lorem.Sentence(),
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var memories = connection.GetMemories();

        // assert
        result.ShouldStartWith("Previous memory has already been replaced");
        memories.Count.ShouldBe(2);
    }

    [Fact]
    public void UpdateMemory_ShouldAddNewMemoryAndUpdatePreviousMemory()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var previousMemory = _memoryFaker.WithTopic(topic).WithContext(context).Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemory(previousMemory);
        }

        var expectedMemory = _faker.Lorem.Sentence();
        var expectedContext = _faker.Lorem.Sentence();

        // act
        _ = UpdateMemoryTool.UpdateMemory(
            previousMemory.Id,
            expectedMemory,
            expectedContext,
            ConnectionString,
            JsonSerializerOptions.Default
        );

        using var connection = ConnectionString.CreateConnection();
        var actualMemories = connection.GetMemories();
        var actualContexts = connection.GetMemoryContexts();

        // assert
        actualMemories.ShouldSatisfyAllConditions(
            () => actualMemories.Count.ShouldBe(2),
            () => actualMemories.First(m => m.Id != previousMemory.Id).Content.ShouldBe(expectedMemory)
        );
        actualContexts.ShouldSatisfyAllConditions(
            () => actualContexts.Count.ShouldBe(2),
            () => actualContexts.First(c => c.Id != previousMemory.ContextId).Value.ShouldBe(expectedContext)
        );
    }

    [Fact]
    public void UpdateMemory_ShouldReturnNewMemory()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var previousMemory = _memoryFaker.WithTopic(topic).WithContext(context).Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemory(previousMemory);
        }

        var expectedMemory = _faker.Lorem.Sentence();

        // act
        var result = UpdateMemoryTool.UpdateMemory(
            previousMemory.Id,
            expectedMemory,
            context.Value,
            ConnectionString,
            JsonSerializerOptions.Default
        );

        var actual = JsonSerializer.Deserialize<CreatedMemoryDto>(result);

        // assert
        actual.ShouldNotBeNull().Content.ShouldBe(expectedMemory);
    }
}
