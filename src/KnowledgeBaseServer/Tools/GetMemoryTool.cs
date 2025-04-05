using System;
using System.ComponentModel;
using System.Text.Json;
using Dapper;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class GetMemoryTool
{
    [McpServerTool(Name = "GetMemory", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Retrieves a memory from the knowledge base.")]
    public static string GetMemory(
        [Description("Id of the memory to retrieve.")] Guid memoryId,
        [Description("When true, automatically loads linked memories.")] bool includeLinkedMemories,
        ConnectionString connectionString,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        using var connection = connectionString.CreateConnection();

        var memory = connection.QuerySingleOrDefault<MemoryDto>(
            sql: """
            select m.id, m.created, t.name as topic, content, mc.value as context, replaced_by_memory_id
            from memories m
            inner join topics t on t.id = m.topic_id
            inner join memory_contexts mc on mc.id = m.context_id
            where m.id = @Id
            """,
            new { Id = memoryId }
        );

        if (memory is null)
        {
            return "Memory not found.";
        }

        if (!includeLinkedMemories)
        {
            return JsonSerializer.Serialize(memory, jsonSerializerOptions);
        }

        var linkedMemories = connection.Query<MemoryDto>(
            sql: """
            select m.id, m.created, t.name as topic, content, mc.value as context, replaced_by_memory_id
            from memory_links ml
            inner join memories m on m.id = ml.to_memory_id
            inner join topics t on t.id = m.topic_id
            inner join memory_contexts mc on mc.id = m.context_id
            where ml.from_memory_id = @Id
            order by m.created
            """,
            new { Id = memoryId }
        );

        var response = new MemoryWithRelationsDto(
            memory.Id,
            memory.Created,
            memory.Topic,
            memory.Content,
            memory.Context,
            memory.ReplacedByMemoryId,
            linkedMemories.AsList()
        );
        return JsonSerializer.Serialize(response, jsonSerializerOptions);
    }
}
