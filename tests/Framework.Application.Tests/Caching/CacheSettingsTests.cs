using Framework.Application.Caching;
using Shouldly;

namespace Framework.Application.Tests.Caching;

public class CacheSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeCache()
    {
        CacheSettings.SectionName.ShouldBe("Cache");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var settings = new CacheSettings();

        // Assert
        settings.IsEnabled.ShouldBeTrue();
        settings.CacheType.ShouldBe(CacheType.Memory);
        settings.DefaultExpirationMinutes.ShouldBe(5);
        settings.RedisConnectionString.ShouldBeNull();
        settings.RedisInstanceName.ShouldBe("Framework:");
        settings.SqlServerConnectionString.ShouldBeNull();
        settings.SqlServerSchemaName.ShouldBe("dbo");
        settings.SqlServerTableName.ShouldBe("Cache");
        settings.UseTenantIsolation.ShouldBeTrue();
        settings.KeyPrefix.ShouldBe(string.Empty);
        settings.MemoryCacheSizeLimitMB.ShouldBe(100);
        settings.CompactionPercentage.ShouldBe(0.25);
    }

    [Fact]
    public void AllProperties_CanBeModified()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.IsEnabled = false;
        settings.CacheType = CacheType.Redis;
        settings.DefaultExpirationMinutes = 30;
        settings.RedisConnectionString = "localhost:6379";
        settings.RedisInstanceName = "MyApp:";
        settings.SqlServerConnectionString = "Server=.;Database=Cache";
        settings.SqlServerSchemaName = "cache";
        settings.SqlServerTableName = "CacheEntries";
        settings.UseTenantIsolation = false;
        settings.KeyPrefix = "myapp:";
        settings.MemoryCacheSizeLimitMB = 500;
        settings.CompactionPercentage = 0.5;

        // Assert
        settings.IsEnabled.ShouldBeFalse();
        settings.CacheType.ShouldBe(CacheType.Redis);
        settings.DefaultExpirationMinutes.ShouldBe(30);
        settings.RedisConnectionString.ShouldBe("localhost:6379");
        settings.RedisInstanceName.ShouldBe("MyApp:");
        settings.SqlServerConnectionString.ShouldBe("Server=.;Database=Cache");
        settings.SqlServerSchemaName.ShouldBe("cache");
        settings.SqlServerTableName.ShouldBe("CacheEntries");
        settings.UseTenantIsolation.ShouldBeFalse();
        settings.KeyPrefix.ShouldBe("myapp:");
        settings.MemoryCacheSizeLimitMB.ShouldBe(500);
        settings.CompactionPercentage.ShouldBe(0.5);
    }
}

public class CacheTypeTests
{
    [Theory]
    [InlineData(CacheType.Memory, 0)]
    [InlineData(CacheType.Redis, 1)]
    [InlineData(CacheType.SqlServer, 2)]
    public void CacheType_ShouldHaveCorrectValues(CacheType type, int expected)
    {
        ((int)type).ShouldBe(expected);
    }
}
