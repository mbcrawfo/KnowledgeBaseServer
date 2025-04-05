using System;
using System.Collections.Generic;

namespace KnowledgeBaseServer.Dtos;

public sealed record MemoryWithRelationsDto(
    Guid Id,
    DateTimeOffset Created,
    string Topic,
    string Content,
    string Context,
    Guid? ReplacedByMemoryId,
    IReadOnlyCollection<MemoryDto> LinkedMemories
) : MemoryDto(Id, Created, Topic, Content, Context, ReplacedByMemoryId);
