using System.Net;
using System.Text;
using System.Text.Json;
using FileSearchService.Application.Interfaces;
using FileSearchService.Domain.Entities;
using FileSearchService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FileSearchService.Infrastructure;


public class QdrantClient : IQdrantClient
{
    private readonly HttpClient _httpClient;
    private readonly string _collectionName;
    private readonly ILogger _logger;

    public QdrantClient(IConfiguration configuration, ILogger logger)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var qdrantUrl = configuration["Qdrant:Url"] ?? throw new ArgumentNullException("Qdrant:Url is missing in configuration");
        _httpClient = new HttpClient { BaseAddress = new Uri(qdrantUrl) };
        _collectionName = configuration["Qdrant:CollectionName"] ?? throw new ArgumentNullException("Qdrant:CollectionName is missing in configuration");

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureCollectionExistsAsync()
    {
        try
        {
            _logger.Information("Checking/creating Qdrant collection: {CollectionName}", _collectionName);

            var checkResponse = await _httpClient.GetAsync($"/collections/{_collectionName}");
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.Information("Qdrant collection already exists: {CollectionName}", _collectionName);
                return;
            }

            var collectionConfig = new
            {
                vectors = new
                {
                    size = 1024,
                    distance = "Cosine"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(collectionConfig), Encoding.UTF8, "application/json");
            var createResponse = await _httpClient.PutAsync($"/collections/{_collectionName}", content);

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync();
                _logger.Error("Failed to create Qdrant collection: {ErrorContent}", errorContent);
                throw new QdrantOperationFailedException("collection creation", errorContent);
            }

            _logger.Information("Qdrant collection created: {CollectionName}", _collectionName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize Qdrant collection");
            throw;
        }
    }

    public async Task<bool> DocumentExistsAsync(string id)
    {
        try
        {
            _logger.Information("Checking if document exists: {Id}", id);
            var response = await _httpClient.GetAsync($"/collections/{_collectionName}/points/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to check document existence: {Id}", id);
            throw new QdrantOperationFailedException("document existence check", ex.Message);
        }
    }

    public async Task UpsertDocumentAsync(Document document)
    {
        try
        {
            if (document.Vector == null || document.Vector.Length != 1024)
            {
                _logger.Error("Invalid vector for document {DocumentId}: Length = {VectorLength}", document.Id, document.Vector?.Length ?? 0);
                throw new BaseCustomException($"Vector must be non-null and have length 1024, got length {document.Vector?.Length ?? 0}", StatusCodes.Status400BadRequest, "INVALID_VECTOR");
            }

            var point = new
            {
                id = document.Id,
                vector = document.Vector,
                payload = new
                {
                    text = document.Metadata.Text,
                    file_name = document.Metadata.FileName,
                    file_path = document.Metadata.FilePath,
                    chunk_index = document.Metadata.ChunkIndex
                }
            };

            var points = new[] { point };
            var requestContent = JsonSerializer.Serialize(new { points }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            _logger.Information("Sending to Qdrant: URL = /collections/{CollectionName}/points, PointId = {Content}, VectorLength = {VectorLength}", _collectionName, point.id, point.vector.Length);

            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/collections/{_collectionName}/points", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.Error("Qdrant API failed with status {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                throw new QdrantOperationFailedException("upsert", errorContent);
            }

            _logger.Information("Upserted document: {DocumentId}", document.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to upsert document: {DocumentId}", document.Id);
            throw;
        }
    }

    public async Task<List<SearchResult>> SearchAsync(float[] queryVector, int limit)
    {
        try
        {
            var searchRequest = new
            {
                vector = queryVector,
                limit,
                with_payload = true,
                with_vector = true,
                score_threshold = 0.6f
            };

            var content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/collections/{_collectionName}/points/search", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<QdrantSearchResponse>(responseContent)
                ?? throw new InvalidOperationException("Failed to deserialize Qdrant search response");

            return searchResult.result.Select(r => new SearchResult
            {
                Id = r.id,
                Score = r.score,
                Metadata = JsonSerializer.Deserialize<DocumentMetadata>(JsonSerializer.Serialize(r.payload))!
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Search failed in Qdrant");
            throw new QdrantOperationFailedException("search", ex.Message);
        }
    }

    private class QdrantSearchResponse
    {
        public required List<QdrantPoint> result { get; set; }
    }

    private class QdrantPoint
    {
        public required string id { get; set; }
        public required float[] vector { get; set; }
        public required object payload { get; set; }
        public float score { get; set; }
    }
}