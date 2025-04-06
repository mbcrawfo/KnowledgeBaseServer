using System.Collections.Generic;

namespace KnowledgeBaseServer.Dtos;

public sealed record AddMemoriesResponseDto(IReadOnlyCollection<CreatedMemoryDto> Memories);