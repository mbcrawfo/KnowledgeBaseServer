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
    private readonly Faker<Memory> _memoryFaker = Memory.Faker();
    private readonly Faker<MemoryLink> _memoryLinkFaker = MemoryLink.Faker();
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void GetMemory_ShouldReturnMemory()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var replacementMemory = _memoryFaker.WithTopic(topic).WithContext(context).Generate();
        var memory = _memoryFaker
            .WithTopic(topic)
            .WithContext(context)
            .WithReplacementMemory(replacementMemory)
            .Generate();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemories([replacementMemory, memory]);
        }

        var expected = new MemoryDto(
            memory.Id,
            memory.Created,
            topic.Name,
            memory.Content,
            context.Value,
            replacementMemory.Id
        );

        // act
        var result = GetMemoryByIdTool.Handle(ConnectionString, JsonSerializerOptions.Default, memory.Id, false);

        var actual = JsonSerializer.Deserialize<MemoryDto>(result);

        // assert
        actual.ShouldNotBeNull().ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void GetMemory_ShouldReturnMemoryWithRelatedMemories()
    {
        // arrange
        var topic = _topicFaker.Generate();
        var context = _memoryContextFaker.Generate();
        var memory = _memoryFaker.WithTopic(topic).WithContext(context).Generate();
        // The topic and context persist on the faker.
        var linkedMemories = _memoryFaker.Generate(3);
        var memoryLinks = linkedMemories
            .Select(m => _memoryLinkFaker.Clone().WithMemories(memory, m).Generate())
            .ToArray();

        using (var seedConnection = ConnectionString.CreateConnection())
        {
            seedConnection.SeedTopic(topic);
            seedConnection.SeedMemoryContext(context);
            seedConnection.SeedMemories([memory, .. linkedMemories]);
            seedConnection.SeedMemoryLinks(memoryLinks);
        }

        var expected = new MemoryWithRelationsDto(
            memory.Id,
            memory.Created,
            topic.Name,
            memory.Content,
            context.Value,
            null,
            linkedMemories
                .Select(m => new MemoryDto(m.Id, m.Created, topic.Name, m.Content, context.Value, null))
                .OrderBy(m => m.Created)
                .ToList()
        );

        // act
        var result = GetMemoryByIdTool.Handle(ConnectionString, JsonSerializerOptions.Default, memory.Id, true);

        var actual = JsonSerializer.Deserialize<MemoryWithRelationsDto>(result);

        // assert
        actual.ShouldNotBeNull().ShouldBeEquivalentTo(expected);
    }
}
