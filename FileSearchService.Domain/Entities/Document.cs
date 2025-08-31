namespace FileSearchService.Domain.Entities
{
    public class Document
    {
        public required string Id { get; set; }
        public required float[] Vector { get; set; }
        public required DocumentMetadata Metadata { get; set; }
    }
}