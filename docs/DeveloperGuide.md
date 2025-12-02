# Developer Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Core Features](#core-features)
   - [Multi-Tenancy](#multi-tenancy)
   - [Identity & Authentication](#identity--authentication)
   - [User Profile & Preferences](#user-profile--preferences)
   - [Background Jobs (Hangfire)](#background-jobs-hangfire)
   - [Caching](#caching)
   - [Email & Notifications](#email--notifications)
   - [File Storage](#file-storage)
   - [Domain Events](#domain-events)
   - [Audit Logging](#audit-logging)
   - [Feature Flags](#feature-flags)
   - [Localization](#localization)
   - [Health Checks](#health-checks)
3. [Best Practices](#best-practices)
4. [Configuration Reference](#configuration-reference)

---

## Getting Started

### Framework Overview

This is a modern, enterprise-grade application framework built with .NET 9.0, following **Clean Architecture** principles and **Domain-Driven Design (DDD)** patterns. The framework provides essential building blocks for rapid development of scalable, maintainable, and testable enterprise applications.

### Architecture

The framework is organized into four layers:

```
┌─────────────────────────────────────┐
│         API Layer                   │  → Controllers, Middleware, Endpoints
│  (Framework.Api)                    │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│      Application Layer              │  → Commands, Queries, DTOs, Validators
│  (Framework.Application)            │
└─────────────────────────────────────┘
              ↓
┌─────────────────────────────────────┐
│       Domain Layer                  │  → Entities, Value Objects, Events
│  (Framework.Domain)                 │
└─────────────────────────────────────┘
              ↑
┌─────────────────────────────────────┐
│    Infrastructure Layer             │  → EF Core, Services, Integrations
│  (Framework.Infrastructure)         │
└─────────────────────────────────────┘
```

**Key Principle**: Dependencies flow inward. The Domain layer has no dependencies. The Application layer depends only on Domain. Infrastructure and API depend on Application and Domain.

### Prerequisites

- **.NET 9.0 SDK** or later
- **SQL Server** (or SQL Server LocalDB for development)
- **IDE**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd framework
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Build the solution**
```bash
dotnet build
```

4. **Configure connection string** (in `appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FrameworkDb;Trusted_Connection=true"
  }
}
```

5. **Run migrations** (if using SQL Server):
```bash
cd src/Framework.Api
dotnet ef database update
```

6. **Run the application**:
```bash
cd src/Framework.Api
dotnet run
```

The API will be available at `https://localhost:5001`.

### Project Structure

```
Framework/
├── src/
│   ├── Framework.Domain/          # Entities, Value Objects, Domain Events
│   ├── Framework.Application/     # Use Cases (Commands/Queries), DTOs
│   ├── Framework.Infrastructure/  # Data Access, External Services
│   └── Framework.Api/             # REST API, Controllers
├── tests/
│   ├── Framework.Domain.Tests/
│   ├── Framework.Application.Tests/
│   ├── Framework.Infrastructure.Tests/
│   └── Framework.Api.Tests/
└── docs/                          # Documentation
```

### Quick Start Example

**1. Register services in `Program.cs`:**
```csharp
using Framework.Application;
using Framework.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register framework services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();
app.Run();
```

**2. Create a command:**
```csharp
// Application/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IRepository<Product> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IRepository<Product> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Price);

        await _repository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.Id);
    }
}
```

**3. Create a controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProductsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

---

## Core Features

### Multi-Tenancy

The framework provides built-in multi-tenancy support with flexible tenant resolution strategies and data isolation.

#### Configuration

**appsettings.json:**
```json
{
  "MultiTenancy": {
    "IsEnabled": true,
    "DefaultConnectionString": "Server=.;Database=FrameworkDb;...",
    "TenantHeader": "X-Tenant-Id",
    "TenantQueryParam": "tenant",
    "TenantClaimType": "tenant_id",
    "Resolution": {
      "UseHeader": true,
      "UseQueryString": true,
      "UseRoute": false,
      "UseSubdomain": false,
      "UseClaim": true
    }
  }
}
```

#### Tenant Resolution Strategies

The framework supports multiple strategies for identifying the current tenant:

1. **HTTP Header**: `X-Tenant-Id: {tenant-guid}`
2. **Query String**: `?tenant={tenant-guid}`
3. **JWT Claim**: `tenant_id` claim in the access token
4. **Subdomain**: `tenant1.yourdomain.com`
5. **Route**: `/api/tenants/{tenant-id}/resources`

#### Accessing Tenant Context

**Inject `ITenantContext` to access the current tenant:**
```csharp
public class MyService
{
    private readonly ITenantContext _tenantContext;

    public MyService(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public void DoSomething()
    {
        var tenantId = _tenantContext.TenantId;
        var tenant = _tenantContext.CurrentTenant;
        var isHost = _tenantContext.IsHost;

        if (_tenantContext.IsEnabled && tenant != null)
        {
            Console.WriteLine($"Working in tenant: {tenant.Name}");
        }
    }
}
```

#### Per-Tenant Data Isolation

**Option 1: Shared Database with Tenant Filtering**

Entities that implement `IMultiTenant` are automatically filtered by tenant:

```csharp
using Framework.Domain.MultiTenancy;

public class Product : AuditableEntity, IMultiTenant
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    // Constructor, methods...
}
```

The framework automatically:
- Sets `TenantId` when saving
- Filters queries by current tenant
- Prevents cross-tenant data access

**Option 2: Per-Tenant Database**

Configure a tenant with its own connection string:

```csharp
var tenant = new Tenant("Acme Corp", "acme");
tenant.SetConnectionString("Server=.;Database=AcmeDb;...");
```

#### Creating and Managing Tenants

```csharp
public class TenantService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ITenantStore _tenantStore;

    public async Task<Guid> CreateTenantAsync(string name, string identifier)
    {
        // Create tenant
        var tenant = new Tenant(name, identifier, adminEmail: "admin@acme.com");
        tenant.SetValidity(DateTimeOffset.UtcNow.AddYears(1));

        await _tenantRepository.AddAsync(tenant);
        await _unitOfWork.SaveChangesAsync();

        // Tenant is now available for resolution
        return tenant.Id;
    }

    public async Task<Tenant?> GetTenantAsync(Guid tenantId)
    {
        return await _tenantStore.GetByIdAsync(tenantId);
    }
}
```

#### Temporarily Switching Tenant Context

```csharp
public class CrossTenantService
{
    private readonly TenantContext _tenantContext;
    private readonly IRepository<Product> _productRepository;

    public async Task<int> GetTotalProductsAcrossTenantsAsync(List<Guid> tenantIds)
    {
        int total = 0;

        foreach (var tenantId in tenantIds)
        {
            // Temporarily switch to another tenant
            using (new TenantScope(_tenantContext, tenantId))
            {
                total += await _productRepository.CountAsync();
            }
            // Original tenant context is restored automatically
        }

        return total;
    }
}
```

---

### Identity & Authentication

The framework includes a complete authentication and authorization system with JWT tokens, role-based access control (RBAC), and permission-based authorization.

#### User Registration

```csharp
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _identityService.RegisterAsync(request);

        if (result.IsSuccess)
            return Ok(new { UserId = result.Value });

        return BadRequest(result.Error);
    }
}
```

**Request model:**
```csharp
public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
```

#### User Login

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var result = await _identityService.LoginAsync(request);

    if (result.IsSuccess)
    {
        return Ok(new
        {
            result.Value.AccessToken,
            result.Value.RefreshToken,
            result.Value.ExpiresIn
        });
    }

    return Unauthorized(result.Error);
}
```

**Response includes:**
```csharp
public class TokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
```

#### JWT Configuration

**appsettings.json:**
```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyHereAtLeast32CharactersLong!",
    "Issuer": "Framework",
    "Audience": "Framework.Api",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

#### Role and Permission Management

**Assign role to user:**
```csharp
public class UserManagementService
{
    private readonly IUserManagementService _userService;

    public async Task AssignAdminRoleAsync(Guid userId)
    {
        var result = await _userService.AssignRoleAsync(userId, roleId: adminRoleId);

        if (result.IsFailure)
            throw new Exception(result.Error);
    }
}
```

**Check permissions:**
```csharp
public class OrderService
{
    private readonly IUserManagementService _userService;

    public async Task<bool> CanApproveOrderAsync(Guid userId)
    {
        var result = await _userService.HasPermissionAsync(userId, "orders.approve");
        return result.IsSuccess && result.Value;
    }
}
```

#### Protecting Endpoints with Permissions

**Using the `[Authorize]` attribute with permission-based policy:**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires authentication
public class OrdersController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "orders.create")] // Requires permission
    public async Task<IActionResult> CreateOrder(CreateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = "orders.approve")]
    public async Task<IActionResult> ApproveOrder(Guid id)
    {
        // Only users with "orders.approve" permission can access
        var result = await _mediator.Send(new ApproveOrderCommand(id));
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
```

#### OpenID Connect / External Providers

**Configure external authentication (Azure AD, Google):**

```csharp
public class ExternalAuthController : ControllerBase
{
    private readonly IExternalLoginService _externalLoginService;

    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin(ExternalLoginRequest request)
    {
        // Process external login (from Azure AD, Google, etc.)
        var result = await _externalLoginService.ProcessExternalLoginAsync(
            provider: request.Provider,      // "AzureAD", "Google"
            providerKey: request.ProviderKey, // External user ID
            email: request.Email,
            name: request.Name,
            claims: request.Claims
        );

        if (result.IsSuccess)
        {
            if (result.Value.UserFound)
            {
                // Generate JWT token for existing user
                return Ok(new { UserId = result.Value.UserId });
            }
            else
            {
                // New user - create account
                return Ok(new { Message = "New user, complete registration" });
            }
        }

        return BadRequest(result.Error);
    }

    [HttpPost("link-external-login")]
    [Authorize]
    public async Task<IActionResult> LinkExternalLogin(LinkExternalLoginRequest request)
    {
        var userId = User.GetUserId(); // Extension method to get current user ID

        var result = await _externalLoginService.LinkLoginAsync(
            userId,
            request.Provider,
            request.ProviderKey,
            request.DisplayName
        );

        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}
```

---

### User Profile & Preferences

The framework includes a comprehensive user profile system with preferences for timezone, date/time formats, currency, theme, and notifications.

#### User Profile Structure

```csharp
public class UserProfileResponse
{
    public Guid UserId { get; set; }

    // Localization
    public string TimeZoneId { get; set; } = "UTC";
    public string Locale { get; set; } = "en-US";

    // Formatting
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string NumberFormatLocale { get; set; } = "en-US";
    public string CurrencyCode { get; set; } = "USD";

    // UI Preferences
    public int Theme { get; set; } // 0 = Light, 1 = Dark, 2 = Auto

    // Notifications
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }

    // Security
    public int PreferredTwoFactorMethod { get; set; }
}
```

#### Updating User Profile

```csharp
[Authorize]
[HttpPut("profile")]
public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest request)
{
    var userId = User.GetUserId();

    var result = await _userManagementService.UpdateUserProfileAsync(userId, request);

    return result.IsSuccess ? Ok() : BadRequest(result.Error);
}
```

#### Formatting Dates Based on User Preferences

```csharp
public class ReportService
{
    private readonly IUserManagementService _userService;
    private readonly ICurrentUser _currentUser;

    public async Task<string> FormatDateForUserAsync(DateTime dateUtc)
    {
        var userId = _currentUser.UserId;
        var profileResult = await _userService.GetUserProfileAsync(userId);

        if (profileResult.IsSuccess)
        {
            var profile = profileResult.Value;

            // Convert from UTC to user's timezone
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZoneId);
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, timeZone);

            // Format using user's preferred format
            return localDate.ToString(profile.DateTimeFormat);
        }

        return dateUtc.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
```

#### Currency Formatting

```csharp
public class PricingService
{
    public async Task<string> FormatPriceForUserAsync(decimal amount, UserProfileResponse profile)
    {
        var culture = new CultureInfo(profile.NumberFormatLocale);
        return amount.ToString("C", culture); // e.g., "$1,234.56" or "1.234,56 €"
    }
}
```

---

### Background Jobs (Hangfire)

The framework integrates Hangfire for background job processing with support for fire-and-forget, delayed, recurring, and continuation jobs.

#### Configuration

```json
{
  "BackgroundJobs": {
    "Enabled": true,
    "ServerName": "Framework-Worker",
    "WorkerCount": 5,
    "Queues": ["default", "critical", "low-priority"],
    "Dashboard": {
      "Enabled": true,
      "Path": "/hangfire",
      "RequireAuthentication": true
    }
  }
}
```

#### Fire-and-Forget Jobs

**Enqueue a job for immediate execution:**

```csharp
public class EmailService
{
    private readonly IJobService _jobService;

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        // Enqueue job - returns immediately
        var jobId = _jobService.Enqueue<SendEmailJob, SendEmailData>(
            new SendEmailData
            {
                To = email,
                Subject = "Welcome!",
                Body = $"Hello {name}, welcome to our platform!"
            }
        );

        Console.WriteLine($"Job queued with ID: {jobId}");
    }
}

// Job implementation
public class SendEmailJob : IBackgroundJob<SendEmailData>
{
    private readonly IEmailService _emailService;

    public SendEmailJob(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task ExecuteAsync(SendEmailData data, JobContext context)
    {
        var message = EmailMessage.Create(data.To, data.Subject, data.Body);
        await _emailService.SendAsync(message);
    }
}

public class SendEmailData
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}
```

**Using expressions:**
```csharp
_jobService.Enqueue(() => Console.WriteLine("Hello from background!"));
_jobService.Enqueue(async () => await DoSomethingAsync());
```

#### Delayed Jobs

**Schedule a job to run after a delay:**

```csharp
public class NotificationService
{
    private readonly IJobService _jobService;

    public void ScheduleReminderAsync(Guid userId, string message)
    {
        // Run after 1 hour
        _jobService.Schedule<SendReminderJob, ReminderData>(
            new ReminderData { UserId = userId, Message = message },
            delay: TimeSpan.FromHours(1)
        );

        // Or run at specific time
        _jobService.Schedule<SendReminderJob, ReminderData>(
            new ReminderData { UserId = userId, Message = message },
            enqueueAt: DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(9) // Tomorrow at 9 AM
        );
    }
}
```

#### Recurring Jobs

**Define a recurring job with CRON expression:**

```csharp
// Define the job
[RecurringJob("0 2 * * *")] // Every day at 2 AM
public class DailyReportJob : IRecurringJob
{
    private readonly IReportService _reportService;

    public DailyReportJob(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task ExecuteAsync(JobContext context)
    {
        await _reportService.GenerateDailyReportAsync();
    }
}

// Register the job
public class JobRegistration
{
    public static void RegisterJobs(IJobService jobService)
    {
        // Automatically registered from attribute
        jobService.AddOrUpdateRecurring<DailyReportJob>();

        // Or manually
        jobService.AddOrUpdateRecurring(
            jobId: "cleanup-logs",
            methodCall: () => CleanupOldLogs(),
            cronExpression: "0 3 * * 0", // Every Sunday at 3 AM
            timeZone: TimeZoneInfo.Local
        );
    }
}
```

**Common CRON expressions:**
- `"* * * * *"` - Every minute
- `"0 * * * *"` - Every hour
- `"0 0 * * *"` - Every day at midnight
- `"0 0 * * 0"` - Every Sunday at midnight
- `"0 0 1 * *"` - First day of every month

#### Continuation Jobs

**Chain jobs together:**

```csharp
public class OrderProcessingService
{
    private readonly IJobService _jobService;

    public void ProcessOrderAsync(Guid orderId)
    {
        // First job
        var jobId = _jobService.Enqueue<ProcessPaymentJob, OrderData>(
            new OrderData { OrderId = orderId }
        );

        // Second job runs after first completes successfully
        _jobService.ContinueWith(jobId, () => SendConfirmationEmail(orderId));
    }
}
```

#### Job Monitoring

```csharp
public class JobMonitoringService
{
    private readonly IJobService _jobService;

    public JobStatistics GetStatistics()
    {
        return _jobService.GetStatistics();
        // Returns:
        // - EnqueuedCount
        // - ScheduledCount
        // - ProcessingCount
        // - SucceededCount
        // - FailedCount
        // - RecurringCount
    }

    public JobInfo? GetJobInfo(string jobId)
    {
        return _jobService.GetJob(jobId);
    }

    public bool RetryFailedJob(string jobId)
    {
        return _jobService.Requeue(jobId);
    }
}
```

---

### Caching

The framework provides a flexible caching abstraction with support for in-memory and distributed caching.

#### Configuration

```json
{
  "Caching": {
    "Provider": "Memory",  // "Memory" or "Redis"
    "DefaultDurationMinutes": 30,
    "Redis": {
      "Configuration": "localhost:6379",
      "InstanceName": "Framework:"
    }
  }
}
```

#### Basic Cache Operations

```csharp
public class ProductService
{
    private readonly ICacheService _cache;
    private readonly IRepository<Product> _repository;

    public ProductService(ICacheService cache, IRepository<Product> repository)
    {
        _cache = cache;
        _repository = repository;
    }

    // Get or create pattern
    public async Task<Product?> GetProductAsync(Guid id)
    {
        var cacheKey = $"product:{id}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                // This factory is only called if cache miss
                return await _repository.GetByIdAsync(id, ct);
            },
            options: CacheOptions.Default(TimeSpan.FromMinutes(30))
        );
    }

    // Manual get/set
    public async Task<Product?> GetProductManualAsync(Guid id)
    {
        var cacheKey = $"product:{id}";

        // Try to get from cache
        var product = await _cache.GetAsync<Product>(cacheKey);

        if (product == null)
        {
            // Cache miss - load from database
            product = await _repository.GetByIdAsync(id);

            if (product != null)
            {
                // Store in cache
                await _cache.SetAsync(cacheKey, product,
                    CacheOptions.Default(TimeSpan.FromMinutes(30)));
            }
        }

        return product;
    }
}
```

#### Cache Options

```csharp
// Absolute expiration (fixed time)
var options1 = new CacheOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
};

// Sliding expiration (resets on access)
var options2 = CacheOptions.Sliding(TimeSpan.FromMinutes(10));

// Never expire
var options3 = CacheOptions.NeverExpire();

// With priority
var options4 = new CacheOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
    Priority = CachePriority.High, // Won't be evicted when memory is low
    Tags = new List<string> { "products", "catalog" }
};
```

#### Cache Invalidation

```csharp
public class ProductService
{
    public async Task UpdateProductAsync(Product product)
    {
        // Update in database
        await _repository.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync($"product:{product.Id}");
    }

    public async Task DeleteProductAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate related caches
        await _cache.RemoveAsync($"product:{id}");
        await _cache.RemoveByPatternAsync("products:list:*"); // Wildcards supported
    }
}
```

#### Cache Patterns

**Cache-Aside (Lazy Loading):**
```csharp
var data = await _cache.GetOrCreateAsync("key", async ct => await LoadDataAsync());
```

**Write-Through:**
```csharp
await _repository.UpdateAsync(entity);
await _unitOfWork.SaveChangesAsync();
await _cache.SetAsync($"entity:{entity.Id}", entity);
```

**Write-Behind (Write-Back):**
```csharp
// Update cache immediately
await _cache.SetAsync($"entity:{entity.Id}", entity);

// Queue database update as background job
_jobService.Enqueue(() => UpdateDatabaseAsync(entity));
```

---

### Email & Notifications

The framework provides a comprehensive email service with template support and SMTP integration.

#### Configuration

```json
{
  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@example.com",
    "FromName": "Framework App",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSsl": true,
      "UseDefaultCredentials": false
    },
    "Templates": {
      "Path": "EmailTemplates",
      "CacheEnabled": true
    }
  }
}
```

#### Sending Simple Emails

```csharp
public class NotificationService
{
    private readonly IEmailService _emailService;

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        var message = EmailMessage.Create(
            to: email,
            subject: "Welcome to Framework!",
            body: $"<h1>Hello {name}</h1><p>Welcome to our platform!</p>",
            isHtml: true
        );

        var result = await _emailService.SendAsync(message);

        if (!result.IsSuccess)
        {
            Console.WriteLine($"Failed to send email: {result.ErrorMessage}");
        }
    }
}
```

#### Advanced Email Options

```csharp
public async Task SendDetailedEmailAsync()
{
    var message = new EmailMessage
    {
        Subject = "Monthly Report",
        HtmlBody = "<h1>Report</h1><p>See attachment</p>",
        TextBody = "Report - See attachment", // Fallback for non-HTML clients
        Priority = EmailPriority.High
    };

    // Multiple recipients
    message.To.Add(new EmailAddress("user1@example.com", "User One"));
    message.To.Add(new EmailAddress("user2@example.com"));
    message.Cc.Add(new EmailAddress("manager@example.com", "Manager"));
    message.Bcc.Add(new EmailAddress("archive@example.com"));

    // Attachments
    message.Attachments.Add(await EmailAttachment.FromFileAsync("report.pdf"));
    message.Attachments.Add(EmailAttachment.FromBytes(
        "data.csv",
        Encoding.UTF8.GetBytes("col1,col2\nval1,val2"),
        "text/csv"
    ));

    // Custom headers
    message.Headers["X-Campaign-Id"] = "monthly-report-2024";
    message.Tags.Add("reports");

    await _emailService.SendAsync(message);
}
```

#### Template-Based Emails

**1. Create a template file** (`EmailTemplates/welcome.html`):
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .header { background: #007bff; color: white; padding: 20px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Welcome {{Name}}!</h1>
    </div>
    <p>Thank you for registering with {{CompanyName}}.</p>
    <p>Your account is now active. <a href="{{LoginUrl}}">Login here</a></p>
</body>
</html>
```

**2. Send using template:**
```csharp
public async Task SendWelcomeEmailWithTemplateAsync(string email, string name)
{
    var model = new
    {
        Name = name,
        CompanyName = "Acme Corp",
        LoginUrl = "https://example.com/login"
    };

    await _emailService.SendTemplateAsync(
        templateName: "welcome",
        to: email,
        model: model
    );
}
```

#### Batch Email Sending

```csharp
public async Task SendNewsletterAsync(List<User> subscribers)
{
    var messages = subscribers.Select(user => EmailMessage.Create(
        to: user.Email,
        subject: "Newsletter",
        body: $"<p>Hi {user.FirstName}, here's what's new...</p>",
        isHtml: true
    ));

    var results = await _emailService.SendBatchAsync(messages);

    var failedCount = results.Count(r => !r.IsSuccess);
    Console.WriteLine($"Sent {results.Count() - failedCount} emails, {failedCount} failed");
}
```

---

### File Storage

The framework provides an abstraction for file storage with support for local file system and cloud storage (extensible to Azure Blob, AWS S3, etc.).

#### Configuration

```json
{
  "Storage": {
    "Provider": "Local",  // "Local", "InMemory", "Azure", "S3"
    "Local": {
      "RootPath": "D:\\Storage",
      "BaseUrl": "https://example.com/files"
    },
    "Azure": {
      "ConnectionString": "...",
      "ContainerName": "uploads"
    }
  }
}
```

#### Upload Files

```csharp
public class FileUploadService
{
    private readonly IStorageService _storage;

    public async Task<StoredFile> UploadProfilePictureAsync(
        Stream fileStream,
        string fileName,
        Guid userId)
    {
        var path = $"users/{userId}/profile/{fileName}";

        var result = await _storage.UploadAsync(
            path,
            fileStream,
            new UploadOptions
            {
                ContentType = "image/jpeg",
                IsPublic = true,
                Overwrite = true,
                Metadata = new Dictionary<string, string>
                {
                    ["UploadedBy"] = userId.ToString(),
                    ["UploadedAt"] = DateTime.UtcNow.ToString("O")
                }
            }
        );

        if (result.IsSuccess)
        {
            return result.Data;
        }

        throw new Exception(result.ErrorMessage);
    }
}
```

#### Download Files

```csharp
public async Task<Stream> DownloadFileAsync(string path)
{
    var result = await _storage.DownloadAsync(path);

    if (result.IsSuccess)
    {
        return result.Data;
    }

    throw new FileNotFoundException(result.ErrorMessage);
}

public async Task<byte[]> DownloadFileBytesAsync(string path)
{
    var result = await _storage.DownloadBytesAsync(path);
    return result.IsSuccess ? result.Data : Array.Empty<byte>();
}
```

#### File Operations

```csharp
public class DocumentService
{
    private readonly IStorageService _storage;

    // Check existence
    public async Task<bool> FileExistsAsync(string path)
    {
        return await _storage.ExistsAsync(path);
    }

    // Get metadata
    public async Task<StoredFile?> GetFileInfoAsync(string path)
    {
        var result = await _storage.GetMetadataAsync(path);
        return result.IsSuccess ? result.Data : null;
    }

    // List files in directory
    public async Task<List<StoredFile>> ListFilesAsync(string directory)
    {
        var result = await _storage.ListAsync(
            directory,
            new ListOptions
            {
                Recursive = true,
                Pattern = "*.pdf",
                MaxResults = 100
            }
        );

        return result.IsSuccess
            ? result.Data.ToList()
            : new List<StoredFile>();
    }

    // Copy file
    public async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        var result = await _storage.CopyAsync(sourcePath, destinationPath);

        if (!result.IsSuccess)
            throw new Exception(result.ErrorMessage);
    }

    // Move file
    public async Task MoveFileAsync(string sourcePath, string destinationPath)
    {
        await _storage.MoveAsync(sourcePath, destinationPath);
    }

    // Delete file
    public async Task DeleteFileAsync(string path)
    {
        await _storage.DeleteAsync(path);
    }
}
```

#### Generating URLs

```csharp
public async Task<string> GetPublicUrlAsync(string path)
{
    var result = await _storage.GetPublicUrlAsync(path);
    return result.IsSuccess ? result.Data : string.Empty;
}

// Temporary signed URL (expires after duration)
public async Task<string> GetTemporaryLinkAsync(string path)
{
    var result = await _storage.GetSignedUrlAsync(
        path,
        expiration: TimeSpan.FromHours(1)
    );

    return result.IsSuccess ? result.Data : string.Empty;
}
```

#### Example: File Upload API Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IStorageService _storage;
    private readonly ICurrentUser _currentUser;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var userId = _currentUser.UserId;
        var path = $"uploads/{userId}/{Guid.NewGuid()}_{file.FileName}";

        using var stream = file.OpenReadStream();

        var result = await _storage.UploadAsync(
            path,
            stream,
            new UploadOptions
            {
                ContentType = file.ContentType,
                IsPublic = false
            }
        );

        if (result.IsSuccess)
        {
            return Ok(new
            {
                path = result.Data.Path,
                name = result.Data.Name,
                size = result.Data.Size,
                contentType = result.Data.ContentType
            });
        }

        return StatusCode(500, result.ErrorMessage);
    }

    [HttpGet("download/{**path}")]
    public async Task<IActionResult> Download(string path)
    {
        var metadataResult = await _storage.GetMetadataAsync(path);
        if (!metadataResult.IsSuccess)
            return NotFound();

        var fileResult = await _storage.DownloadAsync(path);
        if (!fileResult.IsSuccess)
            return StatusCode(500);

        var metadata = metadataResult.Data;
        return File(fileResult.Data, metadata.ContentType, metadata.Name);
    }
}
```

---

### Domain Events

The framework implements the Domain Events pattern for decoupling business logic and enabling event-driven architecture.

#### Defining Domain Events

```csharp
using Framework.Domain.Events;

public class OrderPlacedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public OrderPlacedEvent(Guid orderId, Guid customerId, decimal totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}
```

#### Raising Domain Events

**From an entity:**
```csharp
public class Order : AggregateRoot
{
    public Guid CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }

    public void PlaceOrder()
    {
        Status = OrderStatus.Placed;

        // Raise domain event
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, Total));
    }
}
```

**From a handler:**
```csharp
public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, Guid>
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IEventDispatcher _eventDispatcher;

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var order = new Order(request.CustomerId, request.Items);
        order.PlaceOrder(); // Raises OrderPlacedEvent

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Dispatch events collected from the aggregate
        await _eventDispatcher.DispatchAsync(order.DomainEvents, ct);
        order.ClearDomainEvents();

        return Result.Success(order.Id);
    }
}
```

#### Handling Domain Events

**Create an event handler:**
```csharp
public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IRepository<Customer> _customerRepository;

    public OrderPlacedEventHandler(
        IEmailService emailService,
        IRepository<Customer> customerRepository)
    {
        _emailService = emailService;
        _customerRepository = customerRepository;
    }

    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        // Send confirmation email
        var customer = await _customerRepository.GetByIdAsync(@event.CustomerId, ct);

        if (customer != null)
        {
            await _emailService.SendTemplateAsync(
                "order-confirmation",
                customer.Email,
                new
                {
                    OrderId = @event.OrderId,
                    Total = @event.TotalAmount,
                    CustomerName = customer.Name
                }
            );
        }
    }
}

// Multiple handlers can handle the same event
public class OrderPlacedInventoryHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly IInventoryService _inventoryService;

    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        // Reserve inventory
        await _inventoryService.ReserveInventoryAsync(@event.OrderId);
    }
}
```

**Handlers are automatically discovered and registered** when using `AddApplicationServices()`.

#### Outbox Pattern for Reliability

The Outbox pattern ensures reliable event publishing by storing events in the database and processing them asynchronously.

**Configuration:**
```json
{
  "Outbox": {
    "Enabled": true,
    "ProcessingIntervalSeconds": 5,
    "BatchSize": 100,
    "MaxRetries": 3,
    "RetryDelaySeconds": 60,
    "CleanupIntervalHours": 24,
    "RetentionDays": 7
  }
}
```

**Using the outbox:**
```csharp
public class OrderService
{
    private readonly IOutboxService _outboxService;
    private readonly IRepository<Order> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task PlaceOrderAsync(PlaceOrderCommand command)
    {
        var order = new Order(command.CustomerId, command.Items);
        order.PlaceOrder();

        // Save order and outbox messages in the same transaction
        await _orderRepository.AddAsync(order);

        // Add domain events to outbox
        foreach (var domainEvent in order.DomainEvents)
        {
            var outboxMessage = OutboxMessage.Create(
                domainEvent,
                correlationId: Guid.NewGuid().ToString(),
                tenantId: _tenantContext.TenantId
            );

            await _outboxService.AddAsync(outboxMessage);
        }

        // Single transaction ensures atomicity
        await _unitOfWork.SaveChangesAsync();

        order.ClearDomainEvents();
    }
}
```

**Background processor handles delivery:**
```csharp
// Runs automatically every 5 seconds (configured above)
// Processes pending outbox messages and dispatches events
```

---

### Audit Logging

The framework automatically tracks entity changes (create, update, delete) with audit information.

#### Configuration

```json
{
  "Audit": {
    "IsEnabled": true,
    "LogEntityChanges": true,
    "LogRequestInfo": true,
    "RetentionDays": 90,
    "ExcludedEntityTypes": ["AuditLog"],
    "ExcludedActions": []
  }
}
```

#### Automatic Entity Tracking

**Entities that inherit from `AuditableEntity` are automatically tracked:**

```csharp
public class Product : AuditableEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// AuditableEntity provides:
// - CreatedAt (DateTime)
// - CreatedBy (string?)
// - LastModifiedAt (DateTime?)
// - LastModifiedBy (string?)
```

**When you save changes:**
```csharp
var product = new Product { Name = "Widget", Price = 19.99m };
await _repository.AddAsync(product);
await _unitOfWork.SaveChangesAsync();

// Automatically logged:
// - Action: Create
// - Entity: Product
// - EntityId: {product.Id}
// - NewValues: {"Name":"Widget","Price":19.99}
// - User: {current-user-id}
// - Timestamp: {current-time}
```

#### Manual Audit Logging

```csharp
public class OrderService
{
    private readonly IAuditService _auditService;

    public async Task ApproveOrderAsync(Guid orderId)
    {
        // Business logic...

        // Log custom action
        await _auditService.LogActionAsync(
            action: AuditAction.Update,
            entityType: "Order",
            entityId: orderId.ToString(),
            additionalInfo: "Order approved by manager"
        );
    }
}
```

#### Querying Audit Logs

```csharp
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    [HttpGet("audit/entity/{type}/{id}")]
    public async Task<IActionResult> GetEntityAuditLog(string type, string id)
    {
        var logs = await _auditService.GetEntityLogsAsync(type, id);
        return Ok(logs);
    }

    [HttpGet("audit/user/{userId}")]
    public async Task<IActionResult> GetUserAuditLog(string userId, int page = 1, int pageSize = 20)
    {
        var logs = await _auditService.GetUserLogsAsync(userId, page, pageSize);
        return Ok(logs);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> SearchAuditLogs([FromQuery] AuditLogFilter filter)
    {
        var logs = await _auditService.GetLogsAsync(filter);
        return Ok(logs);
    }
}
```

**Filter example:**
```csharp
var filter = new AuditLogFilter
{
    UserId = "user-id",
    Action = AuditAction.Delete,
    EntityType = "Product",
    FromDate = DateTimeOffset.UtcNow.AddDays(-7),
    ToDate = DateTimeOffset.UtcNow,
    PageNumber = 1,
    PageSize = 50
};

var result = await _auditService.GetLogsAsync(filter);

foreach (var log in result.Items)
{
    Console.WriteLine($"{log.Timestamp}: {log.UserName} {log.ActionName} {log.EntityType}");
    Console.WriteLine($"Old: {log.OldValues}");
    Console.WriteLine($"New: {log.NewValues}");
}
```

#### Purging Old Audit Logs

```csharp
public class AuditCleanupJob : IRecurringJob
{
    private readonly IAuditService _auditService;

    [RecurringJob("0 3 * * 0")] // Every Sunday at 3 AM
    public async Task ExecuteAsync(JobContext context)
    {
        var deletedCount = await _auditService.PurgeLogsAsync(
            olderThan: DateTimeOffset.UtcNow.AddDays(-90)
        );

        Console.WriteLine($"Purged {deletedCount} old audit logs");
    }
}
```

---

### Feature Flags

The framework includes a feature flag system for toggling features on/off at runtime without code deployments.

#### Configuration

```json
{
  "FeatureFlags": {
    "Enabled": true,
    "CacheDurationSeconds": 30,
    "Provider": "Configuration",
    "Features": {
      "NewDashboard": {
        "Enabled": true,
        "Description": "New dashboard UI"
      },
      "AdvancedReporting": {
        "Enabled": false,
        "Description": "Advanced reporting features"
      },
      "BetaFeatures": {
        "Enabled": true,
        "Filters": [
          {
            "Name": "Percentage",
            "Parameters": {
              "Value": 10
            }
          }
        ]
      }
    }
  }
}
```

#### Checking Feature Availability

```csharp
public class DashboardController : ControllerBase
{
    private readonly IFeatureManager _featureManager;

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        if (await _featureManager.IsEnabledAsync("NewDashboard"))
        {
            return Ok(new { Version = "v2", Features = new[] { "charts", "widgets" } });
        }

        return Ok(new { Version = "v1", Features = new[] { "basic" } });
    }
}
```

#### Feature Gate Attribute

**Protect endpoints with feature flags:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    [HttpGet("advanced")]
    [FeatureGate("AdvancedReporting")]
    public IActionResult GetAdvancedReport()
    {
        // Only accessible if feature is enabled
        return Ok(new { Report = "Advanced data..." });
    }

    [HttpGet("beta")]
    [FeatureGate("BetaFeature1", "BetaFeature2", Requirement = FeatureGateRequirement.Any)]
    public IActionResult GetBetaFeature()
    {
        // Accessible if ANY of the features is enabled
        return Ok();
    }
}
```

#### Per-Tenant Features

```csharp
public class FeatureService
{
    private readonly IFeatureManager _featureManager;
    private readonly ITenantContext _tenantContext;

    public async Task<bool> IsFeatureAvailableAsync(string featureName)
    {
        // Check with tenant context
        var context = new
        {
            TenantId = _tenantContext.TenantId,
            TenantName = _tenantContext.CurrentTenant?.Name
        };

        return await _featureManager.IsEnabledAsync(featureName, context);
    }
}
```

#### Feature Filters

**Percentage Filter** - Enable for X% of users:
```json
{
  "BetaFeature": {
    "Enabled": true,
    "Filters": [
      {
        "Name": "Percentage",
        "Parameters": { "Value": 25 }  // 25% of users
      }
    ]
  }
}
```

**Time Window Filter** - Enable during specific times:
```json
{
  "BlackFridaySale": {
    "Enabled": true,
    "Filters": [
      {
        "Name": "TimeWindow",
        "Parameters": {
          "Start": "2024-11-24T00:00:00Z",
          "End": "2024-11-25T23:59:59Z"
        }
      }
    ]
  }
}
```

**Targeting Filter** - Enable for specific users/tenants:
```json
{
  "PremiumFeature": {
    "Enabled": true,
    "Filters": [
      {
        "Name": "Targeting",
        "Parameters": {
          "Audience": {
            "Users": ["user-id-1", "user-id-2"],
            "Tenants": ["tenant-id-1"]
          }
        }
      }
    ]
  }
}
```

---

### Localization

The framework provides comprehensive localization support with culture-specific formatting and translation.

#### Configuration

```json
{
  "Localization": {
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "de-DE", "fr-FR", "es-ES"],
    "ResourcesPath": "Resources",
    "ResourceFileType": "Json",
    "FallbackBehavior": "ParentCulture",
    "CacheResources": true,
    "UseRequestLocalization": true,
    "CultureHeaderName": "Accept-Language",
    "CultureQueryParameterName": "culture",
    "CultureCookieName": ".AspNetCore.Culture"
  }
}
```

#### Resource Files

**Create resource files** (`Resources/en-US.json`):
```json
{
  "Welcome": "Welcome",
  "HelloUser": "Hello {0}!",
  "ProductNotFound": "Product not found",
  "ValidationError": "Validation error",
  "Orders": {
    "Created": "Order created successfully",
    "Cancelled": "Order cancelled"
  }
}
```

**German** (`Resources/de-DE.json`):
```json
{
  "Welcome": "Willkommen",
  "HelloUser": "Hallo {0}!",
  "ProductNotFound": "Produkt nicht gefunden",
  "ValidationError": "Validierungsfehler",
  "Orders": {
    "Created": "Bestellung erfolgreich erstellt",
    "Cancelled": "Bestellung storniert"
  }
}
```

#### Using Localization Service

```csharp
public class ProductController : ControllerBase
{
    private readonly ILocalizationService _localization;

    public ProductController(ILocalizationService localization)
    {
        _localization = localization;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            // Returns localized message based on request culture
            return NotFound(_localization.GetString("ProductNotFound"));
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        // With format arguments
        var message = _localization.GetString("HelloUser", request.UserName);

        return Ok(new { Message = message });
    }
}
```

#### Runtime Language Switching

**Via query string:**
```
GET /api/products?culture=de-DE
```

**Via header:**
```
Accept-Language: de-DE
```

**Via cookie:**
```
Cookie: .AspNetCore.Culture=c=de-DE|uic=de-DE
```

**Programmatically:**
```csharp
public class CultureService
{
    private readonly ILocalizationService _localization;

    public void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public string GetCurrentCulture()
    {
        return _localization.CurrentCulture.Name;
    }

    public List<string> GetSupportedCultures()
    {
        return _localization.SupportedCultures
            .Select(c => c.Name)
            .ToList();
    }
}
```

#### Typed Localization

```csharp
public class OrderService
{
    private readonly ILocalizer<OrderService> _localizer;

    public OrderService(ILocalizer<OrderService> localizer)
    {
        _localizer = localizer;
    }

    public async Task<string> PlaceOrderAsync()
    {
        // Resources/OrderService.en-US.json
        var message = _localizer["Orders.Created"];
        return message;
    }
}
```

#### Localized Validation Messages

```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly ILocalizationService _localization;

    public CreateProductCommandValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(_localization.GetString("Validation.NameRequired"));

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage(_localization.GetString("Validation.PricePositive"));
    }
}
```

---

### Health Checks

The framework provides a health check system for monitoring application and infrastructure health.

#### Configuration

```json
{
  "HealthChecks": {
    "Enabled": true,
    "Path": "/health",
    "DetailedPath": "/health/details",
    "IncludeDetails": true,
    "TimeoutSeconds": 30,
    "CacheDurationSeconds": 5
  }
}
```

#### Built-in Health Checks

The framework includes several built-in health checks:

- **Database** - Checks database connectivity
- **Memory** - Checks available memory
- **Disk Space** - Checks available disk space
- **Redis** (if configured) - Checks Redis connectivity

#### Custom Health Check

**Create a custom health check:**
```csharp
public class ApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public string Name => "External API";
    public IEnumerable<string> Tags => new[] { "external", "api" };

    public ApiHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(
                "https://api.example.com/health",
                cancellationToken
            );
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new HealthCheckResult
                {
                    Status = HealthStatus.Healthy,
                    Description = "API is reachable",
                    Duration = stopwatch.Elapsed,
                    Data =
                    {
                        ["ResponseTime"] = stopwatch.ElapsedMilliseconds,
                        ["StatusCode"] = (int)response.StatusCode
                    }
                };
            }

            return HealthCheckResult.Degraded(
                $"API returned {response.StatusCode}"
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "API is not reachable",
                ex
            );
        }
    }
}
```

**Register the health check:**
```csharp
// In Startup/Program.cs
services.AddScoped<IHealthCheck, ApiHealthCheck>();
services.AddScoped<IHealthCheck, DatabaseHealthCheck>();
services.AddScoped<IHealthCheck, CacheHealthCheck>();
```

#### Accessing Health Check Endpoints

**Basic health check:**
```bash
GET /health

Response:
{
  "status": "Healthy",
  "timestamp": "2024-12-01T10:30:00Z"
}
```

**Detailed health check:**
```bash
GET /health/details

Response:
{
  "status": "Healthy",
  "totalDuration": "00:00:01.234",
  "timestamp": "2024-12-01T10:30:00Z",
  "entries": {
    "Database": {
      "status": "Healthy",
      "description": "Database connection successful",
      "duration": "00:00:00.123",
      "data": {
        "server": "localhost",
        "database": "FrameworkDb"
      }
    },
    "External API": {
      "status": "Healthy",
      "description": "API is reachable",
      "duration": "00:00:00.456",
      "data": {
        "responseTime": 456,
        "statusCode": 200
      }
    },
    "Memory": {
      "status": "Degraded",
      "description": "Memory usage above 80%",
      "duration": "00:00:00.001",
      "data": {
        "usedMemoryMb": 1024,
        "totalMemoryMb": 2048,
        "percentageUsed": 85
      }
    }
  }
}
```

#### Integration with Monitoring

Health checks can be integrated with monitoring systems like:

- **Kubernetes** - Liveness and readiness probes
- **Docker** - HEALTHCHECK instruction
- **Application Insights** - Availability tests
- **Prometheus** - Health metrics endpoint

**Kubernetes example:**
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/details
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

---

## Best Practices

### Clean Architecture Patterns

#### 1. Keep Domain Pure

The Domain layer should have no dependencies on other layers or frameworks:

```csharp
// Good - Pure domain logic
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public void AddItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem(product, quantity));
        }

        AddDomainEvent(new OrderItemAddedEvent(Id, product.Id, quantity));
    }
}

// Bad - Domain depending on infrastructure
public class Order
{
    private readonly IEmailService _emailService; // Infrastructure dependency!

    public void PlaceOrder()
    {
        Status = OrderStatus.Placed;
        _emailService.SendAsync(...); // Business logic mixed with infrastructure
    }
}
```

#### 2. Use CQRS for All Operations

Separate commands (write) from queries (read):

```csharp
// Command - Changes state
public record CreateOrderCommand(Guid CustomerId, List<OrderItemDto> Items) : ICommand<Guid>;

// Query - Reads data
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Handler
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Create, validate, save
        return Result.Success(orderId);
    }
}
```

#### 3. Encapsulate Business Logic

Keep business rules in entities, not in services:

```csharp
// Good - Business logic in entity
public class Order
{
    public bool CanBeCancelled()
    {
        return Status != OrderStatus.Shipped
            && Status != OrderStatus.Delivered
            && Status != OrderStatus.Cancelled;
    }

    public void Cancel(string reason)
    {
        if (!CanBeCancelled())
            throw new DomainException("Order cannot be cancelled");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        AddDomainEvent(new OrderCancelledEvent(Id, reason));
    }
}

// Bad - Business logic in service
public class OrderService
{
    public async Task CancelOrderAsync(Guid orderId, string reason)
    {
        var order = await _repository.GetByIdAsync(orderId);

        // Business logic outside entity
        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            throw new Exception("Cannot cancel shipped order");

        order.Status = OrderStatus.Cancelled;
        await _repository.UpdateAsync(order);
    }
}
```

### Error Handling

#### 1. Use Result Pattern

Return results instead of throwing exceptions for expected failures:

```csharp
// Good
public async Task<Result<Order>> GetOrderAsync(Guid id)
{
    var order = await _repository.GetByIdAsync(id);

    if (order == null)
        return Result.NotFound<Order>("Order not found");

    if (!_currentUser.CanAccess(order))
        return Result.Forbidden<Order>("Access denied");

    return Result.Success(order);
}

// Usage
var result = await _orderService.GetOrderAsync(orderId);
if (result.IsSuccess)
{
    var order = result.Value;
    // Process order
}
else
{
    // Handle error
    Console.WriteLine(result.Error);
}
```

#### 2. Domain Exceptions for Invariant Violations

Use exceptions only for true exceptional cases:

```csharp
public class Order
{
    public void SetShippingAddress(Address address)
    {
        if (address == null)
            throw new DomainException("Address cannot be null");

        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot change address after shipping");

        ShippingAddress = address;
    }
}
```

#### 3. Global Exception Handler

Configure a global exception handler in the API layer:

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public async Task<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        var (statusCode, message) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, exception.Message),
            ValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "An error occurred")
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            error = message,
            timestamp = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
```

### Testing Strategies

#### 1. Unit Test Domain Logic

```csharp
public class OrderTests
{
    [Fact]
    public void AddItem_ValidProduct_AddsToOrder()
    {
        // Arrange
        var order = new Order(customerId: Guid.NewGuid());
        var product = new Product("Widget", 10m);

        // Act
        order.AddItem(product, quantity: 2);

        // Assert
        order.Items.Should().HaveCount(1);
        order.Items.First().Quantity.Should().Be(2);
        order.Total.Should().Be(20m);
    }

    [Fact]
    public void Cancel_ShippedOrder_ThrowsException()
    {
        // Arrange
        var order = new Order(customerId: Guid.NewGuid());
        order.Ship();

        // Act & Assert
        var act = () => order.Cancel("Customer request");
        act.Should().Throw<DomainException>()
            .WithMessage("Order cannot be cancelled");
    }
}
```

#### 2. Integration Test Handlers

```csharp
public class CreateOrderCommandHandlerTests : IClassFixture<TestFixture>
{
    private readonly ApplicationDbContext _context;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests(TestFixture fixture)
    {
        _context = fixture.CreateDbContext();
        _handler = new CreateOrderCommandHandler(
            new Repository<Order>(_context),
            new UnitOfWork(_context)
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrder()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<OrderItemDto>
            {
                new(ProductId: Guid.NewGuid(), Quantity: 2)
            }
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var order = await _context.Orders.FindAsync(result.Value);
        order.Should().NotBeNull();
        order.Items.Should().HaveCount(1);
    }
}
```

#### 3. API Integration Tests

```csharp
public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<OrderItemDto>
            {
                new(Guid.NewGuid(), 2)
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        orderId.Should().NotBeEmpty();
    }
}
```

---

## Configuration Reference

### Complete appsettings.json Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FrameworkDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  },

  "AllowedHosts": "*",

  "JwtSettings": {
    "Secret": "YourSuperSecretKeyHereAtLeast32CharactersLong!",
    "Issuer": "Framework",
    "Audience": "Framework.Api",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },

  "MultiTenancy": {
    "IsEnabled": true,
    "DefaultConnectionString": null,
    "TenantHeader": "X-Tenant-Id",
    "TenantQueryParam": "tenant",
    "TenantClaimType": "tenant_id",
    "Resolution": {
      "UseHeader": true,
      "UseQueryString": true,
      "UseRoute": false,
      "UseSubdomain": false,
      "UseClaim": true
    }
  },

  "Caching": {
    "Provider": "Memory",
    "DefaultDurationMinutes": 30,
    "Redis": {
      "Configuration": "localhost:6379",
      "InstanceName": "Framework:"
    }
  },

  "Email": {
    "Provider": "Smtp",
    "FromAddress": "noreply@example.com",
    "FromName": "Framework App",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSsl": true,
      "UseDefaultCredentials": false
    },
    "Templates": {
      "Path": "EmailTemplates",
      "CacheEnabled": true
    }
  },

  "Storage": {
    "Provider": "Local",
    "Local": {
      "RootPath": "D:\\Storage",
      "BaseUrl": "https://example.com/files"
    },
    "Azure": {
      "ConnectionString": "",
      "ContainerName": "uploads"
    }
  },

  "BackgroundJobs": {
    "Enabled": true,
    "ServerName": "Framework-Worker",
    "WorkerCount": 5,
    "Queues": ["default", "critical", "low-priority"],
    "Dashboard": {
      "Enabled": true,
      "Path": "/hangfire",
      "RequireAuthentication": true
    }
  },

  "Audit": {
    "IsEnabled": true,
    "LogEntityChanges": true,
    "LogRequestInfo": true,
    "RetentionDays": 90,
    "ExcludedEntityTypes": ["AuditLog"],
    "ExcludedActions": []
  },

  "FeatureFlags": {
    "Enabled": true,
    "CacheDurationSeconds": 30,
    "Provider": "Configuration",
    "Features": {
      "NewDashboard": {
        "Enabled": true,
        "Description": "New dashboard UI"
      },
      "AdvancedReporting": {
        "Enabled": false,
        "Description": "Advanced reporting features"
      },
      "BetaFeatures": {
        "Enabled": true,
        "Filters": [
          {
            "Name": "Percentage",
            "Parameters": {
              "Value": 10
            }
          }
        ]
      }
    }
  },

  "Localization": {
    "DefaultCulture": "en-US",
    "SupportedCultures": ["en-US", "de-DE", "fr-FR", "es-ES"],
    "ResourcesPath": "Resources",
    "ResourceFileType": "Json",
    "FallbackBehavior": "ParentCulture",
    "CacheResources": true,
    "UseRequestLocalization": true,
    "CultureHeaderName": "Accept-Language",
    "CultureQueryParameterName": "culture",
    "CultureCookieName": ".AspNetCore.Culture"
  },

  "HealthChecks": {
    "Enabled": true,
    "Path": "/health",
    "DetailedPath": "/health/details",
    "IncludeDetails": true,
    "TimeoutSeconds": 30,
    "CacheDurationSeconds": 5
  },

  "Outbox": {
    "Enabled": true,
    "ProcessingIntervalSeconds": 5,
    "BatchSize": 100,
    "MaxRetries": 3,
    "RetryDelaySeconds": 60,
    "CleanupIntervalHours": 24,
    "RetentionDays": 7
  }
}
```

### Environment-Specific Settings

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FrameworkDb_Dev;Trusted_Connection=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "JwtSettings": {
    "AccessTokenExpirationMinutes": 1440
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=FrameworkDb;User Id=app_user;Password=***;Encrypt=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error"
    }
  },
  "Caching": {
    "Provider": "Redis",
    "Redis": {
      "Configuration": "redis-server:6379,password=***,ssl=true"
    }
  }
}
```

---

## Summary

This developer guide covers all the major features of the framework:

1. **Multi-Tenancy** - Flexible tenant resolution and data isolation
2. **Identity & Authentication** - JWT-based auth with RBAC and permissions
3. **User Profiles** - Comprehensive user preferences and localization
4. **Background Jobs** - Hangfire integration for async processing
5. **Caching** - Memory and distributed caching abstraction
6. **Email** - Template-based email service
7. **File Storage** - Abstraction for local and cloud storage
8. **Domain Events** - Event-driven architecture with outbox pattern
9. **Audit Logging** - Automatic entity change tracking
10. **Feature Flags** - Runtime feature toggling
11. **Localization** - Multi-language support
12. **Health Checks** - Application monitoring

The framework follows **Clean Architecture** and **DDD** principles, ensuring your application remains maintainable, testable, and scalable as it grows.

For more detailed information on specific topics, refer to the individual documentation files in the `docs/` directory.
