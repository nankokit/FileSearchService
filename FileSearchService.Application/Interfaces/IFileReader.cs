using System.IO;

namespace FileSearchService.Application.Interfaces;

public interface IFileReader
{
    List<string> GetTextFiles(string directoryPath);
    Task<string> ReadFileAsync(string filePath);
    Task WriteFileAsync(string filePath, Stream content);
}