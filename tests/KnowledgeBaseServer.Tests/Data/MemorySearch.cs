using System;

namespace KnowledgeBaseServer.Tests.Data;

public sealed class MemorySearch
{
    public required Guid MemoryId { get; init; }

    public required string Content { get; init; }

    public required string Context { get; init; }
}
