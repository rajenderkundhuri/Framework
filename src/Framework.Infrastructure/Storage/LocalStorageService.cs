using System.Security.Cryptography;
using Framework.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Storage;

/// <summary>
/// Local file system storage implementation
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _basePath;

    public LocalStorageService(
        IOptions<StorageSettings> settings,
        ILogger<LocalStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _basePath = Path.GetFullPath(_settings.LocalBasePath);

        if (_settings.AutoCreateDirectories && !Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        Stream content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationError = ValidateUpload(path, content.Length);
            if (validationError != null)
            {
                return StorageResult<StoredFile>.Failure(validationError);
            }

            var fullPath = GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                if (_settings.AutoCreateDirectories)
                {
                    Directory.CreateDirectory(directory);
                }
                else
                {
                    return StorageResult<StoredFile>.Failure($"Directory does not exist: {directory}");
                }
            }

            if (File.Exists(fullPath) && options?.Overwrite == false)
            {
                return StorageResult<StoredFile>.Failure("File already exists");
            }

            // Calculate hash while writing
            string? hash;
            await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using var hashAlgorithm = MD5.Create();
                using var cryptoStream = new CryptoStream(fileStream, hashAlgorithm, CryptoStreamMode.Write);
                await content.CopyToAsync(cryptoStream, cancellationToken);
                await cryptoStream.FlushFinalBlockAsync(cancellationToken);
                hash = Convert.ToHexString(hashAlgorithm.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
            }

            var fileInfo = new FileInfo(fullPath);
            var storedFile = CreateStoredFile(path, fileInfo, options?.ContentType);
            storedFile.ETag = hash;

            _logger.LogInformation("File uploaded: {Path} ({Size} bytes)", path, fileInfo.Length);

            return StorageResult<StoredFile>.Success(storedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {Path}", path);
            return StorageResult<StoredFile>.Failure(ex.Message);
        }
    }

    public async Task<StorageResult<StoredFile>> UploadAsync(
        string path,
        byte[] content,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content);
        return await UploadAsync(path, stream, options, cancellationToken);
    }

    public async Task<StorageResult<Stream>> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return StorageResult<Stream>.Failure("File not found", "NOT_FOUND");
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return StorageResult<Stream>.Success(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {Path}", path);
            return StorageResult<Stream>.Failure(ex.Message);
        }
    }

    public async Task<StorageResult<byte[]>> DownloadBytesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return StorageResult<byte[]>.Failure("File not found", "NOT_FOUND");
            }

            var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
            return StorageResult<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {Path}", path);
            return StorageResult<byte[]>.Failure(ex.Message);
        }
    }

    public Task<StorageResult> DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(StorageResult.Success()); // Already deleted
            }

            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {Path}", path);

            return Task.FromResult(StorageResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", path);
            return Task.FromResult(StorageResult.Failure(ex.Message));
        }
    }

    public Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath) || Directory.Exists(fullPath));
    }

    public Task<StorageResult<StoredFile>> GetMetadataAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(StorageResult<StoredFile>.Failure("File not found", "NOT_FOUND"));
            }

            var fileInfo = new FileInfo(fullPath);
            var storedFile = CreateStoredFile(path, fileInfo);

            return Task.FromResult(StorageResult<StoredFile>.Success(storedFile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata: {Path}", path);
            return Task.FromResult(StorageResult<StoredFile>.Failure(ex.Message));
        }
    }

    public Task<StorageResult<IEnumerable<StoredFile>>> ListAsync(
        string path,
        ListOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult(StorageResult<IEnumerable<StoredFile>>.Success(
                    Enumerable.Empty<StoredFile>()));
            }

            var searchOption = options?.Recursive == true
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var pattern = options?.Pattern ?? "*";
            var files = Directory.GetFiles(fullPath, pattern, searchOption)
                .Select(f =>
                {
                    var relativePath = Path.GetRelativePath(_basePath, f).Replace('\\', '/');
                    return CreateStoredFile(relativePath, new FileInfo(f));
                });

            if (options?.MaxResults > 0)
            {
                files = files.Take(options.MaxResults.Value);
            }

            return Task.FromResult(StorageResult<IEnumerable<StoredFile>>.Success(files.ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files: {Path}", path);
            return Task.FromResult(StorageResult<IEnumerable<StoredFile>>.Failure(ex.Message));
        }
    }

    public async Task<StorageResult<StoredFile>> CopyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFullPath = GetFullPath(sourcePath);
            var destFullPath = GetFullPath(destinationPath);

            if (!File.Exists(sourceFullPath))
            {
                return StorageResult<StoredFile>.Failure("Source file not found", "NOT_FOUND");
            }

            var destDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                if (_settings.AutoCreateDirectories)
                {
                    Directory.CreateDirectory(destDir);
                }
                else
                {
                    return StorageResult<StoredFile>.Failure("Destination directory does not exist");
                }
            }

            File.Copy(sourceFullPath, destFullPath, true);

            var fileInfo = new FileInfo(destFullPath);
            var storedFile = CreateStoredFile(destinationPath, fileInfo);

            _logger.LogInformation("File copied: {Source} -> {Destination}", sourcePath, destinationPath);

            return StorageResult<StoredFile>.Success(storedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file: {Source} -> {Destination}", sourcePath, destinationPath);
            return StorageResult<StoredFile>.Failure(ex.Message);
        }
    }

    public async Task<StorageResult<StoredFile>> MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceFullPath = GetFullPath(sourcePath);
            var destFullPath = GetFullPath(destinationPath);

            if (!File.Exists(sourceFullPath))
            {
                return StorageResult<StoredFile>.Failure("Source file not found", "NOT_FOUND");
            }

            var destDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                if (_settings.AutoCreateDirectories)
                {
                    Directory.CreateDirectory(destDir);
                }
                else
                {
                    return StorageResult<StoredFile>.Failure("Destination directory does not exist");
                }
            }

            File.Move(sourceFullPath, destFullPath, true);

            var fileInfo = new FileInfo(destFullPath);
            var storedFile = CreateStoredFile(destinationPath, fileInfo);

            _logger.LogInformation("File moved: {Source} -> {Destination}", sourcePath, destinationPath);

            return StorageResult<StoredFile>.Success(storedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file: {Source} -> {Destination}", sourcePath, destinationPath);
            return StorageResult<StoredFile>.Failure(ex.Message);
        }
    }

    public Task<StorageResult<string>> GetPublicUrlAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        // Local storage doesn't support public URLs
        return Task.FromResult(StorageResult<string>.Failure(
            "Public URLs are not supported for local storage", "NOT_SUPPORTED"));
    }

    public Task<StorageResult<string>> GetSignedUrlAsync(
        string path,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        // Local storage doesn't support signed URLs
        return Task.FromResult(StorageResult<string>.Failure(
            "Signed URLs are not supported for local storage", "NOT_SUPPORTED"));
    }

    private string GetFullPath(string path)
    {
        // Normalize path separators
        var normalizedPath = path.Replace('\\', '/').TrimStart('/');

        // Prevent directory traversal
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, normalizedPath));
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: path traversal detected");
        }

        return fullPath;
    }

    private string? ValidateUpload(string path, long size)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        // Check blocked extensions
        if (_settings.BlockedExtensions.Contains(extension))
        {
            return $"File extension '{extension}' is not allowed";
        }

        // Check allowed extensions (if specified)
        if (_settings.AllowedExtensions.Any() && !_settings.AllowedExtensions.Contains(extension))
        {
            return $"File extension '{extension}' is not in the allowed list";
        }

        // Check file size
        if (_settings.MaxFileSize > 0 && size > _settings.MaxFileSize)
        {
            return $"File size exceeds maximum allowed size of {_settings.MaxFileSize} bytes";
        }

        return null;
    }

    private StoredFile CreateStoredFile(string path, FileInfo fileInfo, string? contentType = null)
    {
        return new StoredFile
        {
            Path = path,
            Name = fileInfo.Name,
            Extension = fileInfo.Extension,
            Size = fileInfo.Length,
            ContentType = contentType ?? ContentTypeHelper.GetContentType(fileInfo.Name),
            LastModified = fileInfo.LastWriteTimeUtc,
            Created = fileInfo.CreationTimeUtc,
            IsDirectory = false
        };
    }
}
