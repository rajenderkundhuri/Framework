# Available Modules

The Framework provides a collection of ready-to-use modules for common enterprise application requirements. These modules can be integrated into your application to accelerate development.

## Overview

Framework modules are designed to be:
- **Modular**: Use only what you need
- **Extensible**: Customize to fit your requirements
- **Well-tested**: Comprehensive test coverage
- **Production-ready**: Enterprise-grade implementations

## Core Modules

### 1. Identity & Authentication

User management, authentication, and authorization.

#### Features

- User registration and login
- Password hashing and validation
- JWT token generation
- Refresh tokens
- Email confirmation
- Password reset
- Role-based access control (RBAC)
- Claims-based authorization

#### Implementation Example

```csharp
// Domain Entity
public class ApplicationUser : BaseEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private readonly List<string> _roles = new();
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    public void AddRole(string role)
    {
        if (!_roles.Contains(role))
            _roles.Add(role);
    }

    public void RemoveRole(string role)
    {
        _roles.Remove(role);
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}

// Authentication Service
public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(string email, string password);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
}

public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

// Command Example
public class LoginCommand : IRequest<AuthenticationResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

#### Configuration

```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-characters",
    "Issuer": "Framework",
    "Audience": "Framework.Api",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### 2. Multi-Tenancy

Support for multi-tenant applications with data isolation.

#### Features

- Tenant identification
- Tenant-specific data isolation
- Database per tenant or shared database
- Tenant context management
- Tenant-aware queries

#### Implementation Example

```csharp
// Tenant Entity
public class Tenant : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Identifier { get; private set; } = string.Empty;
    public string? ConnectionString { get; private set; }
    public bool IsActive { get; private set; }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

// Multi-Tenant Entity Base
public abstract class MultiTenantEntity : BaseEntity
{
    public Guid TenantId { get; protected set; }
}

// Tenant Service
public interface ITenantService
{
    Guid? CurrentTenantId { get; }
    Task<Tenant> GetCurrentTenantAsync();
    Task<Tenant> GetTenantByIdentifierAsync(string identifier);
}

// Tenant Resolution Middleware
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Extract tenant identifier from header, subdomain, or query string
        var tenantIdentifier = context.Request.Headers["X-Tenant-Id"].FirstOrDefault()
            ?? ExtractFromSubdomain(context.Request.Host.Host);

        if (!string.IsNullOrEmpty(tenantIdentifier))
        {
            var tenant = await tenantService.GetTenantByIdentifierAsync(tenantIdentifier);
            // Set tenant context
        }

        await _next(context);
    }
}

// Query Filter for Tenant Isolation
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>().HasQueryFilter(
        e => e.TenantId == _tenantService.CurrentTenantId);
}
```

### 3. Audit Logging

Track all changes to entities for compliance and debugging.

#### Features

- Automatic audit trail creation
- Track who changed what and when
- Entity change history
- Query audit logs
- Configurable audit rules

#### Implementation Example

```csharp
// Audit Log Entity
public class AuditLog : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty; // Create, Update, Delete
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public DateTime Timestamp { get; private set; }

    public AuditLog(string userId, string userName, string entityName, Guid entityId,
        string action, string? oldValues, string? newValues)
    {
        UserId = userId;
        UserName = userName;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        Timestamp = DateTime.UtcNow;
    }
}

// Audit Interceptor
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var auditEntries = new List<AuditLog>();

    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        if (entry.State == EntityState.Unchanged)
            continue;

        var auditLog = new AuditLog(
            _currentUserService.UserId ?? "System",
            _currentUserService.UserName ?? "System",
            entry.Entity.GetType().Name,
            entry.Entity.Id,
            entry.State.ToString(),
            entry.State == EntityState.Modified ? JsonSerializer.Serialize(entry.OriginalValues.ToObject()) : null,
            entry.State != EntityState.Deleted ? JsonSerializer.Serialize(entry.CurrentValues.ToObject()) : null);

        auditEntries.Add(auditLog);
    }

    var result = await base.SaveChangesAsync(cancellationToken);

    if (auditEntries.Any())
    {
        AuditLogs.AddRange(auditEntries);
        await base.SaveChangesAsync(cancellationToken);
    }

    return result;
}
```

### 4. Background Jobs

Execute long-running tasks asynchronously.

#### Features

- Job scheduling
- Recurring jobs
- Job queues
- Retry logic
- Job status tracking

#### Implementation Example

```csharp
// Background Job Entity
public class BackgroundJob : BaseEntity
{
    public string JobName { get; private set; } = string.Empty;
    public string JobType { get; private set; } = string.Empty;
    public string? Arguments { get; private set; }
    public JobStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void Start()
    {
        Status = JobStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = JobStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
    }
}

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

// Background Job Service
public interface IBackgroundJobService
{
    Task<Guid> EnqueueAsync<T>(Expression<Action<T>> methodCall);
    Task<Guid> ScheduleAsync<T>(Expression<Action<T>> methodCall, DateTime scheduledAt);
    Task<Guid> RecurringAsync<T>(Expression<Action<T>> methodCall, string cronExpression);
}

// Example Job
public class EmailNotificationJob
{
    private readonly IEmailService _emailService;

    public EmailNotificationJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        await _emailService.SendEmailAsync(
            email,
            "Welcome!",
            $"Hello {name}, welcome to our platform!");
    }
}

// Usage
await _backgroundJobService.EnqueueAsync<EmailNotificationJob>(
    job => job.SendWelcomeEmailAsync("user@example.com", "John"));
```

### 5. Caching

Improve performance with distributed caching.

#### Features

- In-memory caching
- Distributed caching (Redis)
- Cache invalidation
- Cache tags
- Sliding and absolute expiration

#### Implementation Example

```csharp
// Cache Service
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            return cached == null ? default : JsonSerializer.Deserialize<T>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            var serialized = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        // Implementation depends on cache provider
        throw new NotImplementedException();
    }
}

// Caching Behavior
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cacheService;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache queries
        if (!typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        var cacheKey = $"{typeof(TRequest).Name}:{JsonSerializer.Serialize(request)}";

        var cached = await _cacheService.GetAsync<TResponse>(cacheKey);
        if (cached != null)
            return cached;

        var response = await next();

        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }
}
```

### 6. File Storage

Store and retrieve files with support for different providers.

#### Features

- Local file system storage
- Cloud storage (Azure Blob, AWS S3)
- File upload/download
- File metadata
- Access control

#### Implementation Example

```csharp
// File Entity
public class StoredFile : BaseEntity
{
    public string FileName { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedBy { get; private set; }
    public DateTime UploadedAt { get; private set; }

    public StoredFile(string fileName, string originalFileName, string contentType,
        long size, string storagePath, Guid uploadedBy)
    {
        FileName = fileName;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        Size = size;
        StoragePath = storagePath;
        UploadedBy = uploadedBy;
        UploadedAt = DateTime.UtcNow;
    }
}

// File Storage Service Interface
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<Stream> DownloadAsync(string fileId);
    Task DeleteAsync(string fileId);
    Task<FileMetadata> GetMetadataAsync(string fileId);
}

// Azure Blob Storage Implementation
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient($"{Guid.NewGuid()}/{fileName}");

        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        return blobClient.Uri.ToString();
    }

    // Additional methods...
}
```

### 7. Notifications

Send notifications through multiple channels.

#### Features

- Email notifications
- SMS notifications
- Push notifications
- In-app notifications
- Notification templates
- Notification preferences

#### Implementation Example

```csharp
// Notification Entity
public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}

public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

// Notification Service
public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string message, NotificationType type);
    Task SendEmailAsync(string email, string subject, string body);
    Task SendSmsAsync(string phoneNumber, string message);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
}

// Domain Event Handler for Notifications
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly INotificationService _notificationService;

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await _notificationService.SendAsync(
            notification.CustomerId,
            "Order Confirmed",
            $"Your order {notification.OrderNumber} has been confirmed!",
            NotificationType.Success);
    }
}
```

### 8. Email Service

Comprehensive email functionality.

#### Features

- SMTP email sending
- Email templates
- HTML and text emails
- Attachments
- Email queue
- Retry logic

#### Implementation Example

```csharp
// Email Template
public class EmailTemplate : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsHtml { get; private set; }

    public string Render(Dictionary<string, string> parameters)
    {
        var result = Body;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{{{param.Key}}}}}", param.Value);
        }
        return result;
    }
}

// Email Service
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, bool isHtml = true);
    Task SendTemplateAsync(string to, string templateName, Dictionary<string, string> parameters);
    Task SendWithAttachmentsAsync(string to, string subject, string body,
        IEnumerable<EmailAttachment> attachments);
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

// Example Usage
await _emailService.SendTemplateAsync(
    "customer@example.com",
    "OrderConfirmation",
    new Dictionary<string, string>
    {
        { "CustomerName", "John Doe" },
        { "OrderNumber", "ORD-12345" },
        { "TotalAmount", "$150.00" }
    });
```

## Module Integration

### Adding a Module

1. **Install Dependencies**:
```bash
dotnet add package Package.Name
```

2. **Configure Services**:
```csharp
builder.Services.AddModule(builder.Configuration);
```

3. **Add Migrations** (if needed):
```bash
dotnet ef migrations add AddModuleName
```

4. **Update Configuration**:
```json
{
  "ModuleSettings": {
    "Key": "Value"
  }
}
```

## Best Practices

1. **Use Dependency Injection**: Register module services properly
2. **Configure Settings**: Use configuration files for module settings
3. **Follow Patterns**: Maintain consistency with framework patterns
4. **Test Modules**: Write tests for module functionality
5. **Document Usage**: Provide clear documentation for module usage

## Summary

Framework modules provide:
- Ready-to-use enterprise features
- Production-grade implementations
- Extensible and customizable designs
- Comprehensive documentation
- Test coverage

These modules accelerate development by providing common functionality out of the box while maintaining the flexibility to customize as needed.
