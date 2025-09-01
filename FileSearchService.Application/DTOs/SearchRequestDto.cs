using FileSearchService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace FileSearchService.Application.DTOs;

public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Query))
            throw new BaseCustomException("Search query cannot be empty", StatusCodes.Status400BadRequest, "EMPTY_QUERY");

        if (Limit <= 0 || Limit > 100)
            throw new BaseCustomException("Limit must be greater than zero and less than 100", StatusCodes.Status400BadRequest, "INVALID_LIMIT");
    }
}