using FileSearchService.Application.DTOs;
using FileSearchService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using tryAGI.OpenAI;
using ILogger = Serilog.ILogger;

namespace FileSearchService.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger _logger;

    public SearchController(ISearchService searchService, IFileUploadService fileUploadService, ILogger logger, IWebHostEnvironment env)
    {
        _searchService = searchService;
        _fileUploadService = fileUploadService;
        _logger = logger;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
    {
        try
        {
            _logger.Information("Processing search request with query: {Query}, limit: {Limit}", request.Query, request.Limit);

            request.Validate();
            var results = await _searchService.SearchAsync(request.Query, request.Limit);

            var response = new
            {
                Query = request.Query,
                Limit = request.Limit,
                TotalResults = results.Count,
                Results = results
            };

            _logger.Information("Found {ResultCount} results for query: {Query}", results.Count, request.Query);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.Warning("Invalid search request: {ErrorMessage}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing search request for query: {Query}", request.Query);
            return StatusCode(500, new { Error = "An unexpected error occurred during search." });
        }
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex([FromServices] IFileIndexingService indexingService, [FromServices] IWebHostEnvironment env)
    {
        try
        {
            _logger.Information("Manual reindexing triggered");
            string dataPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "data"));
            await indexingService.IndexFilesAsync(dataPath);
            _logger.Information("Manual reindexing completed");
            return Ok(new { Message = "Reindexing completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Manual reindexing failed");
            return StatusCode(500, new { Error = "An unexpected error occurred during reindexing." });
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        try
        {
            string dataPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "data"));
            var response = await _fileUploadService.UploadFileAsync(file, dataPath);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.Warning("Invalid file upload request: {ErrorMessage}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error uploading file: {FileName}", file?.FileName ?? "Unknown");
            return StatusCode(500, new { Error = "An unexpected error occurred during file upload." });
        }
    }
}