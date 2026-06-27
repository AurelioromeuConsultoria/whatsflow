namespace WhatsFlow.Application.DTOs.Search;

public class GlobalSearchResultDto
{
    public required IReadOnlyList<GlobalSearchItemDto> Items { get; init; }
}

