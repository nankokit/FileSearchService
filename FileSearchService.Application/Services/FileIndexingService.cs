using System.Security.Cryptography;
using System.Text;
using FileSearchService.Application.Interfaces;
using FileSearchService.Domain.Entities;
using FileSearchService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace FileSearchService.Application.Services;

public class FileIndexingService : IFileIndexingService
{
    private readonly IFileReader _fileReader;
    private readonly IEmbeddingService _embeddingService;
    private readonly IQdrantClient _qdrantClient;
    private readonly ILogger _logger;

    public FileIndexingService(IFileReader fileReader, IEmbeddingService embeddingService, IQdrantClient qdrantClient, ILogger logger)
    {
        _fileReader = fileReader;
        _embeddingService = embeddingService;
        _qdrantClient = qdrantClient;
        _logger = logger;
    }

    public async Task IndexFilesAsync(string directoryPath)
    {
        try
        {
            _logger.Information("Starting file indexing for directory: {DirectoryPath}", directoryPath);

            await _qdrantClient.EnsureCollectionExistsAsync();

            var files = _fileReader.GetTextFiles(directoryPath);
            _logger.Information("Found {FileCount} text files to process", files.Count);

            foreach (var filePath in files)
            {
                await IndexFileAsync(filePath);
            }

            _logger.Information("Completed indexing {FileCount} files", files.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to index files in directory: {DirectoryPath}", directoryPath);
            throw new IndexingFailedException("directory", ex.Message);
        }
    }

    public async Task IndexFileAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            _logger.Information("Processing file: {FilePath}", filePath);

            var text = await _fileReader.ReadFileAsync(filePath);
            if (string.IsNullOrEmpty(text))
            {
                _logger.Warning("File is empty: {FilePath}", filePath);
                throw new BaseCustomException($"File is empty: {fileName}", StatusCodes.Status400BadRequest, "EMPTY_FILE");
            }

            var chunkEmbeddings = await _embeddingService.GenerateChunkedEmbeddingsAsync(text);
            _logger.Information("Generated {ChunkCount} chunks for file: {FilePath}", chunkEmbeddings.Count, filePath);

            for (int chunkIndex = 0; chunkIndex < chunkEmbeddings.Count; chunkIndex++)
            {
                var (chunkText, embedding) = chunkEmbeddings[chunkIndex];
                var id = ComputeChunkUuid(fileName, chunkIndex);

                if (await _qdrantClient.DocumentExistsAsync(id))
                {
                    _logger.Information("Skipping already indexed chunk {ChunkIndex} of file: {FilePath}", chunkIndex, filePath);
                    continue;
                }

                var document = new Document
                {
                    Id = id,
                    Vector = embedding,
                    Metadata = new DocumentMetadata
                    {
                        Text = chunkText,
                        FileName = fileName,
                        FilePath = filePath,
                        ChunkIndex = chunkIndex
                    }
                };

                await _qdrantClient.UpsertDocumentAsync(document);
                _logger.Information("Indexed chunk {ChunkIndex} of file: {FilePath}", chunkIndex, filePath);
            }
        }
        catch (HuggingFaceApiException ex)
        {
            _logger.Error(ex, "Failed to index file {FilePath} due to Hugging Face API error: {ErrorMessage}", filePath, ex.Message);
            throw new IndexingFailedException(Path.GetFileName(filePath), $"Hugging Face API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to index file: {FilePath}", filePath);
            throw new IndexingFailedException(Path.GetFileName(filePath), ex.Message);
        }
    }
    private string ComputeChunkUuid(string fileName, int chunkIndex)
    {
        var input = $"{fileName}:{chunkIndex}";
        var inputBytes = Encoding.UTF8.GetBytes(input);

        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(inputBytes);

        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        var uuidBytes = new byte[16];
        Array.Copy(hash, uuidBytes, 16);

        return new Guid(uuidBytes).ToString();
    }
}