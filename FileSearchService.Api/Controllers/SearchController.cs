using FileSearchService.Application.DTOs;
using FileSearchService.Application.Interfaces;
using FileSearchService.Domain.Exceptions;
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

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        string dataPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "data"));
        var response = await _fileUploadService.UploadFileAsync(file, dataPath);
        return Ok(response);

    }

}