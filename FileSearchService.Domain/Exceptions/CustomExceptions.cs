using Microsoft.AspNetCore.Http;

namespace FileSearchService.Domain.Exceptions;

public class FileNotFoundException : BaseCustomException
{
    public FileNotFoundException(string filePath)
        : base($"File not found: {filePath}", StatusCodes.Status404NotFound, "FILE_NOT_FOUND")
    {
    }
}

public class DirectoryNotFoundException : BaseCustomException
{
    public DirectoryNotFoundException(string directoryPath)
        : base($"Directory not found: {directoryPath}", StatusCodes.Status404NotFound, "DIRECTORY_NOT_FOUND")
    {
    }
}

public class InvalidFileTypeException : BaseCustomException
{
    public InvalidFileTypeException(string fileName)
        : base($"Invalid file type: {fileName}. Only .txt files are allowed.", StatusCodes.Status400BadRequest, "INVALID_FILE_TYPE")
    {
    }
}

public class FileAlreadyExistsException : BaseCustomException
{
    public FileAlreadyExistsException(string fileName)
        : base($"File already exists: {fileName}", StatusCodes.Status409Conflict, "FILE_ALREADY_EXISTS")
    {
    }
}

public class IndexingFailedException : BaseCustomException
{
    public IndexingFailedException(string fileName, string details = "")
        : base($"Failed to index file: {fileName}. {details}", StatusCodes.Status500InternalServerError, "INDEXING_FAILED")
    {
    }
}

public class EmbeddingGenerationFailedException : BaseCustomException
{
    public EmbeddingGenerationFailedException(string details)
        : base($"Failed to generate embedding: {details}", StatusCodes.Status500InternalServerError, "EMBEDDING_FAILED")
    {
    }
}

public class QdrantOperationFailedException : BaseCustomException
{
    public QdrantOperationFailedException(string operation, string details)
        : base($"Qdrant {operation} failed: {details}", StatusCodes.Status500InternalServerError, "QDRANT_OPERATION_FAILED")
    {
    }
}

public class SearchFailedException : BaseCustomException
{
    public SearchFailedException(string query, string details)
        : base($"Search failed for query '{query}': {details}", StatusCodes.Status500InternalServerError, "SEARCH_FAILED")
    {
    }
}

public class HuggingFaceApiException : BaseCustomException
{
    public HuggingFaceApiException(string message, string details)
        : base($"Hugging Face API error: {message}. {details}", StatusCodes.Status401Unauthorized, "HF_API_ERROR")
    {
    }
}