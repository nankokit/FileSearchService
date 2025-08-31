namespace FileSearchService.Domain.Entities
{
    public class SearchResult
    {
        public required string Id { get; set; }
        public float Score { get; set; }
        public required DocumentMetadata Metadata { get; set; }
    }
}