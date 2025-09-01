using Microsoft.AspNetCore.Http;

namespace FileSearchService.Domain.Exceptions;

public class BaseCustomException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public BaseCustomException(string message, int statusCode, string errorCode = "GENERAL_ERROR")
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}