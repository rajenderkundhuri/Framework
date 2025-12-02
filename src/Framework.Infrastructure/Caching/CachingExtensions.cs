using Framework.Application.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.Caching;

/// <summary>
/// Extension methods for configuring caching services
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Adds caching services to the service collection
    /// </summary>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(CacheSettings.SectionName)
            .Get<CacheSettings>() ?? new CacheSettings();

        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        switch (settings.CacheType)
        {
            case CacheType.Memory:
                services.AddMemoryCache(options =>
                {
                    if (settings.MemoryCacheSizeLimitMB > 0)
                    {
                        options.SizeLimit = settings.MemoryCacheSizeLimitMB * 1024 * 1024;
                        options.CompactionPercentage = settings.CompactionPercentage;
                    }
                });
                services.AddScoped<ICacheService, MemoryCacheService>();
                break;

            case CacheType.Redis:
                if (string.IsNullOrEmpty(settings.RedisConnectionString))
                    throw new InvalidOperationException("Redis connection string is required for Redis cache");

                // Note: Microsoft.Extensions.Caching.StackExchangeRedis package is required
                // Install with: dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
                throw new NotSupportedException(
                    "Redis cache requires Microsoft.Extensions.Caching.StackExchangeRedis package. " +
                    "Use Memory cache for development or install the package for production.");

            case CacheType.SqlServer:
                if (string.IsNullOrEmpty(settings.SqlServerConnectionString))
                    throw new InvalidOperationException("SQL Server connection string is required for SQL Server cache");

                // Note: Microsoft.Extensions.Caching.SqlServer package is required
                // Install with: dotnet add package Microsoft.Extensions.Caching.SqlServer
                throw new NotSupportedException(
                    "SQL Server cache requires Microsoft.Extensions.Caching.SqlServer package. " +
                    "Use Memory cache for development or install the package for production.");

            default:
                services.AddMemoryCache();
                services.AddScoped<ICacheService, MemoryCacheService>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds in-memory caching (for testing)
    /// </summary>
    public static IServiceCollection AddInMemoryCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.Configure<CacheSettings>(options => { });
        services.AddScoped<ICacheService, MemoryCacheService>();
        return services;
    }
}
