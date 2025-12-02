using Framework.Application.BackgroundJobs;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.BackgroundJobs;

/// <summary>
/// Extension methods for configuring background job services
/// </summary>
public static class BackgroundJobsExtensions
{
    /// <summary>
    /// Adds background job services to the service collection
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(BackgroundJobSettings.SectionName)
            .Get<BackgroundJobSettings>() ?? new BackgroundJobSettings();

        services.Configure<BackgroundJobSettings>(configuration.GetSection(BackgroundJobSettings.SectionName));

        // Register job context accessor
        services.AddSingleton<IJobContextAccessor, JobContextAccessor>();

        // Configure Hangfire based on storage type
        services.AddHangfire((sp, config) =>
        {
            ConfigureStorage(config, settings);
            ConfigureFilters(config, sp);
            ConfigureOptions(config, settings);
        });

        // Add Hangfire server if enabled
        if (settings.IsEnabled)
        {
            services.AddHangfireServer(options =>
            {
                options.ServerName = !string.IsNullOrEmpty(settings.ServerNamePrefix)
                    ? $"{settings.ServerNamePrefix}:{Environment.MachineName}"
                    : Environment.MachineName;

                options.WorkerCount = settings.WorkerCount;
                options.Queues = settings.Queues
                    .OrderByDescending(q => q.Priority)
                    .Select(q => q.Name)
                    .ToArray();

                options.HeartbeatInterval = TimeSpan.FromSeconds(settings.HeartbeatIntervalSeconds);
                options.ServerTimeout = TimeSpan.FromMinutes(settings.JobTimeoutMinutes);
            });
        }

        // Register job service
        services.AddScoped<IJobService, HangfireJobService>();

        return services;
    }

    /// <summary>
    /// Adds background jobs with in-memory storage (for testing)
    /// </summary>
    public static IServiceCollection AddInMemoryBackgroundJobs(this IServiceCollection services)
    {
        services.AddSingleton<IJobContextAccessor, JobContextAccessor>();

        services.AddHangfire((sp, config) =>
        {
            config.UseInMemoryStorage();
            ConfigureFilters(config, sp);
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 1;
            options.Queues = new[] { "critical", "default", "low" };
        });

        services.AddScoped<IJobService, HangfireJobService>();

        return services;
    }

    private static void ConfigureStorage(IGlobalConfiguration config, BackgroundJobSettings settings)
    {
        switch (settings.StorageType)
        {
            case JobStorageType.InMemory:
                config.UseInMemoryStorage();
                break;

            case JobStorageType.SqlServer:
                if (string.IsNullOrEmpty(settings.StorageConnectionString))
                    throw new InvalidOperationException("StorageConnectionString is required for SQL Server storage");

                // Note: Hangfire.SqlServer package needs to be added for this to work
                // config.UseSqlServerStorage(settings.StorageConnectionString);
                throw new NotSupportedException("SQL Server storage requires Hangfire.SqlServer package. Use InMemory for development.");

            case JobStorageType.PostgreSql:
                if (string.IsNullOrEmpty(settings.StorageConnectionString))
                    throw new InvalidOperationException("StorageConnectionString is required for PostgreSQL storage");

                // Note: Hangfire.PostgreSql package needs to be added for this to work
                throw new NotSupportedException("PostgreSQL storage requires Hangfire.PostgreSql package. Use InMemory for development.");

            case JobStorageType.Redis:
                if (string.IsNullOrEmpty(settings.StorageConnectionString))
                    throw new InvalidOperationException("StorageConnectionString is required for Redis storage");

                // Note: Hangfire.Pro.Redis package needs to be added for this to work
                throw new NotSupportedException("Redis storage requires Hangfire.Pro.Redis package. Use InMemory for development.");

            default:
                config.UseInMemoryStorage();
                break;
        }
    }

    private static void ConfigureFilters(IGlobalConfiguration config, IServiceProvider serviceProvider)
    {
        // Add job context filter for tenant/user context propagation
        config.UseFilter(new JobContextFilter(serviceProvider));
    }

    private static void ConfigureOptions(IGlobalConfiguration config, BackgroundJobSettings settings)
    {
        // Configure serialization and other options
        config.UseRecommendedSerializerSettings();
    }

    /// <summary>
    /// Registers a recurring job to be scheduled on application startup
    /// </summary>
    public static IServiceCollection AddRecurringJob<TJob>(this IServiceCollection services)
        where TJob : class, IRecurringJob
    {
        services.AddScoped<TJob>();
        return services;
    }

    /// <summary>
    /// Registers a background job handler
    /// </summary>
    public static IServiceCollection AddBackgroundJob<TJob, TData>(this IServiceCollection services)
        where TJob : class, IBackgroundJob<TData>
        where TData : class
    {
        services.AddScoped<TJob>();
        return services;
    }
}
