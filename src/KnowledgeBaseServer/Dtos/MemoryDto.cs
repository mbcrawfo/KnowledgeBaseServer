using System;
using System.Text.Json.Serialization;

namespace KnowledgeBaseServer.Dtos;

public record MemoryDto(
    Guid Id,
    DateTimeOffset Created,
    string Topic,
    string Content,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Context
);
