using FileSearchService.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Net;
using System.Text.Json;

namespace FileSearchService.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new
        {
            ErrorCode = "GENERAL_ERROR",
            Message = "An unexpected error occurred.",
            StatusCode = StatusCodes.Status500InternalServerError
        };

        switch (exception)
        {
            case BaseCustomException BaseCustomException:
                errorResponse = new
                {
                    BaseCustomException.ErrorCode,
                    BaseCustomException.Message,
                    BaseCustomException.StatusCode
                };
                _logger.Warning("API error: {ErrorCode} - {Message} at {RequestPath}", BaseCustomException.ErrorCode, BaseCustomException.Message, context.Request.Path);
                break;

            case HttpRequestException httpEx:
                errorResponse = new
                {
                    ErrorCode = "EXTERNAL_API_ERROR",
                    Message = httpEx.Message,
                    StatusCode = StatusCodes.Status502BadGateway
                };
                _logger.Error(httpEx, "External API request failed at {RequestPath}", context.Request.Path);
                break;

            default:
                _logger.Error(exception, "Unhandled exception occurred at {RequestPath}", context.Request.Path);
                break;
        }

        response.StatusCode = errorResponse.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}