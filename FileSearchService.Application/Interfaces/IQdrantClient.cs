using FileSearchService.Domain.Entities;

namespace FileSearchService.Application.Interfaces;

public interface IQdrantClient
{
    Task EnsureCollectionExistsAsync();
    Task<bool> DocumentExistsAsync(string id);
    Task UpsertDocumentAsync(Document document);
    Task<List<SearchResult>> SearchAsync(float[] queryVector, int limit);
}
