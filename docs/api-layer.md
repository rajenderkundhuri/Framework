# API Layer Guide

The API layer provides RESTful endpoints to expose application functionality. It handles HTTP requests, authentication, validation, and response formatting.

## Overview

The API layer is the presentation layer:
- **Location**: `src/Framework.Api/`
- **Dependencies**: Application and Infrastructure layers
- **Purpose**: Expose RESTful APIs
- **Technologies**: ASP.NET Core, Swagger/OpenAPI

## Key Components

### 1. Controllers

Controllers handle HTTP requests and delegate work to the Application layer via MediatR.

#### Base API Controller

```csharp
namespace Framework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected ActionResult HandleResult<T>(T result)
    {
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    protected ActionResult HandlePagedResult<T>(PaginatedResult<T> result)
    {
        Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new
        {
            result.TotalCount,
            result.PageSize,
            result.PageNumber,
            result.TotalPages
        }));

        return Ok(result.Items);
    }
}
```

#### Example Controller

```csharp
namespace Framework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ApiControllerBase
{
    /// <summary>
    /// Get a customer by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>Customer details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> GetCustomer(Guid id)
    {
        var query = new GetCustomerQuery { Id = id };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>List of customers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null)
    {
        var query = new GetCustomersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive
        };

        var result = await Mediator.Send(query);
        return HandlePagedResult(result);
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="command">Customer creation data</param>
    /// <returns>Created customer ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateCustomer([FromBody] CreateCustomerCommand command)
    {
        var customerId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, customerId);
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="command">Customer update data</param>
    /// <returns>No content</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCustomer(Guid id)
    {
        var command = new DeleteCustomerCommand { Id = id };
        await Mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Activate a customer
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <returns>No content</returns>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActivateCustomer(Guid id)
    {
        var command = new ActivateCustomerCommand { Id = id };
        await Mediator.Send(command);
        return NoContent();
    }
}
```

#### Orders Controller

```csharp
namespace Framework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ApiControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var query = new GetOrderQuery { Id = id };
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetOrdersQuery
        {
            CustomerId = customerId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await Mediator.Send(query);
        return HandlePagedResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var orderId = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, orderId);
    }

    [HttpPost("{id}/items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AddOrderItem(Guid id, [FromBody] AddOrderItemCommand command)
    {
        command.OrderId = id;
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmOrder(Guid id)
    {
        var command = new ConfirmOrderCommand { OrderId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/ship")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ShipOrder(Guid id)
    {
        var command = new ShipOrderCommand { OrderId = id };
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CancelOrder(Guid id)
    {
        var command = new CancelOrderCommand { OrderId = id };
        await Mediator.Send(command);
        return NoContent();
    }
}
```

### 2. Exception Handling

Global exception handling using middleware or filters.

#### Exception Handling Middleware

```csharp
namespace Framework.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationException => new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Errors = validationException.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Message = e.ErrorMessage
                })
            },
            EntityNotFoundException notFoundException => new
            {
                StatusCode = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Message = notFoundException.Message
            },
            DomainException domainException => new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Title = "Business Rule Violation",
                Message = domainException.Message
            },
            _ => new
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Message = "An error occurred while processing your request"
            }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

#### Exception Filter

```csharp
namespace Framework.Api.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "An exception occurred");

        var problemDetails = context.Exception switch
        {
            ValidationException validationException => new ValidationProblemDetails(
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error"
            },
            EntityNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = context.Exception.Message
            },
            DomainException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Business Rule Violation",
                Detail = context.Exception.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request"
            }
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };

        context.ExceptionHandled = true;
    }
}
```

### 3. API Versioning

Support multiple API versions.

#### Setup

```csharp
// In Program.cs or extension method
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

#### Versioned Controller

```csharp
namespace Framework.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CustomersController : ApiControllerBase
{
    // V1 endpoints
}

namespace Framework.Api.Controllers.V2;

[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class CustomersController : ApiControllerBase
{
    // V2 endpoints with different contract
}
```

### 4. Swagger/OpenAPI

API documentation with Swagger.

#### Swagger Configuration

```csharp
namespace Framework.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Framework API",
                Version = "v1",
                Description = "A clean architecture framework API",
                Contact = new OpenApiContact
                {
                    Name = "Framework Team",
                    Email = "support@framework.com"
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add JWT authentication
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Framework API V1");
            options.RoutePrefix = string.Empty; // Swagger at root
            options.DisplayRequestDuration();
        });

        return app;
    }
}
```

### 5. Authentication & Authorization

Implement JWT-based authentication.

#### JWT Configuration

```csharp
namespace Framework.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdministratorRole",
                policy => policy.RequireRole("Administrator"));
            options.AddPolicy("RequireManagerRole",
                policy => policy.RequireRole("Manager", "Administrator"));
        });

        return services;
    }
}
```

#### Protected Endpoints

```csharp
[Authorize]
[HttpPost]
public async Task<ActionResult<Guid>> CreateCustomer([FromBody] CreateCustomerCommand command)
{
    var customerId = await Mediator.Send(command);
    return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, customerId);
}

[Authorize(Roles = "Administrator")]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteCustomer(Guid id)
{
    var command = new DeleteCustomerCommand { Id = id };
    await Mediator.Send(command);
    return NoContent();
}

[Authorize(Policy = "RequireManagerRole")]
[HttpPost("{id}/approve")]
public async Task<ActionResult> ApproveOrder(Guid id)
{
    var command = new ApproveOrderCommand { OrderId = id };
    await Mediator.Send(command);
    return NoContent();
}
```

### 6. CORS Configuration

Enable Cross-Origin Resource Sharing.

```csharp
namespace Framework.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("X-Pagination");
            });
        });

        return services;
    }
}
```

### 7. Rate Limiting

Protect APIs from abuse.

```csharp
namespace Framework.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", token);
            };
        });

        return services;
    }
}
```

### 8. Program.cs Setup

Complete application configuration.

```csharp
using Framework.Api.Extensions;
using Framework.Api.Middleware;
using Framework.Application;
using Framework.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerDocumentation();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddRateLimiting();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerDocumentation();
}
else
{
    app.UseMiddleware<ExceptionHandlingMiddleware>();
}

app.UseHttpsRedirection();
app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
```

## Best Practices

### 1. Use Attribute Routing

```csharp
[HttpGet("{id}")]
[HttpPost]
[HttpPut("{id}")]
[HttpDelete("{id}")]
```

### 2. Return Proper Status Codes

```csharp
return Ok(result);           // 200
return Created(...);         // 201
return NoContent();          // 204
return BadRequest();         // 400
return NotFound();           // 404
return Unauthorized();       // 401
return Forbid();            // 403
```

### 3. Use ProducesResponseType

```csharp
[ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
```

### 4. Validate Input

Let FluentValidation handle validation in the pipeline.

### 5. Use DTOs

Never expose domain entities in API responses.

### 6. Document APIs

Use XML comments for Swagger documentation.

## Summary

The API layer provides:
- RESTful endpoints using ASP.NET Core
- Global exception handling
- API versioning support
- Swagger/OpenAPI documentation
- JWT authentication and authorization
- CORS configuration
- Rate limiting protection

This design ensures a clean, secure, and well-documented API surface.
