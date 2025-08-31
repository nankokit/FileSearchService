using FileSearchService.Application.Interfaces;
using Serilog;

namespace FileSearchService.Infrastructure;

public class FileReader : IFileReader
{
    private readonly ILogger _logger;

    public FileReader(ILogger logger)
    {
        _logger = logger;
    }

    public List<string> GetTextFiles(string directoryPath)
    {
        try
        {
            _logger.Information("Scanning directory for text files: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.Warning("Directory not found: {DirectoryPath}", directoryPath);
                return new List<string>();
            }

            var files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories).ToList();
            _logger.Information("Found {FileCount} text files", files.Count);
            return files;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to scan directory: {DirectoryPath}", directoryPath);
            throw;
        }
    }

    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            _logger.Information("Reading file: {FilePath}", filePath);
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to read file: {FilePath}", filePath);
            throw;
        }
    }

    public async Task WriteFileAsync(string filePath, Stream content)
    {
        try
        {
            _logger.Information("Writing file: {FilePath}", filePath);
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream);
            }
            _logger.Information("Successfully wrote file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to write file: {FilePath}", filePath);
            throw;
        }
    }
}