using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Framework.Infrastructure.SqlServer;

/// <summary>
/// Design-time factory for SQL Server migrations
/// </summary>
public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Default connection string for development/migrations
        // Override with environment variable or command line argument
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
            ?? "Server=localhost;Database=FrameworkDb;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
