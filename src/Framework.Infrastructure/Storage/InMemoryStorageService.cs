using System.Collections.Concurrent;
using System.Security.Cryptography;
using Framework.Application.Storage;

namespace Framework.Infrastructure.Storage;

/// <summary>
/// In-memory storage implementation for testing
/// </summary>
public class InMemoryStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, InMemoryFile> _files = new();

    public Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        Stream content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var bytes = ms.ToArray();

            return UploadAsync(path, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromResult(StorageResult<StoredFile>.Failure(ex.Message));
        }
    }

    public Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        byte[] content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedPath = NormalizePath(path);

            if (_files.ContainsKey(normalizedPath) && options?.Overwrite == false)
            {
                return Task.FromResult(StorageResult<StoredFile>.Failure("File already exists"));
            }

            var hash = Convert.ToHexString(MD5.HashData(content)).ToLowerInvariant();
            var now = DateTime.UtcNow;

            var file = new InMemoryFile
            {
                Path = normalizedPath,
                Content = content,
                ContentType = options?.ContentType ?? ContentTypeHelper.GetContentType(path),
                Metadata = options?.Metadata ?? new Dictionary<string, string>(),
                Created = now,
                LastModified = now,
                ETag = hash
            };

            _files[normalizedPath] = file;

            var storedFile = CreateStoredFile(file);
            return Task.FromResult(StorageResult<StoredFile>.Success(storedFile));
        }
        catch (Exception ex)
        {
            return Task.FromResult(StorageResult<StoredFile>.Failure(ex.Message));
        }
    }

    public Task<StorageResult<Stream>> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!_files.TryGetValue(normalizedPath, out var file))
        {
            return Task.FromResult(StorageResult<Stream>.Failure("File not found", "NOT_FOUND"));
        }

        var stream = new MemoryStream(file.Content);
        return Task.FromResult(StorageResult<Stream>.Success((Stream)stream));
    }

    public Task<StorageResult<byte[]>> DownloadBytesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!_files.TryGetValue(normalizedPath, out var file))
        {
            return Task.FromResult(StorageResult<byte[]>.Failure("File not found", "NOT_FOUND"));
        }

        return Task.FromResult(StorageResult<byte[]>.Success(file.Content.ToArray()));
    }

    public Task<StorageResult> DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        _files.TryRemove(normalizedPath, out _);
        return Task.FromResult(StorageResult.Success());
    }

    public Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        return Task.FromResult(_files.ContainsKey(normalizedPath));
    }

    public Task<StorageResult<StoredFile>> GetMetadataAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);

        if (!_files.TryGetValue(normalizedPath, out var file))
        {
            return Task.FromResult(StorageResult<StoredFile>.Failure("File not found", "NOT_FOUND"));
        }

        var storedFile = CreateStoredFile(file);
        return Task.FromResult(StorageResult<StoredFile>.Success(storedFile));
    }

    public Task<StorageResult<IEnumerable<StoredFile>>> ListAsync(
        string path,
        ListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPath = NormalizePath(path);
        var prefix = string.IsNullOrEmpty(normalizedPath) ? "" : normalizedPath + "/";

        var files = _files.Values
            .Where(f =>
            {
                if (string.IsNullOrEmpty(normalizedPath))
                    return true;

                if (!f.Path.StartsWith(prefix))
                    return false;

                if (options?.Recursive != true)
                {
                    // Only include direct children
                    var relativePath = f.Path[prefix.Length..];
                    return !relativePath.Contains('/');
                }

                return true;
            })
            .Select(CreateStoredFile);

        if (!string.IsNullOrEmpty(options?.Pattern))
        {
            var pattern = options.Pattern.Replace("*", ".*").Replace("?", ".");
            var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            files = files.Where(f => regex.IsMatch(f.Name));
        }

        if (options?.MaxResults > 0)
        {
            files = files.Take(options.MaxResults.Value);
        }

        return Task.FromResult(StorageResult<IEnumerable<StoredFile>>.Success(files.ToList()));
    }

    public Task<StorageResult<StoredFile>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var sourceNormalized = NormalizePath(sourcePath);
        var destNormalized = NormalizePath(destinationPath);

        if (!_files.TryGetValue(sourceNormalized, out var sourceFile))
        {
            return Task.FromResult(StorageResult<StoredFile>.Failure("Source file not found", "NOT_FOUND"));
        }

        var newFile = new InMemoryFile
        {
            Path = destNormalized,
            Content = sourceFile.Content.ToArray(),
            ContentType = sourceFile.ContentType,
            Metadata = new Dictionary<string, string>(sourceFile.Metadata),
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            ETag = sourceFile.ETag
        };

        _files[destNormalized] = newFile;

        return Task.FromResult(StorageResult<StoredFile>.Success(CreateStoredFile(newFile)));
    }

    public async Task<StorageResult<StoredFile>> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var copyResult = await CopyAsync(sourcePath, destinationPath, cancellationToken);
        if (!copyResult.IsSuccess)
        {
            return copyResult;
        }

        await DeleteAsync(sourcePath, cancellationToken);
        return copyResult;
    }

    public Task<StorageResult<string>> GetPublicUrlAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StorageResult<string>.Failure(
            "Public URLs are not supported for in-memory storage", "NOT_SUPPORTED"));
    }

    public Task<StorageResult<string>> GetSignedUrlAsync(
        string path,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(StorageResult<string>.Failure(
            "Signed URLs are not supported for in-memory storage", "NOT_SUPPORTED"));
    }

    /// <summary>
    /// Gets the count of stored files (for testing)
    /// </summary>
    public int FileCount => _files.Count;

    /// <summary>
    /// Clears all files (for testing)
    /// </summary>
    public void Clear() => _files.Clear();

    /// <summary>
    /// Gets all file paths (for testing)
    /// </summary>
    public IEnumerable<string> GetAllPaths() => _files.Keys;

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static StoredFile CreateStoredFile(InMemoryFile file)
    {
        return new StoredFile
        {
            Path = file.Path,
            Name = Path.GetFileName(file.Path),
            Extension = Path.GetExtension(file.Path),
            Size = file.Content.Length,
            ContentType = file.ContentType,
            LastModified = file.LastModified,
            Created = file.Created,
            ETag = file.ETag,
            Metadata = new Dictionary<string, string>(file.Metadata),
            IsDirectory = false
        };
    }

    private class InMemoryFile
    {
        public string Path { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public string? ETag { get; set; }
    }
}
