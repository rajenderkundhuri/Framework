using Framework.Application.Storage;
using Shouldly;

namespace Framework.Application.Tests.Storage;

public class StorageSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeStorage()
    {
        // Assert
        StorageSettings.SectionName.ShouldBe("Storage");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new StorageSettings();

        // Assert
        settings.Provider.ShouldBe(StorageProvider.Local);
        settings.LocalBasePath.ShouldBe("storage");
        settings.AutoCreateDirectories.ShouldBeTrue();
        settings.MaxFileSize.ShouldBe(0);
        settings.AllowedExtensions.ShouldBeEmpty();
        settings.BlockedExtensions.ShouldContain(".exe");
        settings.BlockedExtensions.ShouldContain(".dll");
        settings.BlockedExtensions.ShouldContain(".bat");
        settings.TenantIsolation.ShouldBeTrue();
        settings.PreserveOriginalFilenames.ShouldBeTrue();
        settings.AzureBlob.ShouldBeNull();
        settings.AwsS3.ShouldBeNull();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new StorageSettings
        {
            Provider = StorageProvider.AzureBlob,
            LocalBasePath = "/custom/path",
            AutoCreateDirectories = false,
            MaxFileSize = 10 * 1024 * 1024,
            AllowedExtensions = new List<string> { ".jpg", ".png" },
            BlockedExtensions = new List<string> { ".exe" },
            TenantIsolation = false,
            PreserveOriginalFilenames = false,
            DefaultCacheControl = "max-age=86400"
        };

        // Assert
        settings.Provider.ShouldBe(StorageProvider.AzureBlob);
        settings.LocalBasePath.ShouldBe("/custom/path");
        settings.AutoCreateDirectories.ShouldBeFalse();
        settings.MaxFileSize.ShouldBe(10 * 1024 * 1024);
        settings.AllowedExtensions.ShouldContain(".jpg");
        settings.BlockedExtensions.ShouldContain(".exe");
        settings.TenantIsolation.ShouldBeFalse();
        settings.PreserveOriginalFilenames.ShouldBeFalse();
    }
}

public class StorageProviderTests
{
    [Fact]
    public void StorageProvider_ShouldHaveExpectedValues()
    {
        // Assert
        ((int)StorageProvider.Local).ShouldBe(0);
        ((int)StorageProvider.AzureBlob).ShouldBe(1);
        ((int)StorageProvider.AwsS3).ShouldBe(2);
        ((int)StorageProvider.InMemory).ShouldBe(99);
    }

    [Fact]
    public void StorageProvider_ShouldHaveAllProviders()
    {
        // Act
        var providers = Enum.GetValues<StorageProvider>();

        // Assert
        providers.Length.ShouldBe(4);
    }
}

public class AzureBlobSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new AzureBlobSettings();

        // Assert
        settings.ConnectionString.ShouldBe(string.Empty);
        settings.ContainerName.ShouldBe("files");
        settings.AutoCreateContainer.ShouldBeTrue();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new AzureBlobSettings
        {
            ConnectionString = "DefaultEndpointsProtocol=https;...",
            ContainerName = "custom-container",
            AutoCreateContainer = false
        };

        // Assert
        settings.ConnectionString.ShouldStartWith("DefaultEndpointsProtocol");
        settings.ContainerName.ShouldBe("custom-container");
        settings.AutoCreateContainer.ShouldBeFalse();
    }
}

public class AwsS3SettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new AwsS3Settings();

        // Assert
        settings.Region.ShouldBe("us-east-1");
        settings.BucketName.ShouldBe(string.Empty);
        settings.AccessKey.ShouldBeNull();
        settings.SecretKey.ShouldBeNull();
        settings.AutoCreateBucket.ShouldBeFalse();
        settings.ServiceUrl.ShouldBeNull();
        settings.ForcePathStyle.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new AwsS3Settings
        {
            Region = "eu-west-1",
            BucketName = "my-bucket",
            AccessKey = "AKIA...",
            SecretKey = "secret",
            AutoCreateBucket = true,
            ServiceUrl = "http://localhost:9000",
            ForcePathStyle = true
        };

        // Assert
        settings.Region.ShouldBe("eu-west-1");
        settings.BucketName.ShouldBe("my-bucket");
        settings.AccessKey.ShouldBe("AKIA...");
        settings.SecretKey.ShouldBe("secret");
        settings.AutoCreateBucket.ShouldBeTrue();
        settings.ServiceUrl.ShouldBe("http://localhost:9000");
        settings.ForcePathStyle.ShouldBeTrue();
    }
}
