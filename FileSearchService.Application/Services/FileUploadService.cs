using FileSearchService.Application.DTOs;
using FileSearchService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using FileSearchService.Domain.Exceptions;
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
            throw new BaseCustomException("No file uploaded or file is empty", StatusCodes.Status400BadRequest, "EMPTY_FILE");
        }

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("Invalid file type uploaded: {FileName}", file.FileName);
            throw new InvalidFileTypeException(file.FileName);
        }

        Directory.CreateDirectory(dataPath);
        string filePath = Path.Combine(dataPath, file.FileName);

        if (System.IO.File.Exists(filePath))
        {
            _logger.Warning("File already exists: {FileName}", file.FileName);
            throw new FileAlreadyExistsException(file.FileName);
        }

        try
        {
            using (var stream = file.OpenReadStream())
            {
                await _fileReader.WriteFileAsync(filePath, stream);
            }

            _logger.Information("File uploaded successfully: {FileName}", file.FileName);

            try
            {
                await _indexingService.IndexFileAsync(filePath);
                _logger.Information("File indexed successfully: {FileName}", file.FileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to index uploaded file: {FileName}", file.FileName);
                throw new IndexingFailedException(file.FileName, ex.Message);
            }

            return new FileUploadResponseDto
            {
                Message = $"File {file.FileName} uploaded and indexed successfully",
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