# Coding Standards & Naming Conventions

This document defines the coding standards, naming conventions, and best practices for the Framework project.

## Table of Contents

1. [General Principles](#general-principles)
2. [Naming Conventions](#naming-conventions)
3. [Code Formatting](#code-formatting)
4. [Project Structure](#project-structure)
5. [Architecture Guidelines](#architecture-guidelines)
6. [C# Coding Standards](#c-coding-standards)
7. [API Standards](#api-standards)
8. [Database Standards](#database-standards)
9. [Testing Standards](#testing-standards)
10. [Git Conventions](#git-conventions)
11. [Code Review Guidelines](#code-review-guidelines)

---

## General Principles

### Core Values

1. **Readability** - Code is read more often than it's written
2. **Simplicity** - Prefer simple solutions over clever ones
3. **Consistency** - Follow established patterns throughout the codebase
4. **Maintainability** - Write code that's easy to modify and extend
5. **Testability** - Design for unit testing from the start

### DRY, KISS, YAGNI

- **DRY (Don't Repeat Yourself)** - Extract common logic into reusable components
- **KISS (Keep It Simple, Stupid)** - Avoid unnecessary complexity
- **YAGNI (You Aren't Gonna Need It)** - Don't add features until they're needed

---

## Naming Conventions

### General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Namespaces | PascalCase | `Framework.Domain.Products` |
| Classes | PascalCase | `ProductService` |
| Interfaces | IPascalCase | `IProductService` |
| Methods | PascalCase | `GetProductAsync` |
| Properties | PascalCase | `ProductName` |
| Constants | PascalCase | `MaxRetryCount` |
| Private fields | _camelCase | `_productRepository` |
| Parameters | camelCase | `productId` |
| Local variables | camelCase | `totalAmount` |
| Enums | PascalCase | `OrderStatus` |
| Enum values | PascalCase | `OrderStatus.Pending` |

### Specific Patterns

#### Classes and Interfaces

```csharp
// Entities - noun, singular
public class Product { }
public class Order { }
public class User { }

// Services - noun + "Service"
public interface IProductService { }
public class ProductService : IProductService { }

// Repositories - noun + "Repository"
public interface IProductRepository { }
public class ProductRepository : IProductRepository { }

// Controllers - noun + "Controller"
public class ProductsController : ControllerBase { }

// Commands - verb + noun + "Command"
public record CreateProductCommand { }
public record UpdateOrderCommand { }
public record DeleteUserCommand { }

// Queries - "Get" + noun + "Query"
public record GetProductQuery { }
public record GetOrdersQuery { }

// Command/Query Handlers - command/query name + "Handler"
public class CreateProductCommandHandler { }
public class GetProductQueryHandler { }

// DTOs/Responses - noun + "Response" or "Request"
public class ProductResponse { }
public class CreateProductRequest { }
public class ProductListRequest { }

// Validators - request name + "Validator"
public class CreateProductRequestValidator { }

// Events - noun + past tense verb + "Event"
public class ProductCreatedEvent { }
public class OrderShippedEvent { }
public class UserRegisteredEvent { }

// Event Handlers - event name + "Handler"
public class ProductCreatedEventHandler { }

// Specifications - descriptive name + "Spec"
public class ActiveProductsSpec { }
public class ProductsByCategorySpec { }

// Exceptions - descriptive name + "Exception"
public class ProductNotFoundException { }
public class InvalidOrderStateException { }
```

#### Methods

```csharp
// Async methods - end with "Async"
Task<Product> GetProductAsync(Guid id);
Task CreateOrderAsync(Order order);
Task<bool> ValidateUserAsync(string email);

// Boolean methods/properties - use "Is", "Has", "Can", "Should"
bool IsActive { get; }
bool HasPermission(string permission);
bool CanBeCancelled();
bool ShouldNotify();

// Collection methods - use plural nouns
List<Product> GetProducts();
IEnumerable<Order> GetOrdersByCustomer(Guid customerId);

// Factory methods - use "Create" or "Build"
Product CreateProduct(string name, decimal price);
Order BuildOrder(OrderBuilder builder);

// Conversion methods - use "To" prefix
ProductDto ToDto();
string ToString();
int ToInt();

// Query methods - use "Get", "Find", "Search"
Product? GetById(Guid id);
Product? FindByName(string name);
List<Product> SearchProducts(string term);

// Command methods - use imperative verbs
void Activate();
void Deactivate();
void UpdatePrice(decimal newPrice);
void Cancel(string reason);
```

#### Variables and Fields

```csharp
// Private fields - underscore prefix
private readonly IProductRepository _productRepository;
private readonly ILogger<ProductService> _logger;
private int _retryCount;

// Local variables - descriptive camelCase
var activeProducts = await GetActiveProductsAsync();
var totalAmount = order.Items.Sum(i => i.Price);
var isValid = validator.Validate(request);

// Loop variables - single letter only for simple iterations
for (int i = 0; i < items.Count; i++) { }
foreach (var item in items) { }

// LINQ variables - descriptive names
var expiredOrders = orders.Where(o => o.ExpiresAt < DateTime.UtcNow);
var customerNames = customers.Select(c => c.Name);
```

### File Naming

| File Type | Convention | Example |
|-----------|------------|---------|
| Entity | `{EntityName}.cs` | `Product.cs` |
| Interface | `I{Name}.cs` | `IProductService.cs` |
| Service | `{Name}Service.cs` | `ProductService.cs` |
| Controller | `{Name}Controller.cs` | `ProductsController.cs` |
| Command | `{Verb}{Noun}Command.cs` | `CreateProductCommand.cs` |
| Query | `Get{Noun}Query.cs` | `GetProductQuery.cs` |
| Handler | `{Command/Query}Handler.cs` | `CreateProductCommandHandler.cs` |
| Validator | `{Request}Validator.cs` | `CreateProductRequestValidator.cs` |
| Test | `{ClassName}Tests.cs` | `ProductServiceTests.cs` |
| Configuration | `{Entity}Configuration.cs` | `ProductConfiguration.cs` |
| Razor Page | `{PageName}.razor` | `Products.razor` |
| Dialog | `{Name}Dialog.razor` | `ProductDialog.razor` |

### Folder Structure

```
src/
├── Framework.Domain/
│   └── {Module}/                    # e.g., Products, Orders
│       ├── {Entity}.cs              # Product.cs
│       ├── Events/
│       │   └── {Event}.cs           # ProductCreatedEvent.cs
│       └── Specifications/
│           └── {Spec}.cs            # ActiveProductsSpec.cs
│
├── Framework.Application/
│   └── {Module}/                    # e.g., Products
│       ├── I{Module}Service.cs      # IProductService.cs
│       ├── {Response}Response.cs    # ProductResponse.cs
│       ├── {Request}Request.cs      # CreateProductRequest.cs
│       └── {Request}Validator.cs    # CreateProductRequestValidator.cs
│
├── Framework.Infrastructure/
│   └── {Module}/                    # e.g., Products
│       └── {Service}.cs             # ProductService.cs
│
├── Framework.Api/
│   └── Controllers/
│       └── {Module}Controller.cs    # ProductsController.cs
│
└── Framework.Admin/
    ├── Pages/
    │   └── {Module}/                # e.g., Products
    │       ├── {Module}.razor       # Products.razor
    │       └── {Dialog}.razor       # ProductDialog.razor
    └── Services/
        └── {Module}ApiService.cs    # ProductApiService.cs
```

---

## Code Formatting

### Indentation and Spacing

```csharp
// Use 4 spaces for indentation (no tabs)
public class Product
{
    private readonly string _name;

    public Product(string name)
    {
        _name = name;
    }
}

// Blank line between logical sections
public async Task<Result<Guid>> CreateAsync(CreateProductRequest request)
{
    // Validate
    var validation = await _validator.ValidateAsync(request);
    if (!validation.IsValid)
        return Result<Guid>.Failure(validation.Errors);

    // Create entity
    var product = new Product(request.Name, request.Price);

    // Save
    await _repository.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();

    return Result<Guid>.Success(product.Id);
}

// Single blank line between members
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _orderRepository.GetByIdAsync(id);
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _orderRepository.GetAllAsync();
    }
}
```

### Braces

```csharp
// Always use braces for control statements
if (condition)
{
    DoSomething();
}

// Exception: simple single-line statements
if (product == null)
    return Result.NotFound();

// Multi-line conditions - each condition on new line
if (order.Status == OrderStatus.Pending &&
    order.CreatedAt < DateTime.UtcNow.AddDays(-7) &&
    !order.HasPayment)
{
    order.Cancel("Order expired");
}
```

### Line Length

- **Maximum line length**: 120 characters
- Break long lines at logical points

```csharp
// Break method calls
var result = await _productService
    .GetProductsAsync(request, cancellationToken);

// Break LINQ queries
var activeProducts = products
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .Select(p => new ProductDto(p.Id, p.Name))
    .ToList();

// Break long parameter lists
public async Task<Result<Guid>> CreateOrderAsync(
    Guid customerId,
    List<OrderItem> items,
    Address shippingAddress,
    PaymentMethod paymentMethod,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Using Statements

```csharp
// Order: System → Microsoft → Third-party → Project
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using FluentValidation;
using MediatR;

using Framework.Application.Common.Models;
using Framework.Domain.Products;
```

---

## Project Structure

### Layer Dependencies

```
┌─────────────────────────────────────┐
│         Framework.Api               │  ← Entry point
│  (Controllers, Middleware)          │
└─────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│      Framework.Application          │  ← Business logic orchestration
│  (Services, DTOs, Validators)       │
└─────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│       Framework.Domain              │  ← Core business logic
│  (Entities, Value Objects, Events)  │  ← NO external dependencies
└─────────────────────────────────────┘
              ▲
              │
┌─────────────────────────────────────┐
│    Framework.Infrastructure         │  ← Technical implementations
│  (EF Core, External Services)       │
└─────────────────────────────────────┘
```

### Allowed References

| Project | Can Reference |
|---------|---------------|
| Domain | Nothing (pure domain logic) |
| Application | Domain |
| Infrastructure | Domain, Application |
| Api | Domain, Application, Infrastructure |
| Admin | Application (DTOs only) |
| Tests | All projects |

---

## Architecture Guidelines

### Domain Layer

```csharp
// DO: Rich domain models with behavior
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify a submitted order");

        var existingItem = _items.Find(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(new OrderItem(product, quantity));
        }

        RecalculateTotal();
    }
}

// DON'T: Anemic domain models
public class Order
{
    public List<OrderItem> Items { get; set; } = new(); // Public setter!
    public decimal Total { get; set; } // Logic elsewhere
}
```

### Application Layer

```csharp
// DO: Thin handlers that orchestrate domain logic
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result.NotFound<Guid>("Customer not found");

        var order = customer.PlaceOrder(request.Items); // Domain logic in entity

        await _orderRepository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(order.Id);
    }
}

// DON'T: Business logic in handlers
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Business logic should be in domain entities, not here
        if (request.Items.Sum(i => i.Quantity) > 100)
            return Result.Failure<Guid>("Too many items");

        var order = new Order();
        order.CustomerId = request.CustomerId;
        order.Status = OrderStatus.Pending; // Setting status directly!
        // ...
    }
}
```

### Infrastructure Layer

```csharp
// DO: Implement application interfaces
public class EmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly ILogger<EmailService> _logger;

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            await _smtpClient.SendMailAsync(message.ToMailMessage(), ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.To);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }
}

// DON'T: Leak infrastructure concerns to application layer
public interface IEmailService
{
    Task SendAsync(MailMessage message); // MailMessage is System.Net.Mail!
}
```

---

## C# Coding Standards

### Use Modern C# Features

```csharp
// Use records for DTOs
public record ProductResponse(Guid Id, string Name, decimal Price);

// Use file-scoped namespaces
namespace Framework.Domain.Products;

public class Product { }

// Use target-typed new
private readonly List<Product> _products = new();

// Use pattern matching
public string GetStatusMessage(Order order) => order.Status switch
{
    OrderStatus.Draft => "Order is being prepared",
    OrderStatus.Pending => "Awaiting payment",
    OrderStatus.Confirmed => "Order confirmed",
    OrderStatus.Shipped => "On the way",
    OrderStatus.Delivered => "Delivered",
    OrderStatus.Cancelled => "Order was cancelled",
    _ => "Unknown status"
};

// Use null-conditional and null-coalescing
var name = user?.Profile?.DisplayName ?? user?.Email ?? "Unknown";

// Use collection expressions (C# 12)
List<int> numbers = [1, 2, 3, 4, 5];

// Use primary constructors (C# 12)
public class ProductService(IProductRepository repository, ILogger<ProductService> logger)
{
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        logger.LogInformation("Getting product {ProductId}", id);
        return await repository.GetByIdAsync(id);
    }
}
```

### Async/Await

```csharp
// Always use async/await for I/O operations
public async Task<Product?> GetProductAsync(Guid id, CancellationToken ct)
{
    return await _context.Products.FindAsync(new object[] { id }, ct);
}

// Use ConfigureAwait(false) in library code
public async Task<string> ReadFileAsync(string path)
{
    return await File.ReadAllTextAsync(path).ConfigureAwait(false);
}

// Pass CancellationToken through the call chain
public async Task<Result<Order>> ProcessOrderAsync(
    ProcessOrderCommand command,
    CancellationToken cancellationToken)
{
    var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
    // ...
}

// Don't block on async code
// BAD: Can cause deadlocks
var result = GetProductAsync(id).Result;

// GOOD: Use async all the way
var result = await GetProductAsync(id);
```

### Exception Handling

```csharp
// Use specific exception types
public class ProductNotFoundException : Exception
{
    public Guid ProductId { get; }

    public ProductNotFoundException(Guid productId)
        : base($"Product with ID {productId} was not found")
    {
        ProductId = productId;
    }
}

// Prefer Result pattern over exceptions for expected failures
public async Task<Result<Product>> GetProductAsync(Guid id)
{
    var product = await _repository.GetByIdAsync(id);

    if (product == null)
        return Result.NotFound<Product>($"Product {id} not found");

    if (!product.IsActive)
        return Result.Failure<Product>("Product is not available");

    return Result.Success(product);
}

// Log exceptions with context
try
{
    await ProcessPaymentAsync(order);
}
catch (PaymentException ex)
{
    _logger.LogError(ex,
        "Payment failed for order {OrderId}, customer {CustomerId}, amount {Amount}",
        order.Id, order.CustomerId, order.Total);
    throw;
}
```

### Dependency Injection

```csharp
// Use constructor injection
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// Register with appropriate lifetime
services.AddScoped<IOrderService, OrderService>();      // Per-request
services.AddSingleton<ICacheService, MemoryCacheService>(); // Singleton
services.AddTransient<IEmailBuilder, EmailBuilder>();   // Per-resolution
```

---

## API Standards

### Endpoint Naming

```csharp
// Use plural nouns for resources
[Route("api/products")]      // Good
[Route("api/product")]       // Bad

// Use kebab-case for multi-word resources
[Route("api/order-items")]   // Good
[Route("api/orderItems")]    // Bad

// Nest related resources
[Route("api/orders/{orderId}/items")]
```

### HTTP Methods

| Method | Purpose | Example |
|--------|---------|---------|
| GET | Retrieve resource(s) | `GET /api/products` |
| POST | Create resource | `POST /api/products` |
| PUT | Update entire resource | `PUT /api/products/{id}` |
| PATCH | Partial update | `PATCH /api/products/{id}` |
| DELETE | Remove resource | `DELETE /api/products/{id}` |

### Response Codes

```csharp
// 200 OK - Successful GET, PUT, PATCH
return Ok(product);

// 201 Created - Successful POST
return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);

// 204 No Content - Successful DELETE
return NoContent();

// 400 Bad Request - Validation failure
return BadRequest(new { errors = validationErrors });

// 401 Unauthorized - Not authenticated
return Unauthorized();

// 403 Forbidden - Not authorized
return Forbid();

// 404 Not Found - Resource doesn't exist
return NotFound();

// 409 Conflict - Business rule violation
return Conflict(new { message = "Order already submitted" });

// 500 Internal Server Error - Unexpected error
return StatusCode(500, new { message = "An error occurred" });
```

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of products with optional filtering
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductListRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsView))
            return Forbid();

        var result = await _productService.GetProductsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ProductsView))
            return Forbid();

        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return product != null ? Ok(product) : NotFound();
    }

    // ... other actions
}
```

---

## Database Standards

### Table Naming

```csharp
// Table names: PascalCase, plural
builder.ToTable("Products");
builder.ToTable("OrderItems");
builder.ToTable("UserRoles");

// Column names: PascalCase
builder.Property(p => p.ProductName);
builder.Property(p => p.CreatedAt);
```

### Entity Configuration

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Table
        builder.ToTable("Products");

        // Primary key
        builder.HasKey(p => p.Id);

        // Required fields first, then optional
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // Indexes
        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");

        builder.HasIndex(p => p.CategoryId);

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Migrations

```bash
# Migration naming: descriptive, PascalCase
dotnet ef migrations add AddProductsTable
dotnet ef migrations add AddIndexToProductSku
dotnet ef migrations add AddOrderItemsRelationship
```

---

## Testing Standards

### Test Naming

```csharp
// Format: {Method}_{Scenario}_{ExpectedResult}
[Fact]
public void AddItem_WithValidProduct_IncreasesItemCount()

[Fact]
public async Task CreateAsync_WithDuplicateSku_ReturnsFailure()

[Fact]
public void SetPrice_WithNegativeValue_ThrowsDomainException()

// For parameterized tests
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void SetName_WithInvalidValue_ThrowsDomainException(string? invalidName)
```

### Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task CreateOrderAsync_WithValidData_ReturnsSuccessWithOrderId()
{
    // Arrange
    var customer = new Customer("John Doe", "john@example.com");
    var items = new List<OrderItem>
    {
        new(productId: Guid.NewGuid(), quantity: 2, price: 29.99m)
    };
    var command = new CreateOrderCommand(customer.Id, items);

    _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>())
        .Returns(customer);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBe(Guid.Empty);
    await _orderRepository.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
}
```

### Test Organization

```
tests/
├── Framework.Domain.Tests/
│   └── Products/
│       ├── ProductTests.cs
│       └── Specifications/
│           └── ActiveProductsSpecTests.cs
│
├── Framework.Application.Tests/
│   └── Products/
│       ├── CreateProductRequestValidatorTests.cs
│       └── ProductServiceTests.cs
│
└── Framework.Infrastructure.Tests/
    └── Products/
        └── ProductRepositoryTests.cs
```

---

## Git Conventions

### Branch Naming

```
feature/{ticket-id}-{short-description}
bugfix/{ticket-id}-{short-description}
hotfix/{ticket-id}-{short-description}
release/{version}

Examples:
feature/PRJ-123-add-product-module
bugfix/PRJ-456-fix-order-calculation
hotfix/PRJ-789-security-patch
release/v2.1.0
```

### Commit Messages

```
# Format: <type>(<scope>): <description>

# Types:
feat:     New feature
fix:      Bug fix
docs:     Documentation only
style:    Code style (formatting, missing semicolons)
refactor: Code refactoring
test:     Adding or updating tests
chore:    Build, CI, dependencies

# Examples:
feat(products): add product search functionality
fix(orders): correct total calculation for discounts
docs(readme): update installation instructions
refactor(auth): extract token validation logic
test(products): add unit tests for ProductService
chore(deps): update MediatR to v12.0.0
```

### Pull Request Guidelines

1. **Title**: Follow commit message format
2. **Description**: Include:
   - Summary of changes
   - Related ticket/issue
   - Testing performed
   - Screenshots (for UI changes)
3. **Size**: Keep PRs small and focused (<400 lines ideally)
4. **Reviews**: Require at least one approval

---

## Code Review Guidelines

### What to Look For

1. **Correctness**
   - Does the code work as intended?
   - Are edge cases handled?
   - Are there potential bugs?

2. **Architecture**
   - Is code in the right layer?
   - Does it follow established patterns?
   - Is there proper separation of concerns?

3. **Maintainability**
   - Is the code readable?
   - Are names descriptive?
   - Is there unnecessary complexity?

4. **Testing**
   - Are there adequate tests?
   - Do tests cover edge cases?
   - Are tests well-structured?

5. **Security**
   - Is input validated?
   - Are there SQL injection risks?
   - Is sensitive data protected?

6. **Performance**
   - Are there N+1 query issues?
   - Is caching used appropriately?
   - Are there unnecessary database calls?

### Review Comments

```csharp
// Suggest improvements constructively
// Instead of: "This is wrong"
// Say: "Consider using the Result pattern here to handle the failure case explicitly"

// Ask questions to understand intent
// "What's the reason for this approach? I was thinking we could also..."

// Provide examples when suggesting changes
// "You could simplify this using pattern matching:
//  return status switch { ... };"
```

---

## Summary

Following these standards ensures:

- **Consistent** codebase that's easy to navigate
- **Readable** code that communicates intent
- **Maintainable** solutions that can evolve
- **Testable** components with clear boundaries
- **Professional** quality suitable for enterprise use

When in doubt, look at existing code patterns in the codebase and follow the established conventions.
