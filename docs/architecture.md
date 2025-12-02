# Architecture Overview

The Framework is built on Clean Architecture principles, ensuring a maintainable, testable, and scalable foundation for enterprise applications.

## Clean Architecture Principles

Clean Architecture, introduced by Robert C. Martin (Uncle Bob), organizes code into layers with clear dependencies that flow inward toward the domain core.

### Core Principles

1. **Independence of Frameworks** - Business logic is not tied to any framework
2. **Testability** - Business logic can be tested without UI, database, or external services
3. **Independence of UI** - UI can change without affecting business logic
4. **Independence of Database** - Business logic is not bound to a specific database
5. **Independence of External Services** - Business logic doesn't depend on external systems

### Dependency Rule

Dependencies flow inward:
- Outer layers depend on inner layers
- Inner layers never depend on outer layers
- Domain layer has no dependencies
- Application layer depends only on Domain
- Infrastructure and API depend on Application and Domain

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                      API Layer                              │
│  ┌──────────────┬──────────────┬──────────────────────┐     │
│  │ Controllers  │ Middleware   │ Filters              │     │
│  └──────────────┴──────────────┴──────────────────────┘     │
│               HTTP Requests/Responses                       │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                  Application Layer                          │
│  ┌──────────────┬──────────────┬──────────────────────┐     │
│  │ Commands     │ Queries      │ Handlers             │     │
│  ├──────────────┼──────────────┼──────────────────────┤     │
│  │ DTOs         │ Validators   │ Behaviors            │     │
│  └──────────────┴──────────────┴──────────────────────┘     │
│               Business Workflows & Use Cases                │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  ┌──────────────┬──────────────┬──────────────────────┐     │
│  │ Entities     │ Value Objects│ Domain Events        │     │
│  ├──────────────┼──────────────┼──────────────────────┤     │
│  │ Aggregates   │ Specifications│ Domain Services     │     │
│  └──────────────┴──────────────┴──────────────────────┘     │
│               Business Logic & Domain Models                │
└─────────────────────────────────────────────────────────────┘
                           ↑
┌─────────────────────────────────────────────────────────────┐
│                Infrastructure Layer                         │
│  ┌──────────────┬──────────────┬──────────────────────┐     │
│  │ EF Core      │ Repositories │ External Services    │     │
│  ├──────────────┼──────────────┼──────────────────────┤     │
│  │ DbContext    │ Unit of Work │ File Storage         │     │
│  └──────────────┴──────────────┴──────────────────────┘     │
│          Data Access & External Integrations                │
└─────────────────────────────────────────────────────────────┘
```

## Layer Responsibilities

### 1. Domain Layer (`Framework.Domain`)

**Purpose**: Contains enterprise business logic and domain models

**Responsibilities**:
- Define business entities and their behavior
- Enforce business rules and invariants
- Define value objects for domain concepts
- Raise domain events for significant state changes
- Define domain exceptions
- Specify aggregate boundaries

**Dependencies**: None (pure C# code)

**Key Components**:
- **Entities**: Objects with identity and lifecycle
- **Value Objects**: Immutable objects defined by their attributes
- **Aggregates**: Clusters of entities with a root entity
- **Domain Events**: Notifications of domain state changes
- **Specifications**: Reusable query criteria
- **Domain Services**: Operations that don't belong to a single entity

**Example Structure**:
```
Framework.Domain/
├── Entities/
│   ├── BaseEntity.cs
│   ├── AuditableEntity.cs
│   └── Customer.cs
├── ValueObjects/
│   ├── Address.cs
│   └── Money.cs
├── Events/
│   ├── DomainEvent.cs
│   └── CustomerCreatedEvent.cs
├── Specifications/
│   └── CustomerSpecification.cs
├── Exceptions/
│   └── DomainException.cs
└── Interfaces/
    └── IAggregateRoot.cs
```

### 2. Application Layer (`Framework.Application`)

**Purpose**: Implements application use cases and orchestrates domain logic

**Responsibilities**:
- Define application use cases (commands and queries)
- Coordinate domain objects to perform tasks
- Validate input data
- Transform domain objects to DTOs
- Define application interfaces
- Implement cross-cutting concerns (logging, validation, caching)

**Dependencies**: Domain Layer only

**Key Components**:
- **Commands**: Operations that change state
- **Queries**: Operations that return data
- **Handlers**: Execute commands and queries
- **DTOs**: Data Transfer Objects for communication
- **Validators**: Input validation using FluentValidation
- **Behaviors**: Cross-cutting concerns in the MediatR pipeline

**Example Structure**:
```
Framework.Application/
├── Commands/
│   ├── CreateCustomer/
│   │   ├── CreateCustomerCommand.cs
│   │   ├── CreateCustomerCommandHandler.cs
│   │   └── CreateCustomerCommandValidator.cs
│   └── UpdateCustomer/
│       └── ...
├── Queries/
│   ├── GetCustomer/
│   │   ├── GetCustomerQuery.cs
│   │   └── GetCustomerQueryHandler.cs
│   └── GetCustomers/
│       └── ...
├── DTOs/
│   └── CustomerDto.cs
├── Mappings/
│   └── MappingProfile.cs
├── Behaviors/
│   ├── ValidationBehavior.cs
│   └── LoggingBehavior.cs
└── Interfaces/
    └── IApplicationDbContext.cs
```

### 3. Infrastructure Layer (`Framework.Infrastructure`)

**Purpose**: Provides implementations for data access and external services

**Responsibilities**:
- Implement data persistence (EF Core)
- Implement repositories
- Configure database schema
- Integrate with external services (email, storage, etc.)
- Implement caching
- Implement authentication/authorization infrastructure

**Dependencies**: Application Layer, Domain Layer

**Key Components**:
- **DbContext**: Entity Framework Core context
- **Configurations**: Entity type configurations
- **Repositories**: Data access implementations
- **Migrations**: Database schema migrations
- **Services**: External service integrations

**Example Structure**:
```
Framework.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   └── CustomerConfiguration.cs
│   ├── Migrations/
│   │   └── 20240101000000_InitialCreate.cs
│   └── Repositories/
│       ├── Repository.cs
│       └── CustomerRepository.cs
├── Services/
│   ├── EmailService.cs
│   └── FileStorageService.cs
├── Identity/
│   └── IdentityService.cs
└── DependencyInjection.cs
```

### 4. API Layer (`Framework.Api`)

**Purpose**: Exposes application functionality via RESTful APIs

**Responsibilities**:
- Handle HTTP requests and responses
- Route requests to appropriate handlers
- Perform input validation
- Handle authentication and authorization
- Format responses
- Handle errors and exceptions
- Document APIs (Swagger/OpenAPI)

**Dependencies**: Application Layer, Infrastructure Layer

**Key Components**:
- **Controllers**: API endpoints
- **Middleware**: Request/response processing
- **Filters**: Cross-cutting concerns (authorization, exception handling)
- **Extensions**: Service registration and configuration

**Example Structure**:
```
Framework.Api/
├── Controllers/
│   └── CustomersController.cs
├── Filters/
│   └── ApiExceptionFilter.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
└── Program.cs
```

## CQRS Pattern

The Framework implements Command Query Responsibility Segregation (CQRS) to separate read and write operations.

### Commands

Commands represent intentions to change state:

```csharp
public class CreateCustomerCommand : IRequest<Guid>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
```

### Queries

Queries represent requests for data:

```csharp
public class GetCustomerQuery : IRequest<CustomerDto>
{
    public Guid Id { get; set; }
}

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    private readonly IApplicationDbContext _context;

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == request.Id)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        return customer;
    }
}
```

### Benefits of CQRS

1. **Separation of Concerns**: Read and write logic are separated
2. **Optimization**: Queries can be optimized differently from commands
3. **Scalability**: Read and write sides can scale independently
4. **Simplicity**: Each handler has a single responsibility
5. **Testability**: Handlers are easy to unit test

## MediatR Pipeline

MediatR implements the Mediator pattern, providing a pipeline for request handling:

```
Request → Behavior 1 → Behavior 2 → Handler → Response
          (Logging)    (Validation)
```

### Pipeline Behaviors

Behaviors add cross-cutting concerns:

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

## Domain-Driven Design Patterns

### Entities

Objects with identity that persists over time:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
}

public class Customer : BaseEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Value Objects

Immutable objects defined by their attributes:

```csharp
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string ZipCode { get; private set; }

    public Address(string street, string city, string zipCode)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

### Domain Events

Represent significant domain occurrences:

```csharp
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}

public class CustomerCreatedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string CustomerName { get; }

    public CustomerCreatedEvent(Guid customerId, string customerName)
    {
        CustomerId = customerId;
        CustomerName = customerName;
    }
}
```

### Aggregates

Clusters of related entities with consistency boundaries:

```csharp
public class Order : BaseEntity, IAggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Price * i.Quantity);

    public void AddItem(Product product, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem(product.Id, product.Price, quantity));
        }
    }
}
```

## Dependency Injection

The Framework uses ASP.NET Core's built-in DI container:

### Service Registration

```csharp
// In Framework.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

// In Framework.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
```

### Usage in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();
app.Run();
```

## Data Flow Example

Let's trace a request through all layers:

1. **API Layer**: Receives HTTP POST to `/api/customers`
2. **Controller**: Deserializes request to `CreateCustomerCommand`
3. **MediatR**: Routes command through pipeline behaviors
4. **Validation Behavior**: Validates command using FluentValidation
5. **Logging Behavior**: Logs the command execution
6. **Command Handler**: Creates domain entity
7. **Domain Entity**: Enforces business rules
8. **Repository**: Saves entity via DbContext
9. **Response**: Returns customer ID to controller
10. **API Layer**: Returns HTTP 201 Created

## Best Practices

1. **Keep Domain Pure**: No infrastructure dependencies in Domain layer
2. **Use Interfaces**: Define contracts in inner layers, implement in outer layers
3. **Validate at Boundaries**: Validate in Application layer before domain
4. **Encapsulate Domain Logic**: Keep business rules in entities
5. **Use Value Objects**: For concepts without identity
6. **Raise Domain Events**: For significant state changes
7. **Keep Handlers Simple**: One handler per command/query
8. **Use Specifications**: For reusable query logic
9. **Follow SOLID Principles**: Especially Single Responsibility
10. **Test Each Layer**: Unit tests for domain, integration tests for infrastructure

## Common Patterns

### Repository Pattern

Abstracts data access logic:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

### Unit of Work Pattern

Manages transactions across repositories:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
```

### Specification Pattern

Encapsulates query logic:

```csharp
public class ActiveCustomersSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.IsActive && !customer.IsDeleted;
    }
}
```

## Summary

The Framework's architecture provides:

- Clear separation of concerns
- Testable and maintainable code
- Framework independence
- Flexible and extensible design
- Domain-focused development
- Scalable structure

This architecture ensures that business logic remains at the core, isolated from infrastructure concerns, making the application easier to maintain, test, and evolve over time.
