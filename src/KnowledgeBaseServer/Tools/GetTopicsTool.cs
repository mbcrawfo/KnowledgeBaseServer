using System.ComponentModel;
using Dapper;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class GetTopicsTool
{
    [McpServerTool(Name = "GetTopics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists all topics in the knowledge base.")]
    public static string Handle(ConnectionString connectionString)
    {
        using var connection = connectionString.CreateConnection();

        var topics = connection
            .Query<string>(
                """
                select name
                from topics
                order by name
                """
            )
            .AsList();

        return AppJsonSerializer.Serialize(topics);
    }
}
