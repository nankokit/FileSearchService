namespace FileSearchService.Application.DTOs;

public class FileUploadResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}