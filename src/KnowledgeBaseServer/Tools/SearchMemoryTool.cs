using System.ComponentModel;
using System.Linq;
using System.Text;
using Dapper;
using KnowledgeBaseServer.Dtos;
using KnowledgeBaseServer.Extensions;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class SearchMemoryTool
{
    private const int MaxPhrases = 5;
    private const int MaxTopics = 5;
    private const int MaxMaxResults = 50;

    [McpServerTool(Name = "SearchMemory", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Performs a search for memories in the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("Phrases to search for. Maximum length: 5")] string[] phrases,
        [Description("Optionally limits the search to certain topics. Maximum length: 5")] string[]? topics = null,
        [Description("The maximum number of memories to return. Default: 5, Maximum value: 50")] int maxResults = 5
    )
    {
        topics ??= [];

        if (phrases.Length > MaxPhrases)
        {
            return $"Error: Too many {nameof(phrases)}, maximum length is {MaxPhrases}";
        }

        if (topics.Length > MaxTopics)
        {
            return $"Error: Too many {nameof(topics)}, maximum length is {MaxTopics}";
        }

        if (maxResults > MaxMaxResults)
        {
            return $"Error: {nameof(maxResults)} is too large, maximum value is {MaxMaxResults}";
        }

        var phrasesParam = string.Join(" OR ", phrases.Select(s => $"\"{s.RemovePunctuation()}\""));

        var sb = new StringBuilder(
            """
            with search_results as (
              select memory_node_id, rank
              from memory_search
              where memory_search match @Phrases
            )
            select mn.id, mn.created, t.name as topic, mn.content, mc.value as context, mn.outdated, mn.outdated_reason
            from search_results sr
            inner join memory_nodes mn on mn.id = sr.memory_node_id
            inner join topics t on t.id = mn.topic_id
            left outer join memory_contexts mc on mc.id = mn.context_id
            """
        );

        if (topics is { Length: > 0 })
        {
            sb.AppendLine().AppendLine("where t.name in @Topics");
        }

        sb.AppendLine().AppendLine("order by sr.rank").AppendLine("limit @MaxResults");

        using var connection = connectionString.CreateConnection();

        var memories = connection
            .Query<MemoryDto>(
                sb.ToString(),
                new
                {
                    Phrases = phrasesParam,
                    Topics = topics,
                    MaxResults = maxResults,
                }
            )
            .AsList();

        return AppJsonSerializer.Serialize(memories);
    }
}
