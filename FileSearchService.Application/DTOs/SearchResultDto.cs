namespace FileSearchService.Application.DTOs;

public class SearchResultDto
{
    public required string Id { get; set; }
    public float Score { get; set; }
    public required SearchResultMetadata Metadata { get; set; }
}