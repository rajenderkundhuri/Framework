# Application Layer Guide

The Application layer contains business workflows and use cases. It orchestrates domain objects to perform application-specific tasks and implements the CQRS pattern using MediatR.

## Overview

The Application layer bridges the gap between the API and Domain layers:
- **Location**: `src/Framework.Application/`
- **Dependencies**: Domain layer only
- **Purpose**: Implement use cases and business workflows
- **Pattern**: CQRS with MediatR

## Key Components

### 1. Commands (Write Operations)

Commands represent intentions to change system state.

#### Command Structure

```csharp
namespace Framework.Application.Commands.CreateCustomer;

public class CreateCustomerCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AddressDto Address { get; set; } = null!;
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
```

#### Command Handler

```csharp
namespace Framework.Application.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateCustomerCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating customer with email {Email}", request.Email);

        // Create value object
        var address = new Address(
            request.Address.Street,
            request.Address.City,
            request.Address.State,
            request.Address.ZipCode,
            request.Address.Country);

        // Create entity
        var customer = new Customer(
            request.Name,
            request.Email,
            address);

        // Save to database
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer created with ID {CustomerId}", customer.Id);

        return customer.Id;
    }
}
```

#### Command Validator

```csharp
namespace Framework.Application.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Address is required")
            .SetValidator(new AddressDtoValidator());
    }
}

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required")
            .Length(2).WithMessage("State must be 2 characters");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid zip code format");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required");
    }
}
```

#### More Command Examples

```csharp
// Update Command
namespace Framework.Application.Commands.UpdateCustomer;

public class UpdateCustomerCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (customer == null)
            throw new EntityNotFoundException(nameof(Customer), request.Id);

        customer.SetName(request.Name);
        customer.SetEmail(request.Email);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// Delete Command
namespace Framework.Application.Commands.DeleteCustomer;

public class DeleteCustomerCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public DeleteCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (customer == null)
            throw new EntityNotFoundException(nameof(Customer), request.Id);

        customer.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

### 2. Queries (Read Operations)

Queries represent requests for data without modifying state.

#### Query Structure

```csharp
namespace Framework.Application.Queries.GetCustomer;

public class GetCustomerQuery : IRequest<CustomerDto>
{
    public Guid Id { get; set; }
}
```

#### Query Handler

```csharp
namespace Framework.Application.Queries.GetCustomer;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == request.Id)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Address = new AddressDto
                {
                    Street = c.Address.Street,
                    City = c.Address.City,
                    State = c.Address.State,
                    ZipCode = c.Address.ZipCode,
                    Country = c.Address.Country
                },
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
            throw new EntityNotFoundException(nameof(Customer), request.Id);

        return customer;
    }
}
```

#### Paginated Query

```csharp
namespace Framework.Application.Queries.GetCustomers;

public class GetCustomersQuery : IRequest<PaginatedResult<CustomerDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedResult<CustomerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<CustomerDto>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Customers.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(c =>
                c.Name.Contains(request.SearchTerm) ||
                c.Email.Contains(request.SearchTerm));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CustomerDto>(
            customers,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
```

### 3. Data Transfer Objects (DTOs)

DTOs are used to transfer data between layers.

```csharp
namespace Framework.Application.DTOs;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public AddressDto? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
```

### 4. MediatR Pipeline Behaviors

Behaviors add cross-cutting concerns to the request pipeline.

#### Validation Behavior

```csharp
namespace Framework.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

#### Logging Behavior

```csharp
namespace Framework.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Handling {RequestName} with data: {@Request}",
            requestName,
            request);

        try
        {
            var response = await next();

            _logger.LogInformation(
                "Handled {RequestName} successfully",
                requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error handling {RequestName}",
                requestName);

            throw;
        }
    }
}
```

#### Performance Behavior

```csharp
namespace Framework.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500) // Log if request takes longer than 500ms
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) with data: {@Request}",
                requestName,
                elapsedMilliseconds,
                request);
        }

        return response;
    }
}
```

#### Transaction Behavior

```csharp
namespace Framework.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IApplicationDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip transaction for queries
        if (typeof(TRequest).Name.EndsWith("Query"))
            return await next();

        var requestName = typeof(TRequest).Name;

        try
        {
            _logger.LogInformation("Beginning transaction for {RequestName}", requestName);

            await _context.BeginTransactionAsync(cancellationToken);

            var response = await next();

            await _context.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception)
        {
            _logger.LogError("Rolling back transaction for {RequestName}", requestName);
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### 5. Application Interfaces

Define contracts for infrastructure implementations.

```csharp
namespace Framework.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IDateTime
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName);
    Task<Stream> DownloadFileAsync(string fileUrl);
    Task DeleteFileAsync(string fileUrl);
}
```

### 6. Mapping Profiles

Use AutoMapper for object mapping (optional).

```csharp
namespace Framework.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.Address, opt => opt.MapFrom(s => s.Address));

        CreateMap<Address, AddressDto>();

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Total, opt => opt.MapFrom(s => s.Total.Amount))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => s.UnitPrice.Amount))
            .ForMember(d => d.Subtotal, opt => opt.MapFrom(s => s.Subtotal.Amount));
    }
}
```

### 7. Dependency Injection Setup

```csharp
namespace Framework.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviors (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Register AutoMapper (optional)
        services.AddAutoMapper(assembly);

        return services;
    }
}
```

## Best Practices

### 1. Command/Query Naming

Follow consistent naming conventions:

```csharp
// Commands (verbs)
CreateCustomerCommand
UpdateCustomerCommand
DeleteCustomerCommand
ActivateCustomerCommand

// Queries (get/list)
GetCustomerQuery
GetCustomersQuery
GetCustomersByStatusQuery
```

### 2. Single Responsibility

Each handler should do one thing:

```csharp
// Good - Single responsibility
public class CreateOrderCommand : IRequest<Guid> { }
public class ConfirmOrderCommand : IRequest<Unit> { }

// Bad - Multiple responsibilities
public class CreateAndConfirmOrderCommand : IRequest<Guid> { }
```

### 3. Validation Rules

Keep validation in validators, not handlers:

```csharp
// Good
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
    }
}

// Bad - validation in handler
public async Task<Guid> Handle(CreateCustomerCommand request, ...)
{
    if (!IsValidEmail(request.Email))
        throw new Exception("Invalid email");
}
```

### 4. Keep Handlers Thin

Delegate domain logic to entities:

```csharp
// Good
public async Task<Unit> Handle(ConfirmOrderCommand request, ...)
{
    var order = await _context.Orders.FindAsync(request.Id);
    order.Confirm(); // Domain logic in entity
    await _context.SaveChangesAsync();
    return Unit.Value;
}

// Bad - domain logic in handler
public async Task<Unit> Handle(ConfirmOrderCommand request, ...)
{
    var order = await _context.Orders.FindAsync(request.Id);
    if (order.Status != OrderStatus.Pending)
        throw new Exception("...");
    order.Status = OrderStatus.Confirmed;
    // More business logic...
}
```

### 5. Use DTOs for Output

Never expose domain entities directly:

```csharp
// Good
public class GetCustomerQuery : IRequest<CustomerDto> { }

// Bad
public class GetCustomerQuery : IRequest<Customer> { }
```

### 6. Handle Domain Events

Process domain events after persistence:

```csharp
public async Task<Guid> Handle(CreateOrderCommand request, ...)
{
    var order = new Order(...);
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();

    // Publish domain events
    foreach (var domainEvent in order.DomainEvents)
    {
        await _mediator.Publish(domainEvent);
    }

    order.ClearDomainEvents();
    return order.Id;
}
```

## Testing Commands and Queries

### Testing Commands

```csharp
public class CreateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomer()
    {
        // Arrange
        var context = CreateDbContext();
        var handler = new CreateCustomerCommandHandler(context, Mock.Of<ILogger>());
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = new AddressDto { /* ... */ }
        };

        // Act
        var customerId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var customer = await context.Customers.FindAsync(customerId);
        Assert.NotNull(customer);
        Assert.Equal("John Doe", customer.Name);
    }
}
```

### Testing Validators

```csharp
public class CreateCustomerCommandValidatorTests
{
    [Fact]
    public void Validate_EmptyName_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateCustomerCommandValidator();
        var command = new CreateCustomerCommand { Name = "" };

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }
}
```

## Summary

The Application layer provides:
- CQRS pattern implementation with MediatR
- Validation using FluentValidation
- Cross-cutting concerns via pipeline behaviors
- DTOs for data transfer
- Orchestration of domain logic
- Clean separation between reads and writes

This design ensures that use cases are well-defined, testable, and maintainable.
