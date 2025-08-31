
using FileSearchService.Application.DTOs;

namespace FileSearchService.Application.Interfaces;

public interface ISearchService
{
    Task<List<SearchResultDto>> SearchAsync(string query, int limit);
}
