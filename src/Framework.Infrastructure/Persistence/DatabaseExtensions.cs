using Framework.Infrastructure.Auditing;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.Persistence;

/// <summary>
/// Database configuration extensions
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds the database context with the configured provider
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(DatabaseSettings.SectionName).Get<DatabaseSettings>()
            ?? new DatabaseSettings();

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        return settings.Provider switch
        {
            DatabaseProvider.MySql => services.AddMySqlDatabase(settings),
            DatabaseProvider.SqlServer => services.AddSqlServerDatabase(settings),
            DatabaseProvider.PostgreSql => services.AddPostgreSqlDatabase(settings),
            DatabaseProvider.InMemory => services.AddInMemoryDatabase("FrameworkDb"),
            _ => services.AddMySqlDatabase(settings)
        };
    }

    /// <summary>
    /// Adds MySQL database
    /// </summary>
    public static IServiceCollection AddMySqlDatabase(
        this IServiceCollection services,
        DatabaseSettings settings)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var serverVersion = ServerVersion.AutoDetect(settings.ConnectionString);

            options.UseMySql(settings.ConnectionString, serverVersion, mySqlOptions =>
            {
                // Use the MySQL-specific migration project
                mySqlOptions.MigrationsAssembly("Framework.Infrastructure.MySql");

                if (settings.EnableRetryOnFailure)
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null);
                }

                mySqlOptions.CommandTimeout(settings.CommandTimeout);
            });

            if (settings.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();

            if (settings.EnableDetailedErrors)
                options.EnableDetailedErrors();

            // Add audit interceptor
            var auditInterceptor = sp.GetService<AuditInterceptor>();
            if (auditInterceptor != null)
                options.AddInterceptors(auditInterceptor);
        });

        return services;
    }

    /// <summary>
    /// Adds SQL Server database
    /// </summary>
    public static IServiceCollection AddSqlServerDatabase(
        this IServiceCollection services,
        DatabaseSettings settings)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(settings.ConnectionString, sqlOptions =>
            {
                // Use the SQL Server-specific migration project
                sqlOptions.MigrationsAssembly("Framework.Infrastructure.SqlServer");

                if (settings.EnableRetryOnFailure)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.MaxRetryDelaySeconds),
                        errorNumbersToAdd: null);
                }

                sqlOptions.CommandTimeout(settings.CommandTimeout);
            });

            if (settings.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();

            if (settings.EnableDetailedErrors)
                options.EnableDetailedErrors();

            // Add audit interceptor
            var auditInterceptor = sp.GetService<AuditInterceptor>();
            if (auditInterceptor != null)
                options.AddInterceptors(auditInterceptor);
        });

        return services;
    }

    /// <summary>
    /// Adds PostgreSQL database
    /// </summary>
    public static IServiceCollection AddPostgreSqlDatabase(
        this IServiceCollection services,
        DatabaseSettings settings)
    {
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(settings.ConnectionString, npgsqlOptions =>
            {
                // Use the PostgreSQL-specific migration project
                npgsqlOptions.MigrationsAssembly("Framework.Infrastructure.PostgreSql");

                if (settings.EnableRetryOnFailure)
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(settings.MaxRetryDelaySeconds),
                        errorCodesToAdd: null);
                }

                npgsqlOptions.CommandTimeout(settings.CommandTimeout);
            });

            if (settings.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();

            if (settings.EnableDetailedErrors)
                options.EnableDetailedErrors();

            // Add audit interceptor
            var auditInterceptor = sp.GetService<AuditInterceptor>();
            if (auditInterceptor != null)
                options.AddInterceptors(auditInterceptor);
        });

        return services;
    }

    /// <summary>
    /// Adds in-memory database (for testing)
    /// </summary>
    public static IServiceCollection AddInMemoryDatabase(
        this IServiceCollection services,
        string databaseName = "TestDb")
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        return services;
    }

    /// <summary>
    /// Runs database migrations
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Only run migrations for relational databases (not in-memory)
        if (!context.Database.IsInMemory())
        {
            await context.Database.MigrateAsync();
        }
    }

    /// <summary>
    /// Runs database migrations and seeds initial data
    /// </summary>
    public static async Task MigrateAndSeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        await serviceProvider.MigrateDatabaseAsync();
        await serviceProvider.SeedDataAsync();
    }

    /// <summary>
    /// Ensures database is created (for development/testing)
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
