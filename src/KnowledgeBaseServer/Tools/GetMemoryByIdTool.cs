using System;
using System.ComponentModel;
using System.Text.Json;
using Dapper;
using KnowledgeBaseServer.Dtos;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerToolType]
public static class GetMemoryByIdTool
{
    [McpServerTool(Name = "GetMemoryById", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Retrieves a single memory from the knowledge base.")]
    public static string Handle(
        ConnectionString connectionString,
        JsonSerializerOptions jsonSerializerOptions,
        [Description("Id of the memory node to retrieve.")] Guid memoryNodeId,
        [Description("When true, include memory nodes linked to the requested memory.")]
            bool includeLinkedMemories = false
    )
    {
        using var connection = connectionString.CreateConnection();

        var memory = connection.QuerySingleOrDefault<MemoryDto>(
            sql: """
            select mn.id, mn.created, t.name as topic, mn.content, mc.value as context, mn.outdated, mn.outdated_reason
            from memory_nodes mn
            inner join topics t on t.id = mn.topic_id
            left outer join memory_contexts mc on mc.id = mn.context_id
            where mn.id = @Id
            """,
            new { Id = memoryNodeId }
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
            select mn.id, mn.created, t.name as topic, mn.content, mc.value as context, mn.outdated, mn.outdated_reason
            from memory_edges me
            inner join memory_nodes mn on mn.id = me.target_memory_node_id
            inner join topics t on t.id = mn.topic_id
            left outer join memory_contexts mc on mc.id = mn.context_id
            where me.source_memory_node_id = @Id
            order by mn.created
            """,
            new { Id = memoryNodeId }
        );

        var response = new MemoryWithRelationsDto(
            memory.Id,
            memory.Created,
            memory.Topic,
            memory.Content,
            memory.Context,
            memory.Outdated,
            memory.OutdatedReason,
            linkedMemories.AsList()
        );
        return JsonSerializer.Serialize(response, jsonSerializerOptions);
    }
}
