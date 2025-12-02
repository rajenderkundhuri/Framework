using Framework.Application.Storage;
using Shouldly;

namespace Framework.Application.Tests.Storage;

public class StorageResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = StorageResult.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        result.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Act
        var result = StorageResult.Failure("File not found");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("File not found");
    }

    [Fact]
    public void Failure_WithErrorCode_ShouldSetErrorCode()
    {
        // Act
        var result = StorageResult.Failure("Access denied", "FORBIDDEN");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Access denied");
        result.ErrorCode.ShouldBe("FORBIDDEN");
    }
}

public class StorageResultGenericTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResultWithData()
    {
        // Arrange
        var file = new StoredFile { Path = "test.txt", Size = 100 };

        // Act
        var result = StorageResult<StoredFile>.Success(file);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Path.ShouldBe("test.txt");
    }

    [Fact]
    public void Failure_ShouldCreateFailureResultWithoutData()
    {
        // Act
        var result = StorageResult<StoredFile>.Failure("Not found", "NOT_FOUND");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Data.ShouldBeNull();
        result.ErrorMessage.ShouldBe("Not found");
        result.ErrorCode.ShouldBe("NOT_FOUND");
    }
}

public class StoredFileTests
{
    [Fact]
    public void NewStoredFile_ShouldHaveDefaults()
    {
        // Act
        var file = new StoredFile();

        // Assert
        file.Path.ShouldBe(string.Empty);
        file.Name.ShouldBe(string.Empty);
        file.Extension.ShouldBe(string.Empty);
        file.Size.ShouldBe(0);
        file.ContentType.ShouldBe("application/octet-stream");
        file.Metadata.ShouldNotBeNull();
        file.Metadata.ShouldBeEmpty();
        file.IsDirectory.ShouldBeFalse();
        file.PublicUrl.ShouldBeNull();
        file.ETag.ShouldBeNull();
    }

    [Fact]
    public void StoredFile_ShouldAllowSettingAllProperties()
    {
        // Act
        var now = DateTime.UtcNow;
        var file = new StoredFile
        {
            Path = "uploads/test.jpg",
            Name = "test.jpg",
            Extension = ".jpg",
            Size = 12345,
            ContentType = "image/jpeg",
            LastModified = now,
            Created = now,
            ETag = "abc123",
            PublicUrl = "https://example.com/test.jpg",
            IsDirectory = false,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Assert
        file.Path.ShouldBe("uploads/test.jpg");
        file.Name.ShouldBe("test.jpg");
        file.Extension.ShouldBe(".jpg");
        file.Size.ShouldBe(12345);
        file.ContentType.ShouldBe("image/jpeg");
        file.LastModified.ShouldBe(now);
        file.Created.ShouldBe(now);
        file.ETag.ShouldBe("abc123");
        file.PublicUrl.ShouldBe("https://example.com/test.jpg");
        file.IsDirectory.ShouldBeFalse();
        file.Metadata["key"].ShouldBe("value");
    }
}

public class UploadOptionsTests
{
    [Fact]
    public void NewUploadOptions_ShouldHaveDefaults()
    {
        // Act
        var options = new UploadOptions();

        // Assert
        options.ContentType.ShouldBeNull();
        options.Metadata.ShouldNotBeNull();
        options.Metadata.ShouldBeEmpty();
        options.Overwrite.ShouldBeTrue();
        options.CacheControl.ShouldBeNull();
        options.ContentDisposition.ShouldBeNull();
        options.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public void UploadOptions_ShouldAllowSettingProperties()
    {
        // Act
        var options = new UploadOptions
        {
            ContentType = "image/png",
            Overwrite = false,
            CacheControl = "max-age=3600",
            ContentDisposition = "attachment; filename=test.png",
            IsPublic = true,
            Metadata = new Dictionary<string, string> { ["custom"] = "value" }
        };

        // Assert
        options.ContentType.ShouldBe("image/png");
        options.Overwrite.ShouldBeFalse();
        options.CacheControl.ShouldBe("max-age=3600");
        options.ContentDisposition.ShouldBe("attachment; filename=test.png");
        options.IsPublic.ShouldBeTrue();
        options.Metadata["custom"].ShouldBe("value");
    }
}

public class ListOptionsTests
{
    [Fact]
    public void NewListOptions_ShouldHaveDefaults()
    {
        // Act
        var options = new ListOptions();

        // Assert
        options.Recursive.ShouldBeFalse();
        options.MaxResults.ShouldBeNull();
        options.Pattern.ShouldBeNull();
        options.ContinuationToken.ShouldBeNull();
    }

    [Fact]
    public void ListOptions_ShouldAllowSettingProperties()
    {
        // Act
        var options = new ListOptions
        {
            Recursive = true,
            MaxResults = 100,
            Pattern = "*.jpg",
            ContinuationToken = "token123"
        };

        // Assert
        options.Recursive.ShouldBeTrue();
        options.MaxResults.ShouldBe(100);
        options.Pattern.ShouldBe("*.jpg");
        options.ContinuationToken.ShouldBe("token123");
    }
}
