using Framework.Application.Storage;
using Framework.Infrastructure.Storage;
using Shouldly;

namespace Framework.Infrastructure.Tests.Storage;

public class InMemoryStorageServiceTests
{
    private readonly InMemoryStorageService _storage;

    public InMemoryStorageServiceTests()
    {
        _storage = new InMemoryStorageService();
    }

    [Fact]
    public async Task UploadAsync_ShouldStoreFile()
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
        _storage.FileCount.ShouldBe(1);
    }

    [Fact]
    public async Task UploadAsync_WithStream_ShouldStoreFile()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var result = await _storage.UploadAsync("test.txt", stream);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Size.ShouldBe(content.Length);
    }

    [Fact]
    public async Task UploadAsync_WithContentType_ShouldSetContentType()
    {
        // Arrange
        var content = "test"u8.ToArray();
        var options = new UploadOptions { ContentType = "text/plain" };

        // Act
        var result = await _storage.UploadAsync("test.txt", content, options);

        // Assert
        result.Data!.ContentType.ShouldBe("text/plain");
    }

    [Fact]
    public async Task UploadAsync_WithMetadata_ShouldStoreMetadata()
    {
        // Arrange
        var content = "test"u8.ToArray();
        var options = new UploadOptions
        {
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Act
        var result = await _storage.UploadAsync("test.txt", content, options);

        // Assert
        result.Data!.Metadata["key"].ShouldBe("value");
    }

    [Fact]
    public async Task UploadAsync_WithOverwriteFalse_ShouldFailIfExists()
    {
        // Arrange
        var content = "test"u8.ToArray();
        await _storage.UploadAsync("test.txt", content);
        var options = new UploadOptions { Overwrite = false };

        // Act
        var result = await _storage.UploadAsync("test.txt", content, options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("File already exists");
    }

    [Fact]
    public async Task UploadAsync_WithOverwriteTrue_ShouldOverwrite()
    {
        // Arrange
        await _storage.UploadAsync("test.txt", "old"u8.ToArray());
        var newContent = "new content"u8.ToArray();

        // Act
        var result = await _storage.UploadAsync("test.txt", newContent);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Size.ShouldBe(newContent.Length);
        _storage.FileCount.ShouldBe(1);
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
    public async Task DownloadBytesAsync_WhenNotFound_ShouldReturnFailure()
    {
        // Act
        var result = await _storage.DownloadBytesAsync("nonexistent.txt");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        await _storage.UploadAsync("test.txt", "content"u8.ToArray());

        // Act
        var result = await _storage.DeleteAsync("test.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _storage.FileCount.ShouldBe(0);
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
        await _storage.UploadAsync("test.txt", "content"u8.ToArray());

        // Act
        var exists = await _storage.ExistsAsync("test.txt");

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
        await _storage.UploadAsync("test.txt", content);

        // Act
        var result = await _storage.GetMetadataAsync("test.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Path.ShouldBe("test.txt");
        result.Data.Name.ShouldBe("test.txt");
        result.Data.Size.ShouldBe(content.Length);
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
        await _storage.UploadAsync("uploads/file1.txt", "content1"u8.ToArray());
        await _storage.UploadAsync("uploads/file2.txt", "content2"u8.ToArray());
        await _storage.UploadAsync("other/file3.txt", "content3"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("uploads");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var files = result.Data!.ToList();
        files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_WithRecursive_ShouldIncludeSubdirectories()
    {
        // Arrange
        await _storage.UploadAsync("uploads/file1.txt", "content1"u8.ToArray());
        await _storage.UploadAsync("uploads/sub/file2.txt", "content2"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("uploads", new ListOptions { Recursive = true });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var files = result.Data!.ToList();
        files.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_WithoutRecursive_ShouldExcludeSubdirectories()
    {
        // Arrange
        await _storage.UploadAsync("uploads/file1.txt", "content1"u8.ToArray());
        await _storage.UploadAsync("uploads/sub/file2.txt", "content2"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("uploads", new ListOptions { Recursive = false });

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var files = result.Data!.ToList();
        files.Count.ShouldBe(1);
        files[0].Name.ShouldBe("file1.txt");
    }

    [Fact]
    public async Task ListAsync_WithMaxResults_ShouldLimitResults()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            await _storage.UploadAsync($"test/file{i}.txt", "content"u8.ToArray());
        }

        // Act
        var result = await _storage.ListAsync("test", new ListOptions { MaxResults = 5, Recursive = true });

        // Assert
        result.Data!.Count().ShouldBe(5);
    }

    [Fact]
    public async Task ListAsync_WithPattern_ShouldFilterFiles()
    {
        // Arrange
        await _storage.UploadAsync("uploads/photo.jpg", "content"u8.ToArray());
        await _storage.UploadAsync("uploads/document.pdf", "content"u8.ToArray());
        await _storage.UploadAsync("uploads/image.jpg", "content"u8.ToArray());

        // Act
        var result = await _storage.ListAsync("uploads", new ListOptions { Pattern = "*.jpg", Recursive = true });

        // Assert
        var files = result.Data!.ToList();
        files.Count.ShouldBe(2);
        files.ShouldAllBe(f => f.Extension == ".jpg");
    }

    [Fact]
    public async Task CopyAsync_ShouldCopyFile()
    {
        // Arrange
        var content = "original content"u8.ToArray();
        await _storage.UploadAsync("source.txt", content);

        // Act
        var result = await _storage.CopyAsync("source.txt", "dest.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Path.ShouldBe("dest.txt");
        _storage.FileCount.ShouldBe(2);

        var destContent = await _storage.DownloadBytesAsync("dest.txt");
        destContent.Data.ShouldBe(content);
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
        await _storage.UploadAsync("source.txt", content);

        // Act
        var result = await _storage.MoveAsync("source.txt", "dest.txt");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Path.ShouldBe("dest.txt");
        _storage.FileCount.ShouldBe(1);
        (await _storage.ExistsAsync("source.txt")).ShouldBeFalse();
        (await _storage.ExistsAsync("dest.txt")).ShouldBeTrue();
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
    public void Clear_ShouldRemoveAllFiles()
    {
        // Arrange
        _storage.UploadAsync("file1.txt", "content"u8.ToArray()).Wait();
        _storage.UploadAsync("file2.txt", "content"u8.ToArray()).Wait();

        // Act
        _storage.Clear();

        // Assert
        _storage.FileCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetAllPaths_ShouldReturnAllPaths()
    {
        // Arrange
        await _storage.UploadAsync("file1.txt", "content"u8.ToArray());
        await _storage.UploadAsync("dir/file2.txt", "content"u8.ToArray());

        // Act
        var paths = _storage.GetAllPaths().ToList();

        // Assert
        paths.Count.ShouldBe(2);
        paths.ShouldContain("file1.txt");
        paths.ShouldContain("dir/file2.txt");
    }

    [Fact]
    public async Task PathNormalization_ShouldHandleBackslashes()
    {
        // Arrange
        await _storage.UploadAsync("uploads\\images\\test.jpg", "content"u8.ToArray());

        // Act
        var exists = await _storage.ExistsAsync("uploads/images/test.jpg");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task PathNormalization_ShouldTrimLeadingSlashes()
    {
        // Arrange
        await _storage.UploadAsync("/uploads/test.txt", "content"u8.ToArray());

        // Act
        var exists = await _storage.ExistsAsync("uploads/test.txt");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Upload_ShouldGenerateETag()
    {
        // Arrange
        var content = "test content"u8.ToArray();

        // Act
        var result = await _storage.UploadAsync("test.txt", content);

        // Assert
        result.Data!.ETag.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Upload_SameContent_ShouldHaveSameETag()
    {
        // Arrange
        var content = "test content"u8.ToArray();

        // Act
        var result1 = await _storage.UploadAsync("file1.txt", content);
        var result2 = await _storage.UploadAsync("file2.txt", content);

        // Assert
        result1.Data!.ETag.ShouldBe(result2.Data!.ETag);
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
}
