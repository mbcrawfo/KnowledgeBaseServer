using System;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemorySearch
{
    public required Guid MemoryNodeId { get; init; }

    public required string MemoryContent { get; init; }

    public required string MemoryContext { get; init; }
}
