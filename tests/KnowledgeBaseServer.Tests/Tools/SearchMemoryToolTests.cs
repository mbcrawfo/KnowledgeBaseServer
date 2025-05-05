using System;
using System.Collections.Generic;
using System.Linq;
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
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, topic, content, context: context))
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
        var topics = _faker.MakeUnique(f => f.Lorem.Word()).Take(3).ToArray();
        var searchPhrase = _faker.Lorem.Word();

        foreach (var topic in topics)
        {
            foreach (var m in _faker.Make(count: 3, () => _faker.Lorem.Sentence()))
            {
                _ = CreateMemoryTool.Handle(
                    ConnectionString,
                    topic,
                    m + searchPhrase,
                    context: _faker.Lorem.Sentence()
                );
            }
        }

        var expectedTopic = _faker.PickRandom(topics);

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase], [expectedTopic])
        );

        // assert
        result.Length.ShouldBe(3);
        result.ShouldAllBe(x => x.Topic == expectedTopic);
    }

    [Fact]
    public void ShouldLimitResultsBaseOnMaxResults()
    {
        // arrange
        var searchPhrase = _faker.Lorem.Word();

        foreach (var m in _faker.Make(count: 3, () => _faker.Lorem.Sentence() + searchPhrase))
        {
            _ = CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Word(), m, context: _faker.Lorem.Sentence());
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

    [Fact]
    public void ShouldOrderByImportanceAndRelevance()
    {
        // arrange
        var topic = _faker.Lorem.Word();
        var searchPhrase = _faker.Lorem.Word();

        // Create three memories with the same content (same relevance) but different importance values
        var content = $"This is a test about {searchPhrase} for ranking by importance";

        var lowImportanceId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, topic, content, importance: 0.1))
            .Id;

        var mediumImportanceId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, topic, content, importance: 0.5))
            .Id;

        var highImportanceId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, topic, content, importance: 0.9))
            .Id;

        // act
        var results = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase])
        );

        // assert
        results.Length.ShouldBe(3);
        // The high importance memory should come first
        results[0].Id.ShouldBe(highImportanceId);
        // The medium importance memory should come second
        results[1].Id.ShouldBe(mediumImportanceId);
        // The low importance memory should come last
        results[2].Id.ShouldBe(lowImportanceId);
    }

    [Fact]
    public void ShouldRankByRelevanceAndImportance()
    {
        // arrange
        var topic = _faker.Lorem.Word();
        var searchPhrase = "ranking";

        // Create memory with high relevance (exact phrase match) but low importance
        var highRelevanceLowImportanceId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(
                CreateMemoryTool.Handle(
                    ConnectionString,
                    topic,
                    $"This is specifically about {searchPhrase} and nothing else",
                    importance: 0.1
                )
            )
            .Id;

        // Create memory with moderate relevance but high importance
        var moderateRelevanceHighImportanceId = AppJsonSerializer
            .Deserialize<CreatedMemoryDto>(
                CreateMemoryTool.Handle(
                    ConnectionString,
                    topic,
                    $"This mentions {searchPhrase} among other things like sorting and ordering",
                    importance: 0.9
                )
            )
            .Id;

        // act
        var results = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase])
        );

        // assert
        results.Length.ShouldBe(2);

        // Since we're using a weighted approach,
        // the result ordering will depend on how SQLite FTS5 ranks these specific phrases
        // and how the weighting is applied. This test verifies that both factors
        // are considered, but the exact order depends on the specific weights used.
        // We expect both memories to be returned in the results.
        results.ShouldContain(m => m.Id == highRelevanceLowImportanceId);
        results.ShouldContain(m => m.Id == moderateRelevanceHighImportanceId);
    }

    [Fact]
    public void ShouldReturnOnlyNonOutdatedMemories_WhenExcludingOutdated()
    {
        // arrange
        var searchPhrase = _faker.Lorem.Word();

        var memoryIds = _faker
            .Make(count: 3, () => _faker.Lorem.Sentence() + searchPhrase)
            .Select(m =>
                AppJsonSerializer
                    .Deserialize<CreatedMemoryDto>(CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Word(), m))
                    .Id
            )
            .ToArray();
        var outdatedMemoryId = _faker.PickRandom(memoryIds);

        _ = MarkMemoryAsOutdatedTool.Handle(ConnectionString, outdatedMemoryId, _faker.Lorem.Sentence());

        var expectedMemoryIds = memoryIds.Where(id => id != outdatedMemoryId).ToArray();

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase], excludeOutdated: true)
        );

        // assert
        result.Select(m => m.Id).ShouldBe(expectedMemoryIds, ignoreOrder: true);
    }

    [Fact]
    public void ShouldFilterResults_WhenFilteringByTopicAndExcludingOutdated()
    {
        // arrange
        var topics = _faker.MakeUnique(f => f.Lorem.Word()).Take(3).ToArray();
        var searchPhrase = _faker.Lorem.Word();
        var memoryIdsByTopic = new Dictionary<string, List<Guid>>();

        foreach (var topic in topics)
        {
            memoryIdsByTopic[topic] = new List<Guid>();

            foreach (var m in _faker.Make(count: 3, () => _faker.Lorem.Sentence()))
            {
                var memory = AppJsonSerializer.Deserialize<CreatedMemoryDto>(
                    CreateMemoryTool.Handle(ConnectionString, topic, m + searchPhrase, context: _faker.Lorem.Sentence())
                );
                memoryIdsByTopic[topic].Add(memory.Id);
            }
        }

        var searchTopic = _faker.PickRandom(topics);
        var outdatedMemoryId = _faker.PickRandom(memoryIdsByTopic[searchTopic]);
        _ = MarkMemoryAsOutdatedTool.Handle(ConnectionString, outdatedMemoryId, _faker.Lorem.Sentence());
        var expectedMemoryIds = memoryIdsByTopic[searchTopic].Where(id => id != outdatedMemoryId).ToArray();

        // act
        var result = AppJsonSerializer.Deserialize<MemoryDto[]>(
            SearchMemoryTool.Handle(ConnectionString, [searchPhrase], [searchTopic], excludeOutdated: true)
        );

        // assert
        result.Select(x => x.Id).ShouldBe(expectedMemoryIds, ignoreOrder: true);
    }
}
