using System.Collections.Generic;

namespace KnowledgeBaseServer.Dtos;

public sealed record TopicsResponseDto(IReadOnlyCollection<string> Topics);