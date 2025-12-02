# Infrastructure Layer Guide

The Infrastructure layer provides implementations for data access, external services, and infrastructure concerns. It depends on both the Application and Domain layers.

## Overview

The Infrastructure layer handles technical concerns:
- **Location**: `src/Framework.Infrastructure/`
- **Dependencies**: Application and Domain layers
- **Purpose**: Implement data access and external integrations
- **Technologies**: Entity Framework Core, external APIs, file storage, etc.

## Key Components

### 1. Database Context

The DbContext is the entry point for Entity Framework Core.

#### ApplicationDbContext

```csharp
namespace Framework.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService,
        IDateTime dateTime)
        : base(options)
    {
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(_currentUserService.UserId ?? "System");
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedBy(_currentUserService.UserId ?? "System");
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events
        await DispatchDomainEventsAsync();

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.RollbackTransactionAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}
```

### 2. Entity Configurations

Configure entity mappings using the Fluent API.

#### Base Configuration

```csharp
namespace Framework.Infrastructure.Persistence.Configurations;

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();
    }
}
```

#### Customer Configuration

```csharp
namespace Framework.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(c => c.Email)
            .IsUnique();

        // Value object mapping
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200);

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.State)
                .IsRequired()
                .HasMaxLength(2);

            address.Property(a => a.ZipCode)
                .IsRequired()
                .HasMaxLength(10);

            address.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100);
        });

        // Relationships
        builder.HasMany(c => c.Orders)
            .WithOne()
            .HasForeignKey("CustomerId")
            .OnDelete(DeleteBehavior.Restrict);

        // Audit fields
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedAt);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        // Soft delete
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
```

#### Order Configuration

```csharp
namespace Framework.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>();

        // Money value object
        builder.OwnsOne(o => o.Total, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("TotalAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Collection of order items
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.ShippedAt);
        builder.Property(o => o.DeliveredAt);

        // Ignore domain events (not persisted)
        builder.Ignore(o => o.DomainEvents);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("UnitPrice")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(i => i.Quantity)
            .IsRequired();

        // Computed column or read-only property
        builder.Ignore(i => i.Subtotal);
    }
}
```

### 3. Repository Pattern

Generic repository for common data operations.

#### IRepository Interface

```csharp
namespace Framework.Application.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
```

#### Repository Implementation

```csharp
namespace Framework.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
```

#### Specific Repository

```csharp
namespace Framework.Infrastructure.Persistence.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync(CancellationToken cancellationToken = default);
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Orders)
            .ToListAsync(cancellationToken);
    }
}
```

### 4. Unit of Work

Coordinate multiple repository operations in a single transaction.

#### IUnitOfWork Interface

```csharp
namespace Framework.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

#### UnitOfWork Implementation

```csharp
namespace Framework.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repository<>).MakeGenericType(type);
            var repositoryInstance = Activator.CreateInstance(repositoryType, _context);
            _repositories.Add(type, repositoryInstance!);
        }

        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _context.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _context.RollbackTransactionAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }
}
```

### 5. Database Migrations

Entity Framework Core migrations manage database schema changes.

#### Creating Migrations

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project src/Framework.Infrastructure --startup-project src/Framework.Api

# Update database
dotnet ef database update --project src/Framework.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/Framework.Infrastructure --startup-project src/Framework.Api

# Generate SQL script
dotnet ef migrations script --project src/Framework.Infrastructure --startup-project src/Framework.Api --output migration.sql
```

#### Migration Example

```csharp
namespace Framework.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Address_State = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                Address_ZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Address_Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_Email",
            table: "Customers",
            column: "Email",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Customers");
    }
}
```

### 6. External Services

Implement application interfaces for external integrations.

#### DateTime Service

```csharp
namespace Framework.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
```

#### Email Service

```csharp
namespace Framework.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _settings;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
```

#### File Storage Service

```csharp
namespace Framework.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly FileStorageSettings _settings;

    public FileStorageService(
        ILogger<FileStorageService> logger,
        IOptions<FileStorageSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_settings.UploadPath, uniqueFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using var outputStream = File.Create(filePath);
            await fileStream.CopyToAsync(outputStream);

            _logger.LogInformation("File uploaded: {FileName}", uniqueFileName);

            return $"{_settings.BaseUrl}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            var filePath = Path.Combine(_settings.UploadPath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", fileName);

            var memory = new MemoryStream();
            using var stream = File.OpenRead(filePath);
            await stream.CopyToAsync(memory);
            memory.Position = 0;

            return memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FileUrl}", fileUrl);
            throw;
        }
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            var filePath = Path.Combine(_settings.UploadPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {FileName}", fileName);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileUrl}", fileUrl);
            throw;
        }
    }
}

public class FileStorageSettings
{
    public string UploadPath { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
```

### 7. Dependency Injection

Register infrastructure services.

```csharp
namespace Framework.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Register DbContext as interface
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddTransient<IDateTime, DateTimeService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IFileStorageService, FileStorageService>();

        // Settings
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<FileStorageSettings>(configuration.GetSection("FileStorageSettings"));

        return services;
    }
}
```

## Best Practices

### 1. Use Configurations

Define entity configurations separately:

```csharp
// Good - Separate configuration
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder) { }
}

// Bad - Configuration in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>().HasKey(c => c.Id);
    // ...
}
```

### 2. Map Value Objects Properly

Use `OwnsOne` for value objects:

```csharp
builder.OwnsOne(c => c.Address, address =>
{
    address.Property(a => a.Street).HasMaxLength(200);
});
```

### 3. Use Query Filters

Implement soft delete with global query filters:

```csharp
builder.HasQueryFilter(c => !c.IsDeleted);
```

### 4. Handle Migrations

Keep migrations in source control and apply them during deployment.

### 5. Use Transactions

Wrap multiple operations in transactions:

```csharp
await _unitOfWork.BeginTransactionAsync();
try
{
    await _unitOfWork.Repository<Customer>().AddAsync(customer);
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitAsync();
}
catch
{
    await _unitOfWork.RollbackAsync();
    throw;
}
```

## Summary

The Infrastructure layer provides:
- Entity Framework Core for data access
- Repository pattern for data abstraction
- Unit of Work for transaction management
- Database migrations for schema versioning
- External service implementations
- Configuration and dependency injection

This design keeps infrastructure concerns separated from business logic, making the application testable and maintainable.
