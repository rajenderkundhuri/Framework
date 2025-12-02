using Framework.Application.Caching;
using Framework.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Framework.Infrastructure.Tests.Caching;

public class MemoryCacheServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<CacheSettings> _settings;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _settings = Options.Create(new CacheSettings { IsEnabled = true });
        _cacheService = new MemoryCacheService(_memoryCache, _settings);
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotExists_ShouldReturnDefault()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.ShouldBe(value);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldCacheCorrectly()
    {
        // Arrange
        var key = "complex-object";
        var value = new TestCacheObject { Id = 1, Name = "Test", CreatedAt = DateTime.UtcNow };

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Test");
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValue()
    {
        // Arrange
        var key = "to-remove";
        await _cacheService.SetAsync(key, "value");

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "exists-key";
        await _cacheService.SetAsync(key, "value");

        // Act
        var exists = await _cacheService.ExistsAsync(key);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyNotExists_ShouldReturnFalse()
    {
        // Act
        var exists = await _cacheService.ExistsAsync("nonexistent");

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyNotExists_ShouldCallFactory()
    {
        // Arrange
        var key = "get-or-create";
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, async _ =>
        {
            factoryCalled = true;
            return "created-value";
        });

        // Assert
        factoryCalled.ShouldBeTrue();
        result.ShouldBe("created-value");
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyExists_ShouldNotCallFactory()
    {
        // Arrange
        var key = "existing-key";
        await _cacheService.SetAsync(key, "existing-value");
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrCreateAsync(key, async _ =>
        {
            factoryCalled = true;
            return "new-value";
        });

        // Assert
        factoryCalled.ShouldBeFalse();
        result.ShouldBe("existing-value");
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheCreatedValue()
    {
        // Arrange
        var key = "cached-created";

        // Act
        await _cacheService.GetOrCreateAsync(key, _ => Task.FromResult("value"));
        var cached = await _cacheService.GetAsync<string>(key);

        // Assert
        cached.ShouldBe("value");
    }

    [Fact]
    public async Task SetAsync_WithAbsoluteExpiration_ShouldExpire()
    {
        // Arrange
        var key = "expiring-key";
        var options = new CacheOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50)
        };

        // Act
        await _cacheService.SetAsync(key, "value", options);
        await Task.Delay(100);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_ShouldRemoveMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("user:1:profile", "profile1");
        await _cacheService.SetAsync("user:1:settings", "settings1");
        await _cacheService.SetAsync("user:2:profile", "profile2");

        // Act
        await _cacheService.RemoveByPatternAsync("user:1:*");

        // Assert
        (await _cacheService.ExistsAsync("user:1:profile")).ShouldBeFalse();
        (await _cacheService.ExistsAsync("user:1:settings")).ShouldBeFalse();
        (await _cacheService.ExistsAsync("user:2:profile")).ShouldBeTrue();
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllKeys()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1");
        await _cacheService.SetAsync("key2", "value2");
        await _cacheService.SetAsync("key3", "value3");

        // Act
        await _cacheService.ClearAsync();

        // Assert
        (await _cacheService.ExistsAsync("key1")).ShouldBeFalse();
        (await _cacheService.ExistsAsync("key2")).ShouldBeFalse();
        (await _cacheService.ExistsAsync("key3")).ShouldBeFalse();
    }

    [Fact]
    public async Task WhenDisabled_GetAsync_ShouldReturnDefault()
    {
        // Arrange
        var settings = Options.Create(new CacheSettings { IsEnabled = false });
        var service = new MemoryCacheService(_memoryCache, settings);

        // Pre-cache a value using enabled service
        await _cacheService.SetAsync("disabled-test", "value");

        // Act
        var result = await service.GetAsync<string>("disabled-test");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task WhenDisabled_GetOrCreateAsync_ShouldAlwaysCallFactory()
    {
        // Arrange
        var settings = Options.Create(new CacheSettings { IsEnabled = false });
        var service = new MemoryCacheService(_memoryCache, settings);
        var callCount = 0;

        // Act
        await service.GetOrCreateAsync("test", _ => { callCount++; return Task.FromResult("v1"); });
        await service.GetOrCreateAsync("test", _ => { callCount++; return Task.FromResult("v2"); });

        // Assert
        callCount.ShouldBe(2);
    }

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

public class MemoryCacheServiceWithTenantTests
{
    [Fact]
    public async Task WithTenantIsolation_ShouldPrefixKeys()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTenantContext = new MockTenantContext(tenantId);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var settings = Options.Create(new CacheSettings
        {
            IsEnabled = true,
            UseTenantIsolation = true
        });
        var service = new MemoryCacheService(memoryCache, settings, mockTenantContext);

        // Act
        await service.SetAsync("key", "value");
        var exists = await service.ExistsAsync("key");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task DifferentTenants_ShouldHaveIsolatedCache()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var settings = Options.Create(new CacheSettings
        {
            IsEnabled = true,
            UseTenantIsolation = true
        });

        var service1 = new MemoryCacheService(memoryCache, settings, new MockTenantContext(tenant1Id));
        var service2 = new MemoryCacheService(memoryCache, settings, new MockTenantContext(tenant2Id));

        // Act
        await service1.SetAsync("shared-key", "tenant1-value");
        await service2.SetAsync("shared-key", "tenant2-value");

        var result1 = await service1.GetAsync<string>("shared-key");
        var result2 = await service2.GetAsync<string>("shared-key");

        // Assert
        result1.ShouldBe("tenant1-value");
        result2.ShouldBe("tenant2-value");
    }

    private class MockTenantContext : Framework.Domain.MultiTenancy.ITenantContext
    {
        public MockTenantContext(Guid? tenantId)
        {
            TenantId = tenantId;
        }

        public Guid? TenantId { get; }
        public Framework.Domain.MultiTenancy.Tenant? CurrentTenant => null;
        public bool IsHost => TenantId == null;
        public bool IsEnabled => true;
    }
}
