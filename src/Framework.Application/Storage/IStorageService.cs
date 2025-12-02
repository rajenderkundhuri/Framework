namespace Framework.Application.Storage;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a file
    /// </summary>
    /// <param name="path">Storage path (e.g., "uploads/images/photo.jpg")</param>
    /// <param name="content">File content stream</param>
    /// <param name="options">Upload options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the stored file info</returns>
    Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        Stream content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a file from bytes
    /// </summary>
    Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        byte[] content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file
    /// </summary>
    /// <param name="path">Storage path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content stream</returns>
    Task<StorageResult<Stream>> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file as bytes
    /// </summary>
    Task<StorageResult<byte[]>> DownloadBytesAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file
    /// </summary>
    Task<StorageResult> DeleteAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file metadata
    /// </summary>
    Task<StorageResult<StoredFile>> GetMetadataAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a directory
    /// </summary>
    Task<StorageResult<IEnumerable<StoredFile>>> ListAsync(
        string path,
        ListOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file
    /// </summary>
    Task<StorageResult<StoredFile>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file
    /// </summary>
    Task<StorageResult<StoredFile>> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public URL for a file (if supported)
    /// </summary>
    Task<StorageResult<string>> GetPublicUrlAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a temporary signed URL for a file (if supported)
    /// </summary>
    Task<StorageResult<string>> GetSignedUrlAsync(
        string path,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a stored file
/// </summary>
public class StoredFile
{
    /// <summary>
    /// File storage path
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Content type (MIME type)
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// File hash/ETag
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Whether the file is a directory
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Public URL (if available)
    /// </summary>
    public string? PublicUrl { get; set; }
}

/// <summary>
/// Options for file upload
/// </summary>
public class UploadOptions
{
    /// <summary>
    /// Content type override
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Custom metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Whether to overwrite existing file
    /// </summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>
    /// Cache control header
    /// </summary>
    public string? CacheControl { get; set; }

    /// <summary>
    /// Content disposition
    /// </summary>
    public string? ContentDisposition { get; set; }

    /// <summary>
    /// Whether file should be publicly accessible
    /// </summary>
    public bool IsPublic { get; set; }
}

/// <summary>
/// Options for listing files
/// </summary>
public class ListOptions
{
    /// <summary>
    /// Whether to include subdirectories
    /// </summary>
    public bool Recursive { get; set; }

    /// <summary>
    /// Maximum number of files to return
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// File pattern filter (e.g., "*.jpg")
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Continuation token for pagination
    /// </summary>
    public string? ContinuationToken { get; set; }
}

/// <summary>
/// Result of a storage operation
/// </summary>
public class StorageResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static StorageResult Success() => new() { IsSuccess = true };

    public static StorageResult Failure(string message, string? code = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ErrorCode = code
    };
}

/// <summary>
/// Result of a storage operation with data
/// </summary>
public class StorageResult<T> : StorageResult
{
    public T? Data { get; set; }

    public static StorageResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public new static StorageResult<T> Failure(string message, string? code = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        ErrorCode = code
    };
}
