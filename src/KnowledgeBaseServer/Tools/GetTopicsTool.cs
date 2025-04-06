using System.ComponentModel;
using System.Text.Json;
using Dapper;
using JetBrains.Annotations;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class GetTopicsTool
{
    [McpServerTool(Name = "GetTopics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists all topics in the knowledge base.")]
    public static string Handle(ConnectionString connectionString, JsonSerializerOptions jsonSerializerOptions)
    {
        using var connection = connectionString.CreateConnection();

        var topics = connection.Query<string>(
            """
            select name
            from topics
            order by name
            """
        );

        var response = new TopicsResponseDto(topics.AsList());
        return JsonSerializer.Serialize(response, jsonSerializerOptions);
    }
}
