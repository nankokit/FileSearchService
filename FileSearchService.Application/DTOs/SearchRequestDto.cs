namespace FileSearchService.Application.DTOs;

public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Query))
            throw new ArgumentException("Query cannot be empty or whitespace.");
        if (Limit <= 0 || Limit > 100)
            throw new ArgumentException("Limit must be between 1 and 100.");
    }
}