using System;

namespace KnowledgeBaseServer.Dtos;

public sealed record CreatedMemoryDto(Guid Id, string Content);
