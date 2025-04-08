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
        var result = SearchMemoryTool.Handle(ConnectionString, phrases);

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
        var result = SearchMemoryTool.Handle(ConnectionString, phrases, topics);

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
        var result = SearchMemoryTool.Handle(ConnectionString, phrases, topics, maxResults: 100);

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

        var memoryNodeId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, topic, content, context))
            .Id;
        var expected = AppJsonSerializer.Deserialize<MemoryDto>(
            GetMemoryByIdTool.Handle(ConnectionString, memoryNodeId)
        );

        // act
        var actual = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase])
        );

        // assert
        actual.ShouldHaveSingleItem().ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldFilterMatchingMemoriesByTopic_WhenTopicsAreProvided()
    {
        // arrange
        var topics = _faker.Lorem.Words();
        var searchPhrase = _faker.Lorem.Word();

        foreach (var topic in topics)
        {
            foreach (var m in _faker.Make(count: 3, () => _faker.Lorem.Sentence()))
            {
                _ = CreateMemoryTool.Handle(ConnectionString, topic, m, _faker.Lorem.Sentence());
            }
        }

        var expectedTopic = _faker.PickRandom(topics);

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase], [expectedTopic])
        );

        // assert
        result.ShouldAllBe(m => m.Topic == expectedTopic);
    }

    [Fact]
    public void ShouldLimitResultsBaseOnMaxResults()
    {
        // arrange
        var searchPhrase = _faker.Lorem.Word();

        foreach (var m in _faker.Make(count: 3, () => _faker.Lorem.Sentence() + searchPhrase))
        {
            _ = CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Word(), m, _faker.Lorem.Sentence());
        }

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase], maxResults: 2)
        );

        // assert
        result.Length.ShouldBe(2);
    }

    [Fact]
    public void ShouldReturnMatchingMemory_WhenMemoryDoesNotHaveContext()
    {
        // arrange
        var content = _faker.Lorem.Word();

        var expectedMemoryNodeId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Sentence(), content))
            .Id;

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(SearchMemoryTool.Handle(ConnectionString, [content]));

        // assert
        result.ShouldHaveSingleItem().Id.ShouldBe(expectedMemoryNodeId);
    }
}
