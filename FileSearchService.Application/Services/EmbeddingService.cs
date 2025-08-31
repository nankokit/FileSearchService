using FileSearchService.Application.Interfaces;
using Serilog;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using LangChain.Splitters.Text;

namespace FileSearchService.Application.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger _logger;

    public EmbeddingService(IConfiguration configuration, ILogger logger)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["HuggingFace:ApiKey"] ?? throw new ArgumentNullException("HuggingFace:ApiKey is missing in configuration");
        _model = configuration["HuggingFace:Model"] ?? "intfloat/multilingual-e5-large";
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                _logger.Error("Input text is empty or null");
                throw new ArgumentException("Input text cannot be empty");
            }

            text = text.Replace("\r\n", "\n").Trim();
            if (text.Length > 2000)
            {
                _logger.Warning("Text length {TextLength} exceeds 2000 chars, truncating", text.Length);
                text = text.Substring(0, 2000);
            }

            _logger.Information("Generating embedding for text snippet of length {TextLength}: {TextPreview}", text.Length, text.Length > 50 ? text.Substring(0, 50).Replace("\n", " ").Replace("\r", " ") + "..." : text.Replace("\n", " ").Replace("\r", " "));

            var requestBody = new { inputs = text };
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync($"https://router.huggingface.co/hf-inference/models/{_model}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"HF API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var embedding = JsonSerializer.Deserialize<List<float>>(responseContent);

            if (embedding == null || embedding.Count == 0)
            {
                throw new Exception("Empty or invalid embedding returned from HF API");
            }

            _logger.Information("Successfully generated embedding with length {EmbeddingLength}", embedding.Count);
            if (embedding.Count != 1024)
            {
                _logger.Error("Unexpected embedding length: {EmbeddingLength}, expected 1024", embedding.Count);
                throw new Exception($"Embedding length {embedding.Count} does not match expected 1024");
            }
            return embedding.ToArray();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate embedding: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task<List<(string Chunk, float[] Embedding)>> GenerateChunkedEmbeddingsAsync(string text)
    {
        var chunks = ChunkText(text);
        var chunkEmbeddings = new List<(string Chunk, float[] Embedding)>();

        foreach (var chunk in chunks)
        {
            var embedding = await GenerateEmbeddingAsync(chunk);
            chunkEmbeddings.Add((chunk, embedding));
        }

        return chunkEmbeddings;
    }

    private List<string> ChunkText(string text)
    {
        const int maxChunkSize = 1000;
        const int chunkOverlap = 300;

        text = text.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrEmpty(text))
        {
            _logger.Warning("Text is empty");
            return new List<string>();
        }

        var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: maxChunkSize, chunkOverlap: chunkOverlap, separators: new[] { "\n\n", "\n", ".", " " });

        var chunks = textSplitter.SplitText(text);

        _logger.Information("Split text into {ChunkCount} chunks", chunks.Count);

        return (List<string>)chunks;
    }

}
