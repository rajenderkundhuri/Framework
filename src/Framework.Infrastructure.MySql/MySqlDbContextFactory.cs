using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Framework.Infrastructure.MySql;

/// <summary>
/// Design-time factory for MySQL migrations
/// </summary>
public class MySqlDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Default connection string for development/migrations
        // Override with environment variable or command line argument
        var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
            ?? "Server=localhost;Database=framework;User=root;Password=Welcome12;";

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        optionsBuilder.UseMySql(connectionString, serverVersion, options =>
        {
            options.MigrationsAssembly(typeof(MySqlDbContextFactory).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
