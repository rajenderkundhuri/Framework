using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Framework.Infrastructure.PostgreSql;

/// <summary>
/// Design-time factory for PostgreSQL migrations
/// </summary>
public class PostgreSqlDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Default connection string for development/migrations
        // Override with environment variable or command line argument
        var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
            ?? "Host=localhost;Database=frameworkdb;Username=postgres;Password=postgres;";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(PostgreSqlDbContextFactory).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
