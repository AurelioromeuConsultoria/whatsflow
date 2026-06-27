namespace WhatsFlow.Application.DTOs.Search;

public class GlobalSearchItemDto
{
    public required string Type { get; init; }
    public required int Id { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
}

