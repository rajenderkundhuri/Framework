using Framework.Application.Storage;
using Shouldly;

namespace Framework.Application.Tests.Storage;

public class StoragePathBuilderTests
{
    [Fact]
    public void Create_ShouldReturnNewBuilder()
    {
        // Act
        var builder = StoragePathBuilder.Create();

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void WithFileName_ShouldBuildSimplePath()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithFileName("test.txt")
            .Build();

        // Assert
        path.ShouldBe("test.txt");
    }

    [Fact]
    public void WithSegment_ShouldAddPathSegments()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("uploads")
            .WithSegment("images")
            .WithFileName("photo.jpg")
            .Build();

        // Assert
        path.ShouldBe("uploads/images/photo.jpg");
    }

    [Fact]
    public void WithTenant_ShouldAddTenantPrefix()
    {
        // Arrange
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var path = StoragePathBuilder.Create()
            .WithTenant(tenantId)
            .WithSegment("uploads")
            .WithFileName("file.pdf")
            .Build();

        // Assert
        path.ShouldBe("tenants/11111111-1111-1111-1111-111111111111/uploads/file.pdf");
    }

    [Fact]
    public void WithUser_ShouldAddUserSegment()
    {
        // Arrange
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("uploads")
            .WithUser(userId)
            .WithFileName("doc.pdf")
            .Build();

        // Assert
        path.ShouldBe("uploads/users/22222222-2222-2222-2222-222222222222/doc.pdf");
    }

    [Fact]
    public void WithDatePartition_ShouldAddDateSegments()
    {
        // Arrange
        var date = new DateTime(2024, 6, 15);

        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("logs")
            .WithDatePartition(date)
            .WithFileName("app.log")
            .Build();

        // Assert
        path.ShouldBe("logs/2024/06/15/app.log");
    }

    [Fact]
    public void WithDatePartition_WithoutDate_ShouldUseCurrentDate()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("logs")
            .WithDatePartition()
            .WithFileName("app.log")
            .Build();

        // Assert
        var today = DateTime.UtcNow;
        path.ShouldContain($"{today.Year:D4}/{today.Month:D2}/{today.Day:D2}");
    }

    [Fact]
    public void WithUniqueFileName_ShouldGenerateUniqueName()
    {
        // Act
        var path1 = StoragePathBuilder.Create()
            .WithSegment("temp")
            .WithUniqueFileName(".txt")
            .Build();

        var path2 = StoragePathBuilder.Create()
            .WithSegment("temp")
            .WithUniqueFileName(".txt")
            .Build();

        // Assert
        path1.ShouldStartWith("temp/");
        path1.ShouldEndWith(".txt");
        path1.ShouldNotBe(path2); // Should generate different names
    }

    [Fact]
    public void WithUniqueFileName_WithOriginalFileName_ShouldPreserveExtension()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("uploads")
            .WithUniqueFileName("original.jpg")
            .Build();

        // Assert
        path.ShouldStartWith("uploads/");
        path.ShouldEndWith(".jpg");
    }

    [Fact]
    public void WithExtension_ShouldSetExtension()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithUniqueFileName()
            .WithExtension(".pdf")
            .Build();

        // Assert
        path.ShouldEndWith(".pdf");
    }

    [Fact]
    public void WithExtension_WithoutDot_ShouldAddDot()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithUniqueFileName()
            .WithExtension("png")
            .Build();

        // Assert
        path.ShouldEndWith(".png");
    }

    [Fact]
    public void Build_WithoutFileName_ShouldThrow()
    {
        // Arrange
        var builder = StoragePathBuilder.Create()
            .WithSegment("uploads");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void WithSegment_ShouldIgnoreEmptySegments()
    {
        // Act
        var path = StoragePathBuilder.Create()
            .WithSegment("uploads")
            .WithSegment("")
            .WithSegment("  ")
            .WithSegment("images")
            .WithFileName("test.jpg")
            .Build();

        // Assert
        path.ShouldBe("uploads/images/test.jpg");
    }

    [Fact]
    public void ComplexPath_ShouldBuildCorrectly()
    {
        // Arrange
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var date = new DateTime(2024, 3, 20);

        // Act
        var path = StoragePathBuilder.Create()
            .WithTenant(tenantId)
            .WithSegment("uploads")
            .WithUser(userId)
            .WithDatePartition(date)
            .WithFileName("report.pdf")
            .Build();

        // Assert
        path.ShouldBe("tenants/11111111-1111-1111-1111-111111111111/uploads/users/22222222-2222-2222-2222-222222222222/2024/03/20/report.pdf");
    }
}

public class StoragePathsTests
{
    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        // Assert
        StoragePaths.Uploads.ShouldBe("uploads");
        StoragePaths.Images.ShouldBe("images");
        StoragePaths.Documents.ShouldBe("documents");
        StoragePaths.Temp.ShouldBe("temp");
        StoragePaths.Avatars.ShouldBe("avatars");
        StoragePaths.Attachments.ShouldBe("attachments");
        StoragePaths.Exports.ShouldBe("exports");
        StoragePaths.Imports.ShouldBe("imports");
        StoragePaths.Backups.ShouldBe("backups");
        StoragePaths.Logs.ShouldBe("logs");
    }

    [Fact]
    public void UserUploads_ShouldGenerateCorrectPath()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var path = StoragePaths.UserUploads(userId);

        // Assert
        path.ShouldBe("uploads/users/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void UserAvatar_ShouldGenerateCorrectPath()
    {
        // Arrange
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        var path = StoragePaths.UserAvatar(userId);

        // Assert
        path.ShouldBe("avatars/22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void TempFile_ShouldGenerateUniquePath()
    {
        // Act
        var path1 = StoragePaths.TempFile();
        var path2 = StoragePaths.TempFile();

        // Assert
        path1.ShouldStartWith("temp/");
        path1.ShouldNotBe(path2);
    }

    [Fact]
    public void TempFile_WithExtension_ShouldIncludeExtension()
    {
        // Act
        var path = StoragePaths.TempFile(".txt");

        // Assert
        path.ShouldStartWith("temp/");
        path.ShouldEndWith(".txt");
    }

    [Fact]
    public void TempFile_WithExtensionWithoutDot_ShouldAddDot()
    {
        // Act
        var path = StoragePaths.TempFile("csv");

        // Assert
        path.ShouldEndWith(".csv");
    }

    [Fact]
    public void Export_ShouldGenerateDatedPath()
    {
        // Arrange
        var date = new DateTime(2024, 12, 25);

        // Act
        var path = StoragePaths.Export("report.xlsx", date);

        // Assert
        path.ShouldBe("exports/2024/12/report.xlsx");
    }

    [Fact]
    public void Export_WithoutDate_ShouldUseCurrentDate()
    {
        // Act
        var path = StoragePaths.Export("data.csv");

        // Assert
        var today = DateTime.UtcNow;
        path.ShouldBe($"exports/{today.Year:D4}/{today.Month:D2}/data.csv");
    }
}

public class ContentTypeHelperTests
{
    [Theory]
    [InlineData("test.jpg", "image/jpeg")]
    [InlineData("test.jpeg", "image/jpeg")]
    [InlineData("test.png", "image/png")]
    [InlineData("test.gif", "image/gif")]
    [InlineData("test.webp", "image/webp")]
    [InlineData("test.svg", "image/svg+xml")]
    [InlineData("test.pdf", "application/pdf")]
    [InlineData("test.doc", "application/msword")]
    [InlineData("test.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("test.xls", "application/vnd.ms-excel")]
    [InlineData("test.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("test.txt", "text/plain")]
    [InlineData("test.csv", "text/csv")]
    [InlineData("test.html", "text/html")]
    [InlineData("test.json", "application/json")]
    [InlineData("test.xml", "application/xml")]
    [InlineData("test.zip", "application/zip")]
    [InlineData("test.mp3", "audio/mpeg")]
    [InlineData("test.mp4", "video/mp4")]
    [InlineData("test.unknown", "application/octet-stream")]
    public void GetContentType_ShouldReturnCorrectType(string fileName, string expectedType)
    {
        // Act
        var contentType = ContentTypeHelper.GetContentType(fileName);

        // Assert
        contentType.ShouldBe(expectedType);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("text/plain", ".txt")]
    public void GetExtension_ShouldReturnCorrectExtension(string contentType, string expectedExtension)
    {
        // Act
        var extension = ContentTypeHelper.GetExtension(contentType);

        // Assert
        extension.ShouldBe(expectedExtension);
    }

    [Fact]
    public void GetExtension_WithUnknownType_ShouldReturnNull()
    {
        // Act
        var extension = ContentTypeHelper.GetExtension("application/unknown");

        // Assert
        extension.ShouldBeNull();
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("image/gif", true)]
    [InlineData("application/pdf", false)]
    [InlineData("text/plain", false)]
    public void IsImage_ShouldReturnCorrectValue(string contentType, bool expected)
    {
        // Act & Assert
        ContentTypeHelper.IsImage(contentType).ShouldBe(expected);
    }

    [Theory]
    [InlineData("video/mp4", true)]
    [InlineData("video/webm", true)]
    [InlineData("audio/mp3", false)]
    [InlineData("image/jpeg", false)]
    public void IsVideo_ShouldReturnCorrectValue(string contentType, bool expected)
    {
        // Act & Assert
        ContentTypeHelper.IsVideo(contentType).ShouldBe(expected);
    }

    [Theory]
    [InlineData("audio/mpeg", true)]
    [InlineData("audio/wav", true)]
    [InlineData("video/mp4", false)]
    [InlineData("image/jpeg", false)]
    public void IsAudio_ShouldReturnCorrectValue(string contentType, bool expected)
    {
        // Act & Assert
        ContentTypeHelper.IsAudio(contentType).ShouldBe(expected);
    }

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("application/msword", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", true)]
    [InlineData("application/vnd.ms-excel", true)]
    [InlineData("application/vnd.ms-powerpoint", true)]
    [InlineData("image/jpeg", false)]
    [InlineData("text/plain", false)]
    public void IsDocument_ShouldReturnCorrectValue(string contentType, bool expected)
    {
        // Act & Assert
        ContentTypeHelper.IsDocument(contentType).ShouldBe(expected);
    }
}
