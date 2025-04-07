using System.Linq;
using System.Text.Json;
using Bogus;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class GetMemoryByIdToolTests : DatabaseTest
{
    private readonly Faker<MemoryContext> _memoryContextFaker = MemoryContext.Faker();
    private readonly Faker<MemoryNode> _memoryNodeFaker = MemoryNode.Faker();
    private readonly Faker<MemoryEdge> _memoryEdgeFaker = MemoryEdge.Faker();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void ShouldReturnMemory()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var memory = _memoryNodeFaker.WithTopic(topic).WithContext(context).Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemoryNode(memory);
        }

        var expected = new MemoryDto(memory.Id, memory.Created, topic.Name, memory.Content, context.Value);

        // act
        var result = GetMemoryByIdTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            memory.Id,
            includeLinkedMemories: false
        );

        var actual = JsonSerializer.Deserialize<MemoryDto>(result);

        // assert
        actual.ShouldNotBeNull().ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void ShouldReturnMemoryWithRelatedMemories()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var memory = _memoryNodeFaker.WithTopic(topic).WithContext(context).Generate();
        // The topic and context persist on the faker.
        var linkedMemories = _memoryNodeFaker.Generate(3);
        var memoryEdges = linkedMemories
            .Select(m => _memoryEdgeFaker.Clone().WithNodes(memory, m).Generate())
            .ToArray();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemoryNodes([memory, .. linkedMemories]);
            seedConnection.SeedMemoryEdges(memoryEdges);
        }

        var expected = new MemoryWithRelationsDto(
            memory.Id,
            memory.Created,
            topic.Name,
            memory.Content,
            context.Value,
            linkedMemories
                .Select(m => new MemoryDto(m.Id, m.Created, topic.Name, m.Content, context.Value))
                .OrderBy(m => m.Created)
                .ToList()
        );

        // act
        var result = GetMemoryByIdTool.Handle(
            ConnectionString,
            JsonSerializerOptions.Default,
            memory.Id,
            includeLinkedMemories: true
        );

        var actual = JsonSerializer.Deserialize<MemoryWithRelationsDto>(result);

        // assert
        actual.ShouldNotBeNull().ShouldBeEquivalentTo(expected);
    }
}
