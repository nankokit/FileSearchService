using FileSearchService.Application.DTOs;
using FileSearchService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace FileSearchService.Application.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IFileIndexingService _indexingService;
    private readonly IFileReader _fileReader;
    private readonly ILogger _logger;

    public FileUploadService(IFileIndexingService indexingService, IFileReader fileReader, ILogger logger)
    {
        _indexingService = indexingService;
        _fileReader = fileReader;
        _logger = logger;
    }

    public async Task<FileUploadResponseDto> UploadFileAsync(IFormFile file, string dataPath)
    {
        if (file == null || file.Length == 0)
        {
            _logger.Warning("No file uploaded or file is empty");
            throw new ArgumentException("No file uploaded or file is empty");
        }

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("Invalid file type uploaded: {FileName}", file.FileName);
            throw new ArgumentException("Only .txt files are allowed");
        }

        Directory.CreateDirectory(dataPath); // Ensure directory exists
        string filePath = Path.Combine(dataPath, file.FileName);

        if (System.IO.File.Exists(filePath))
        {
            _logger.Warning("File already exists: {FileName}", file.FileName);
            throw new ArgumentException("File already exists");
        }

        try
        {
            // Save the file using FileReader
            using (var stream = file.OpenReadStream())
            {
                await _fileReader.WriteFileAsync(filePath, stream);
            }

            _logger.Information("File uploaded successfully: {FileName}", file.FileName);

            // Trigger indexing for the new file
            try
            {
                await _indexingService.IndexFilesAsync(filePath);
                _logger.Information("File indexed successfully: {FileName}", file.FileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to index uploaded file: {FileName}", file.FileName);
                // Note: We don't fail the upload if indexing fails, but we log it
            }

            return new FileUploadResponseDto
            {
                Message = $"File {file.FileName} uploaded and processed successfully",
                FileName = file.FileName,
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to upload file: {FileName}", file.FileName);
            throw;
        }
    }
}