using Framework.Application.Storage;
using Framework.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Storage;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _basePath;
    private readonly IOptions<StorageSettings> _options;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly LocalStorageService _storage;

    public LocalStorageServiceTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"storage_test_{Guid.NewGuid()}");
        var settings = new StorageSettings
        {
            LocalBasePath = _basePath,
            AutoCreateDirectories = true
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<LocalStorageService>>();
        _storage = new LocalStorageService(_options, _logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, true);
        }
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateFile()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();

        // Act
        var result = await _storage.UploadAsync("test.txt", content);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Path.ShouldBe("test.txt");
        result.Data.Size.ShouldBe(content.Length);
        File.Exists(Path.Combine(_basePath, "test.txt")).ShouldBeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithStream_ShouldCreateFile()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var result = await _storage.UploadAsync("stream-test.txt", stream);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var savedContent = await File.ReadAllBytesAsync(Path.Combine(_basePath, "stream-test.txt"));
        savedContent.ShouldBe(content);
    }

    [Fact]
    public async Task UploadAsync_ShouldCreateDirectories()
    {
        // Arrange
        var content = "test"u8.ToArray();

        // Act
        var result = await _storage.UploadAsync("deep/nested/dir/file.txt", content);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Directory.Exists(Path.Combine(_basePath, "deep/nested/dir")).ShouldBeTrue();
    }

    [Fact]
    public async Task UploadAsync_WithOverwriteFalse_ShouldFailIfExists()
    {
        // Arrange
        var content = "test"u8.ToArray();
        await _storage.UploadAsync("existing.txt", content);
        var options = new UploadOptions { Overwrite = false };

        // Act
        var result = await _storage.UploadAsync("existing.txt", content, options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("File already exists");
    }

    [Fact]
    public async Task UploadAsync_WithBlockedExtension_ShouldFail()
    {
        // Arrange
        var settings = new StorageSettings
        {
            LocalBasePath = _basePath,
            BlockedExtensions = new List<string> { ".exe" }
        };
        var storage = new LocalStorageService(Options.Create(settings), _logger);

        // Act
        var result = await storage.UploadAsync("malware.exe", "test"u8.ToArray());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain(".exe");
    }

    [Fact]
    public async Task UploadAsync_WithAllowedExtensions_ShouldEnforceList()
    {
        // Arrange
        var settings = new StorageSettings
        {
            LocalBasePath = _basePath,
            AllowedExtensions = new List<string> { ".jpg", ".png" }
        };
        var storage = new LocalStorageService(Options.Create(settings), _logger);

        // Act
        var allowed = await storage.UploadAsync("photo.jpg", "test"u8.ToArray());
        var notAllowed = await storage.UploadAsync("doc.pdf", "test"u8.ToArray());

        // Assert
        allowed.IsSuccess.ShouldBeTrue();
        notAllowed.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task UploadAsync_WithMaxFileSize_ShouldEnforceLimit()
    {
        // Arrange
        var settings = new StorageSettings
        {
            LocalBasePath = _basePath,
            MaxFileSize = 100
        };
        var storage = new LocalStorageService(Options.Create(settings), _logger);
        var largeContent = new byte[200];

        // Act
        var result = await storage.UploadAsync("large.bin", largeContent);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("exceeds maximum");
    }

    [Fact]
    public async Task UploadAsync_ShouldGenerateETag()
    {
        // Arrange
        var content = "test content"u8.ToArray();

        // Act
        var result = await _storage.UploadAsync("test.txt", content);

        // Assert
        result.Data!.ETag.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnStream()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        await _storage.UploadAsync("test.txt", content);

        // Act
        var result = await _storage.DownloadAsync("test.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        using var ms = new MemoryStream();
        await result.Data!.CopyToAsync(ms);
        ms.ToArray().ShouldBe(content);
        result.Data.Dispose();
    }

    [Fact]
    public async Task DownloadAsync_WhenNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _storage.DownloadAsync("nonexistent.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task DownloadBytesAsync_ShouldReturnBytes()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        await _storage.UploadAsync("test.txt", content);

        // Act
        var result = await _storage.DownloadBytesAsync("test.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(content);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        await _storage.UploadAsync("to-delete.txt", "content"u8.ToArray());
        var filePath = Path.Combine(_basePath, "to-delete.txt");
        File.Exists(filePath).ShouldBeTrue();

        // Act
        var result = await _storage.DeleteAsync("to-delete.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        File.Exists(filePath).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldSucceed()
    {
        // Act
        var result = await _storage.DeleteAsync("nonexistent.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        await _storage.UploadAsync("exists.txt", "content"u8.ToArray());

        // Act
        var exists = await _storage.ExistsAsync("exists.txt");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Act
        var exists = await _storage.ExistsAsync("nonexistent.txt");

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldReturnFileInfo()
    {
        // Arrange
        var content = "test content"u8.ToArray();
        await _storage.UploadAsync("metadata-test.txt", content);

        // Act
        var result = await _storage.GetMetadataAsync("metadata-test.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Path.ShouldBe("metadata-test.txt");
        result.Data.Name.ShouldBe("metadata-test.txt");
        result.Data.Extension.ShouldBe(".txt");
        result.Data.Size.ShouldBe(content.Length);
        result.Data.ContentType.ShouldBe("text/plain");
    }

    [Fact]
    public async Task GetMetadataAsync_WhenNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _storage.GetMetadataAsync("nonexistent.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task ListAsync_ShouldReturnFiles()
    {
        // Arrange
        await _storage.UploadAsync("list/file1.txt", "content1"u8.ToArray());
        await _storage.UploadAsync("list/file2.txt", "content2"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("list");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var files = result.Data!.ToList();
        files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_WithRecursive_ShouldIncludeSubdirectories()
    {
        // Arrange
        await _storage.UploadAsync("recursive/file1.txt", "content1"u8.ToArray());
        await _storage.UploadAsync("recursive/sub/file2.txt", "content2"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("recursive", new ListOptions { Recursive = true });

        // Assert
        var files = result.Data!.ToList();
        files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_WithPattern_ShouldFilterFiles()
    {
        // Arrange
        await _storage.UploadAsync("pattern/image.jpg", "content"u8.ToArray());
        await _storage.UploadAsync("pattern/document.pdf", "content"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("pattern", new ListOptions { Pattern = "*.jpg" });

        // Assert
        var files = result.Data!.ToList();
        files.Count.ShouldBe(1);
        files[0].Extension.ShouldBe(".jpg");
    }

    [Fact]
    public async Task ListAsync_WithMaxResults_ShouldLimitResults()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _storage.UploadAsync($"max/file{i}.txt", "content"u8.ToArray());
        }

        // Act
        var result = await _storage.ListAsync("max", new ListOptions { MaxResults = 5 });

        // Assert
        result.Data!.Count().ShouldBe(5);
    }

    [Fact]
    public async Task ListAsync_WhenDirectoryNotFound_ShouldReturnEmpty()
    {
        // Act
        var result = await _storage.ListAsync("nonexistent");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyFile()
    {
        // Arrange
        var content = "original content"u8.ToArray();
        await _storage.UploadAsync("copy/source.txt", content);

        // Act
        var result = await _storage.CopyAsync("copy/source.txt", "copy/dest.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await _storage.ExistsAsync("copy/source.txt")).ShouldBeTrue();
        (await _storage.ExistsAsync("copy/dest.txt")).ShouldBeTrue();
    }

    [Fact]
    public async Task CopyAsync_WhenSourceNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _storage.CopyAsync("nonexistent.txt", "dest.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task MoveAsync_ShouldMoveFile()
    {
        // Arrange
        var content = "original content"u8.ToArray();
        await _storage.UploadAsync("move/source.txt", content);

        // Act
        var result = await _storage.MoveAsync("move/source.txt", "move/dest.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await _storage.ExistsAsync("move/source.txt")).ShouldBeFalse();
        (await _storage.ExistsAsync("move/dest.txt")).ShouldBeTrue();
    }

    [Fact]
    public async Task MoveAsync_WhenSourceNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _storage.MoveAsync("nonexistent.txt", "dest.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task GetPublicUrlAsync_ShouldReturnNotSupported()
    {
        // Arrange
        await _storage.UploadAsync("test.txt", "content"u8.ToArray());

        // Act
        var result = await _storage.GetPublicUrlAsync("test.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_SUPPORTED");
    }

    [Fact]
    public async Task GetSignedUrlAsync_ShouldReturnNotSupported()
    {
        // Arrange
        await _storage.UploadAsync("test.txt", "content"u8.ToArray());

        // Act
        var result = await _storage.GetSignedUrlAsync("test.txt", TimeSpan.FromMinutes(5));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_SUPPORTED");
    }

    [Fact]
    public async Task PathTraversal_ShouldBeBlocked()
    {
        // Arrange
        await _storage.UploadAsync("safe/file.txt", "content"u8.ToArray());

        // Act - attempting path traversal should either throw or fail gracefully
        try
        {
            var result = await _storage.DownloadAsync("../../../etc/passwd");
            // If it doesn't throw, it should return failure (file not found)
            result.IsSuccess.ShouldBeFalse();
        }
        catch (UnauthorizedAccessException)
        {
            // Expected - path traversal blocked
        }
    }

    [Fact]
    public async Task ContentTypeDetection_ShouldDetectFromExtension()
    {
        // Arrange & Act
        await _storage.UploadAsync("photo.jpg", "content"u8.ToArray());
        await _storage.UploadAsync("document.pdf", "content"u8.ToArray());
        await _storage.UploadAsync("page.html", "content"u8.ToArray());

        // Assert
        var jpg = await _storage.GetMetadataAsync("photo.jpg");
        var pdf = await _storage.GetMetadataAsync("document.pdf");
        var html = await _storage.GetMetadataAsync("page.html");

        jpg.Data!.ContentType.ShouldBe("image/jpeg");
        pdf.Data!.ContentType.ShouldBe("application/pdf");
        html.Data!.ContentType.ShouldBe("text/html");
    }

    [Fact]
    public async Task UploadAsync_WithCustomContentType_ShouldUseProvided()
    {
        // Arrange
        var options = new UploadOptions { ContentType = "application/custom" };

        // Act
        var result = await _storage.UploadAsync("test.txt", "content"u8.ToArray(), options);

        // Assert
        result.Data!.ContentType.ShouldBe("application/custom");
    }
}
