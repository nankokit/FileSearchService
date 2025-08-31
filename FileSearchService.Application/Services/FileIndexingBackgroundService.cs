using FileSearchService.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FileSearchService.Application.Services;

public class FileIndexingBackgroundService : BackgroundService
{
    private readonly IFileIndexingService _indexingService;
    private readonly ILogger _logger;
    private readonly string _dataPath;

    public FileIndexingBackgroundService(IFileIndexingService indexingService, ILogger logger, IWebHostEnvironment env)
    {
        _indexingService = indexingService;
        _logger = logger;
        _dataPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "data"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.Information("Starting file indexing for directory: {DataPath}", _dataPath);
            await _indexingService.IndexFilesAsync(_dataPath);
            _logger.Information("File indexing completed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "File indexing failed");
        }
    }
}