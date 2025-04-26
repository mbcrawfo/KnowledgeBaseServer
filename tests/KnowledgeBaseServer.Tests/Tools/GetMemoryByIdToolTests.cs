using System.Linq;
using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class GetMemoryByIdToolTests : DatabaseTest
{
    private readonly Faker _faker = new();
    private readonly Faker<MemoryEdge> _memoryEdgeFaker = MemoryEdge.Faker();
    private readonly Faker<MemoryNode> _memoryNodeFaker = MemoryNode.Faker();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void ShouldReturnMemory()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _faker.Lorem.Sentence();
        var memory = _memoryNodeFaker.WithTopic(topic).WithContext(context).WithOutdated().Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryNode(memory);
        }

        var expected = new MemoryDto(
            memory.Id,
            memory.Created,
            topic.Name,
            memory.Content,
            memory.Importance,
            context,
            memory.Outdated,
            memory.OutdatedReason
        );

        // act
        var actual = AppJsonSerializer.Deserialize<MemoryDto>(
            GetMemoryByIdTool.Handle(ConnectionString, memory.Id, includeLinkedMemories: false)
        );

        // assert
        actual.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldReturnMemoryWithRelatedMemories()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _faker.Lorem.Sentence();
        var memory = _memoryNodeFaker.WithTopic(topic).WithContext(context).WithOutdated().Generate();
        // The topic and context persist on the faker.
        var linkedMemories = _memoryNodeFaker.Generate(3);
        var memoryEdges = linkedMemories
            .Select(m => _memoryEdgeFaker.Clone().WithNodes(memory, m).Generate())
            .ToArray();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryNodes([memory, .. linkedMemories]);
            seedConnection.SeedMemoryEdges(memoryEdges);
        }

        var expected = new MemoryWithRelationsDto(
            memory.Id,
            memory.Created,
            topic.Name,
            memory.Content,
            memory.Importance,
            context,
            memory.Outdated,
            memory.OutdatedReason,
            linkedMemories
                .Select(m => new MemoryDto(
                    m.Id,
                    m.Created,
                    topic.Name,
                    m.Content,
                    m.Importance,
                    context,
                    m.Outdated,
                    m.OutdatedReason
                ))
                .OrderBy(m => m.Created)
                .ToList()
        );

        // act
        var actual = AppJsonSerializer.Deserialize<MemoryWithRelationsDto>(
            GetMemoryByIdTool.Handle(ConnectionString, memory.Id, includeLinkedMemories: true)
        );

        // assert
        actual.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldReturnMemory_WhenMemoriesDoNotHaveContext()
    {
        // arrange
        var memories = _faker
            .Lorem.Words(4)
            .Select(w =>
                AppJsonSerializer.Deserialize<CreatedMemoryDto>(
                    CreateMemoryTool.Handle(ConnectionString, _faker.Lorem.Sentence(), w)
                )
            )
            .ToArray();
        var sourceMemoryNodeId = memories[0].Id;
        var targetMemoryNodeIds = memories.Skip(1).Select(m => m.Id).ToArray();

        _ = ConnectMemoriesTool.Handle(ConnectionString, sourceMemoryNodeId, targetMemoryNodeIds);

        // act
        var result = AppJsonSerializer.Deserialize<MemoryWithRelationsDto>(
            GetMemoryByIdTool.Handle(ConnectionString, sourceMemoryNodeId, includeLinkedMemories: true)
        );

        // assert
        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldBe(sourceMemoryNodeId),
            () => result.LinkedMemories.Select(m => m.Id).ShouldBe(targetMemoryNodeIds, ignoreOrder: true)
        );
    }
}
