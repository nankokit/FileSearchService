namespace FileSearchService.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
    Task<List<(string Chunk, float[] Embedding)>> GenerateChunkedEmbeddingsAsync(string text);
}
