namespace FileSearchService.Application.DTOs
{
    public class SearchResultMetadata
    {
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public required string TextSnippet { get; set; }
        public int ChunkIndex { get; set; }
    }
}