using FileSearchService.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace FileSearchService.Application.Interfaces;

public interface IFileUploadService
{
    Task<FileUploadResponseDto> UploadFileAsync(IFormFile file, string dataPath);
}