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
    private const double RankWeight = 0.6;
    private const double ImportanceWeight = 0.4;

    [McpServerTool(Name = "SearchMemory", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Performs a search for memories in the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("Phrases to search for. Maximum length: 5")] string[] phrases,
        [Description("Optionally limits the search to certain topics. Maximum length: 5")] string[]? topics = null,
        [Description("The maximum number of memories to return. Default: 5, Maximum value: 50")] int maxResults = 5,
        [Description("Excludes memories that are outdated. Default: false")] bool excludeOutdated = false
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
            select
                mn.id,
                mn.created,
                t.name as topic,
                mn.content,
                mn.importance,
                mn.context,
                mn.outdated,
                mn.outdated_reason
            from search_results sr
            inner join memory_nodes mn on mn.id = sr.memory_node_id
            inner join topics t on t.id = mn.topic_id
            """
        );

        if (topics.Length > 0 || excludeOutdated)
        {
            sb.AppendLine().Append("where ");

            if (topics is { Length: > 0 })
            {
                sb.Append("t.name in @Topics");
            }

            if (excludeOutdated)
            {
                sb.Append(" mn.outdated is null");
            }

            sb.AppendLine();
        }

        sb.AppendLine()
            .AppendLine("order by (sr.rank * @RankWeight) + (mn.importance * @ImportanceWeight) desc")
            .AppendLine("limit @MaxResults");

        using var connection = connectionString.CreateConnection();

        var memories = connection
            .Query<MemoryDto>(
                sb.ToString(),
                new
                {
                    Phrases = phrasesParam,
                    Topics = topics,
                    MaxResults = maxResults,
                    RankWeight,
                    ImportanceWeight,
                }
            )
            .AsList();

        return AppJsonSerializer.Serialize(memories);
    }
}
