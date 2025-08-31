using FileSearchService.Application.Interfaces;
using FileSearchService.Application.DTOs;
using FileSearchService.Domain.Entities;
using Serilog;

namespace FileSearchService.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IQdrantClient _qdrantClient;
        private readonly ILogger _logger;

        public SearchService(IEmbeddingService embeddingService, IQdrantClient qdrantClient, ILogger logger)
        {
            _embeddingService = embeddingService;
            _qdrantClient = qdrantClient;
            _logger = logger;
        }

        public async Task<List<SearchResultDto>> SearchAsync(string query, int limit)
        {
            try
            {
                _logger.Information("Performing search with query: {Query}, limit: {Limit}", query, limit);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
                var searchResults = await _qdrantClient.SearchAsync(embedding, limit);

                var results = searchResults
                    .Select(r => new SearchResultDto
                    {
                        Id = r.Id,
                        Score = r.Score,
                        Metadata = new SearchResultMetadata
                        {
                            FileName = r.Metadata.FileName,
                            FilePath = r.Metadata.FilePath,
                            TextSnippet = GenerateTextSnippet(r.Metadata.Text, query),
                            ChunkIndex = r.Metadata.ChunkIndex
                        }
                    })
                    .ToList();

                _logger.Information("Returning {ResultCount} search results", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Search failed for query: {Query}", query);
                throw;
            }
        }

        private string GenerateTextSnippet(string text, string query)
        {
            var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index == -1) return text.Length > 400 ? text.Substring(0, 400) + "..." : text;

            var start = Math.Max(0, index - 50);
            var length = Math.Min(100, text.Length - start);
            return text.Substring(start, length) + (text.Length > start + length ? "..." : "");
        }
    }
}