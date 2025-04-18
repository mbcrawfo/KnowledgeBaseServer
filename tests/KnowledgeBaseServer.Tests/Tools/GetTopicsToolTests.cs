using System.Linq;
using System.Text.Json;
using Bogus;
using KnowledgeBaseServer.Tests.Data;
using KnowledgeBaseServer.Tools;
using Shouldly;
using Xunit;

namespace KnowledgeBaseServer.Tests.Tools;

public class GetTopicsToolTests : DatabaseTest
{
    private readonly Faker<Topic> _topicFaker = Topic.Faker();

    [Fact]
    public void ShouldReturnTopicsInDatabase()
    {
        // arrange
        var topics = _topicFaker.Generate(3);
        using (var connection = ConnectionString.CreateConnection())
        {
            connection.SeedTopics(topics);
        }

        var expected = topics.Select(t => t.Name).Order().ToArray();

        // act
        var actual = AppJsonSerializer.Deserialize<string[]>(GetTopicsTool.Handle(ConnectionString));

        // assert
        actual.ShouldBe(expected);
    }
}
