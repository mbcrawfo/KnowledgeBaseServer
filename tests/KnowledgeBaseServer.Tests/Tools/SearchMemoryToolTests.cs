using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class SearchMemoryToolTests : DatabaseTest
{
    private readonly Faker _faker = new();

    [Fact]
    public void ShouldReturnError_WhenTooManyPhrases()
    {
        // arrange
        var phrases = _faker.Lorem.Words(10);

        // act
        var result = SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, phrases);

        // assert
        result.ShouldStartWith("Error: Too many phrases");
    }

    [Fact]
    public void ShouldReturnError_WhenTooManyTopics()
    {
        // arrange
        var phrases = _faker.Lorem.Words(1);
        var topics = _faker.Lorem.Words(10);

        // act
        var result = SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, phrases, topics);

        // assert
        result.ShouldStartWith("Error: Too many topics");
    }

    [Fact]
    public void ShouldReturnError_WhenTooMaxResultsTooLarge()
    {
        // arrange
        var phrases = _faker.Lorem.Words(1);
        var topics = _faker.Lorem.Words(1);

        // act
        var result = SearchMemoryTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            phrases,
            topics,
            maxResults: 100
        );

        // assert
        result.ShouldStartWith("Error: maxResults is too large");
    }

    [Fact]
    public void ShouldReturnMemoryMatchingPhrases()
    {
        // arrange
        var topic = _faker.Lorem.Word();
        var content = _faker.Lorem.Sentence();
        var context = _faker.Lorem.Sentence();
        var searchPhrase = _faker.PickRandom(content.Split(' '));

        var memories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(ConnectionString, JsonSerializerOptions.Default, topic, [content], context)
        );
        Debug.Assert(memories is { Length: 1 });
        var expected = JsonSerializer.Deserialize<MemoryDto>(
            GetMemoryByIdTool.Handle(ConnectionString, JsonSerializerOptions.Default, memories[0].Id)
        );

        // act
        var actual = JsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, [searchPhrase])
        );

        // assert
        actual.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldFilterMatchingMemoriesByTopic_WhenTopicsAreProvided()
    {
        // arrange
        var topics = _faker.Lorem.Words();
        var searchPhrase = _faker.Lorem.Word();

        foreach (var topic in topics)
        {
            var memories = Enumerable
                .Range(start: 0, count: 3)
                .Select(_ => _faker.Lorem.Sentence() + searchPhrase)
                .ToArray();
            _ = CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                topic,
                memories,
                _faker.Lorem.Sentence()
            );
        }

        var expectedTopic = _faker.PickRandom(topics);

        // act
        var result = JsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, [searchPhrase], [expectedTopic])
        );

        // assert
        result.ShouldNotBeNull().ShouldAllBe(m => m.Topic == expectedTopic);
    }

    [Fact]
    public void ShouldLimitResultsBaseOnMaxResults()
    {
        // arrange
        var searchPhrase = _faker.Lorem.Word();
        var memories = Enumerable
            .Range(start: 0, count: 10)
            .Select(_ => _faker.Lorem.Sentence() + searchPhrase)
            .ToArray();

        _ = CreateMemoriesTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            _faker.Lorem.Word(),
            memories,
            _faker.Lorem.Sentence()
        );

        // act
        var result = JsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, [searchPhrase], maxResults: 2)
        );

        // assert
        result.ShouldNotBeNull().Length.ShouldBe(2);
    }

    [Fact]
    public void ShouldReturnMatchingMemory_WhenMemoryDoesNotHaveContext()
    {
        // arrange
        var content = _faker.Lorem.Word();

        var memories = JsonSerializer.Deserialize<CreatedMemoryDto[]>(
            CreateMemoriesTool.Handle(
                ConnectionString,
                JsonSerializerOptions.Default,
                _faker.Lorem.Sentence(),
                [content]
            )
        );

        Debug.Assert(memories is { Length: 1 });
        var expectedMemoryNodeId = memories[0].Id;

        // act
        var result = JsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, JsonSerializerOptions.Default, [content])
        );

        // assert
        result.ShouldNotBeNull().ShouldHaveSingleItem().Id.ShouldBe(expectedMemoryNodeId);
    }
}
