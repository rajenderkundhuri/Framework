using Framework.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.Storage;

/// <summary>
/// Extension methods for configuring storage services
/// </summary>
public static class StorageExtensions
{
    /// <summary>
    /// Adds storage services to the service collection
    /// </summary>
    public static IServiceCollection AddStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(StorageSettings.SectionName)
            .Get<StorageSettings>() ?? new StorageSettings();

        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));

        switch (settings.Provider)
        {
            case StorageProvider.Local:
                services.AddScoped<IStorageService, LocalStorageService>();
                break;

            case StorageProvider.AzureBlob:
                // Note: Azure.Storage.Blobs package required
                throw new NotSupportedException(
                    "Azure Blob Storage requires Azure.Storage.Blobs package. " +
                    "Use Local or InMemory storage for development.");

            case StorageProvider.AwsS3:
                // Note: AWSSDK.S3 package required
                throw new NotSupportedException(
                    "AWS S3 Storage requires AWSSDK.S3 package. " +
                    "Use Local or InMemory storage for development.");

            case StorageProvider.InMemory:
                services.AddSingleton<IStorageService, InMemoryStorageService>();
                break;

            default:
                services.AddScoped<IStorageService, LocalStorageService>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds local file storage
    /// </summary>
    public static IServiceCollection AddLocalStorage(
        this IServiceCollection services,
        string basePath = "storage")
    {
        services.Configure<StorageSettings>(options =>
        {
            options.Provider = StorageProvider.Local;
            options.LocalBasePath = basePath;
        });
        services.AddScoped<IStorageService, LocalStorageService>();
        return services;
    }

    /// <summary>
    /// Adds in-memory storage (for testing)
    /// </summary>
    public static IServiceCollection AddInMemoryStorage(this IServiceCollection services)
    {
        services.Configure<StorageSettings>(options =>
        {
            options.Provider = StorageProvider.InMemory;
        });
        services.AddSingleton<IStorageService, InMemoryStorageService>();
        return services;
    }
}
