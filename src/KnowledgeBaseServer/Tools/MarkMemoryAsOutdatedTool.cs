using System;
using System.ComponentModel;
using Dapper;
using ModelContextProtocol.Server;

namespace KnowledgeBaseServer.Tools;

[McpServerPromptType]
public static class MarkMemoryAsOutdatedTool
{
    [McpServerTool(
        Name = "MarkMemoryAsOutdated",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false
    )]
    [Description("Updates a memory node to indicate that it is outdated or invalid.")]
    public static string Handle(
        ConnectionString connectionString,
        [Description("Id of the memory node to update.")] Guid memoryNodeId,
        [Description("Reason why the memory is outdated or invalid.")] string reason
    )
    {
        using var connection = connectionString.CreateConnection();

        var existing = connection.QuerySingleOrDefault<ExistingMemoryNodeDto>(
            "select id, outdated from memory_nodes where id = @Id",
            new { Id = memoryNodeId }
        );

        if (existing is null)
        {
            return "Invalid memory node ID.";
        }

        if (existing.Outdated is not null)
        {
            return "Memory is already marked as outdated.";
        }

        _ = connection.Execute(
            """
            update memory_nodes
            set outdated = @Outdated, outdated_reason = @Reason
            where id = @Id and outdated is null
            """,
            new
            {
                Id = memoryNodeId,
                Outdated = DateTimeOffset.UtcNow,
                Reason = reason,
            }
        );

        return "Memory marked as outdated.";
    }

    private sealed record ExistingMemoryNodeDto(Guid Id, string? Outdated);
}
