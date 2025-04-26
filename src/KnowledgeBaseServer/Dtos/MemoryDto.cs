using System;
using System.Text.Json.Serialization;

namespace KnowledgeBaseServer.Dtos;

public record MemoryDto(
    Guid Id,
    DateTimeOffset Created,
    string Topic,
    string Content,
    double Importance,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Context,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTimeOffset? Outdated,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? OutdatedReason
);
