namespace FileSearchService.Application.Interfaces;

public interface IFileIndexingService
{
    Task IndexFilesAsync(string directoryPath);
}
