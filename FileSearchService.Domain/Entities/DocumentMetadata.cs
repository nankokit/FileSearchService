using System.Text.Json.Serialization;

namespace FileSearchService.Domain.Entities
{
    public class DocumentMetadata
    {
        [JsonPropertyName("text")]
        public required string Text { get; set; }

        [JsonPropertyName("file_name")]
        public required string FileName { get; set; }

        [JsonPropertyName("file_path")]
        public required string FilePath { get; set; }

        [JsonPropertyName("chunk_index")]
        public int ChunkIndex { get; set; }
    }
}