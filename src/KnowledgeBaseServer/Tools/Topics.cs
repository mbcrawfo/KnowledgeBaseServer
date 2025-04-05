using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using Dapper;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
[UsedImplicitly]
public static class Topics
{
    [McpServerTool(Name = "GetTopics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists all topics in the knowledge base.")]
    [UsedImplicitly]
    public static string GetTopics(ConnectionString connectionString, JsonSerializerOptions jsonSerializerOptions)
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

public sealed record TopicsResponseDto(IReadOnlyCollection<string> Topics);
