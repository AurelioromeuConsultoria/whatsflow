using WhatsFlow.Application.DTOs.Search;

namespace WhatsFlow.Application.Services;

public interface ISearchService
{
    Task<IReadOnlyList<GlobalSearchItemDto>> SearchAsync(string query, int limit);
}

