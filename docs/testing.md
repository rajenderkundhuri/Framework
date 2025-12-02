# Testing Guide

Comprehensive testing ensures code quality, reliability, and maintainability. The Framework supports unit tests, integration tests, and end-to-end tests using xUnit, Moq, and other testing tools.

## Overview

Testing strategy:
- **Location**: `tests/` directory
- **Framework**: xUnit
- **Mocking**: Moq
- **Assertions**: FluentAssertions (recommended)
- **Coverage**: Code coverage tools

## Project Structure

```
tests/
├── Framework.Domain.Tests/          # Domain layer tests
│   ├── Entities/
│   ├── ValueObjects/
│   └── Specifications/
├── Framework.Application.Tests/     # Application layer tests
│   ├── Commands/
│   ├── Queries/
│   └── Validators/
└── Framework.Api.Tests/            # API layer tests
    ├── Controllers/
    └── Integration/
```

## Unit Testing

### 1. Testing Domain Entities

Domain entities should be tested for business logic and invariant enforcement.

#### Entity Tests Example

```csharp
namespace Framework.Domain.Tests.Entities;

public class CustomerTests
{
    [Fact]
    public void Constructor_ValidData_CreatesCustomer()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act
        var customer = new Customer(name, email, address);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(name, customer.Name);
        Assert.Equal(email, customer.Email);
        Assert.Equal(address, customer.Address);
        Assert.True(customer.IsActive);
        Assert.False(customer.IsDeleted);
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsDomainException()
    {
        // Arrange
        var name = "";
        var email = "john@example.com";
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act & Assert
        Assert.Throws<DomainException>(() => new Customer(name, email, address));
    }

    [Fact]
    public void SetEmail_ValidEmail_UpdatesEmail()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var newEmail = "newemail@example.com";

        // Act
        customer.SetEmail(newEmail);

        // Assert
        Assert.Equal(newEmail, customer.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalidemail")]
    [InlineData("invalid@")]
    public void SetEmail_InvalidEmail_ThrowsDomainException(string invalidEmail)
    {
        // Arrange
        var customer = CreateValidCustomer();

        // Act & Assert
        Assert.Throws<DomainException>(() => customer.SetEmail(invalidEmail));
    }

    [Fact]
    public void Deactivate_ActiveCustomer_SetsIsActiveToFalse()
    {
        // Arrange
        var customer = CreateValidCustomer();
        Assert.True(customer.IsActive);

        // Act
        customer.Deactivate();

        // Assert
        Assert.False(customer.IsActive);
    }

    [Fact]
    public void Delete_Customer_SetsIsDeletedAndIsActiveToFalse()
    {
        // Arrange
        var customer = CreateValidCustomer();

        // Act
        customer.Delete();

        // Assert
        Assert.True(customer.IsDeleted);
        Assert.False(customer.IsActive);
    }

    [Fact]
    public void Activate_DeletedCustomer_ThrowsDomainException()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.Delete();

        // Act & Assert
        Assert.Throws<DomainException>(() => customer.Activate());
    }

    private static Customer CreateValidCustomer()
    {
        return new Customer(
            "John Doe",
            "john@example.com",
            new Address("123 Main St", "New York", "NY", "10001", "USA"));
    }
}
```

#### Order Aggregate Tests

```csharp
namespace Framework.Domain.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void Constructor_ValidData_CreatesOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderNumber = "ORD-001";

        // Act
        var order = new Order(customerId, orderNumber);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(orderNumber, order.OrderNumber);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void AddItem_ValidItem_AddsToOrder()
    {
        // Arrange
        var order = CreateValidOrder();
        var productId = Guid.NewGuid();
        var price = new Money(10.00m, "USD");

        // Act
        order.AddItem(productId, "Product 1", price, 2);

        // Assert
        Assert.Single(order.Items);
        var item = order.Items.First();
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public void AddItem_SameProductTwice_IncreasesQuantity()
    {
        // Arrange
        var order = CreateValidOrder();
        var productId = Guid.NewGuid();
        var price = new Money(10.00m, "USD");

        // Act
        order.AddItem(productId, "Product 1", price, 2);
        order.AddItem(productId, "Product 1", price, 3);

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
    }

    [Fact]
    public void Confirm_PendingOrder_ChangesStatusToConfirmed()
    {
        // Arrange
        var order = CreateValidOrder();
        order.AddItem(Guid.NewGuid(), "Product 1", new Money(10, "USD"), 1);

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_EmptyOrder_ThrowsDomainException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Ship_ConfirmedOrder_ChangesStatusToShipped()
    {
        // Arrange
        var order = CreateValidOrder();
        order.AddItem(Guid.NewGuid(), "Product 1", new Money(10, "USD"), 1);
        order.Confirm();

        // Act
        order.Ship();

        // Assert
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.NotNull(order.ShippedAt);
    }

    [Fact]
    public void Ship_PendingOrder_ThrowsDomainException()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act & Assert
        Assert.Throws<DomainException>(() => order.Ship());
    }

    private static Order CreateValidOrder()
    {
        return new Order(Guid.NewGuid(), "ORD-001");
    }
}
```

### 2. Testing Value Objects

```csharp
namespace Framework.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidAmount_CreatesMoney()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        Assert.Equal(100.50m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Constructor_NegativeAmount_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(-10, "USD"));
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        Assert.Equal(150, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsException()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1.Add(money2));
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(100, "USD");

        // Act & Assert
        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var money1 = new Money(100, "USD");
        var money2 = new Money(50, "USD");

        // Act & Assert
        Assert.NotEqual(money1, money2);
    }
}
```

### 3. Testing Command Handlers

Use Moq to mock dependencies.

```csharp
namespace Framework.Application.Tests.Commands;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ILogger<CreateCustomerCommandHandler>> _loggerMock;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<CreateCustomerCommandHandler>>();
        _handler = new CreateCustomerCommandHandler(_contextMock.Object, _loggerMock.Object);

        // Setup mock DbSet
        var customers = new List<Customer>().AsQueryable();
        var mockSet = new Mock<DbSet<Customer>>();
        mockSet.As<IQueryable<Customer>>().Setup(m => m.Provider).Returns(customers.Provider);
        mockSet.As<IQueryable<Customer>>().Setup(m => m.Expression).Returns(customers.Expression);

        _contextMock.Setup(c => c.Customers).Returns(mockSet.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomer()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        _contextMock.Verify(c => c.Customers.Add(It.IsAny<Customer>()), Times.Once);
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 4. Testing Query Handlers

```csharp
namespace Framework.Application.Tests.Queries;

public class GetCustomerQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly GetCustomerQueryHandler _handler;

    public GetCustomerQueryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new GetCustomerQueryHandler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsCustomerDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(
            "John Doe",
            "john@example.com",
            new Address("123 Main St", "New York", "NY", "10001", "USA"));

        var customers = new List<Customer> { customer }.AsQueryable();
        var mockSet = CreateMockDbSet(customers);

        _contextMock.Setup(c => c.Customers).Returns(mockSet.Object);

        var query = new GetCustomerQuery { Id = customerId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task Handle_NonExistingCustomer_ThrowsNotFoundException()
    {
        // Arrange
        var customers = new List<Customer>().AsQueryable();
        var mockSet = CreateMockDbSet(customers);

        _contextMock.Setup(c => c.Customers).Returns(mockSet.Object);

        var query = new GetCustomerQuery { Id = Guid.NewGuid() };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    private static Mock<DbSet<Customer>> CreateMockDbSet(IQueryable<Customer> data)
    {
        var mockSet = new Mock<DbSet<Customer>>();
        mockSet.As<IAsyncEnumerable<Customer>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<Customer>(data.GetEnumerator()));

        mockSet.As<IQueryable<Customer>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<Customer>(data.Provider));

        mockSet.As<IQueryable<Customer>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<Customer>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<Customer>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        return mockSet;
    }
}
```

### 5. Testing Validators

```csharp
namespace Framework.Application.Tests.Validators;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator;

    public CreateCustomerCommandValidatorTests()
    {
        _validator = new CreateCustomerCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "",
            Email = "john@example.com",
            Address = new AddressDto { /* ... */ }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalidemail")]
    [InlineData("@example.com")]
    public void Validate_InvalidEmail_ReturnsInvalid(string email)
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = email,
            Address = new AddressDto { /* ... */ }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}
```

## Integration Testing

### 1. Test Database Setup

Use in-memory database or test database for integration tests.

```csharp
namespace Framework.Api.Tests.Integration;

public class TestDatabaseFactory
{
    public static ApplicationDbContext CreateInMemoryDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(
            options,
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDateTime>());

        context.Database.EnsureCreated();

        return context;
    }

    public static void SeedDatabase(ApplicationDbContext context)
    {
        var customers = new List<Customer>
        {
            new Customer("John Doe", "john@example.com",
                new Address("123 Main St", "New York", "NY", "10001", "USA")),
            new Customer("Jane Smith", "jane@example.com",
                new Address("456 Oak Ave", "Los Angeles", "CA", "90001", "USA"))
        };

        context.Customers.AddRange(customers);
        context.SaveChanges();
    }
}
```

### 2. API Integration Tests

```csharp
namespace Framework.Api.Tests.Integration;

public class CustomersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CustomersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory database
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCustomer_ExistingId_ReturnsOk()
    {
        // Arrange
        var customerId = await CreateTestCustomer();

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var customer = JsonSerializer.Deserialize<CustomerDto>(content);
        Assert.NotNull(customer);
    }

    [Fact]
    public async Task CreateCustomer_ValidData_ReturnsCreated()
    {
        // Arrange
        var command = new CreateCustomerCommand
        {
            Name = "Test Customer",
            Email = "test@example.com",
            Address = new AddressDto
            {
                Street = "123 Test St",
                City = "Test City",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(command),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/customers", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);
    }

    private async Task<Guid> CreateTestCustomer()
    {
        var command = new CreateCustomerCommand
        {
            Name = "John Doe",
            Email = "john@example.com",
            Address = new AddressDto
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(command),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/customers", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<Guid>(responseContent);
    }
}
```

## Test Utilities

### Mock DbSet Helper

```csharp
namespace Framework.Application.Tests.Helpers;

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}
```

## Best Practices

### 1. Follow AAA Pattern

Arrange, Act, Assert:

```csharp
[Fact]
public void TestMethod()
{
    // Arrange - Setup
    var sut = new SystemUnderTest();

    // Act - Execute
    var result = sut.DoSomething();

    // Assert - Verify
    Assert.NotNull(result);
}
```

### 2. Test One Thing

Each test should verify one specific behavior.

### 3. Use Descriptive Names

```csharp
// Good
[Fact]
public void CreateOrder_WithNegativeQuantity_ThrowsDomainException()

// Bad
[Fact]
public void Test1()
```

### 4. Use Theory for Multiple Cases

```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(-100)]
public void Constructor_NegativeOrZeroQuantity_ThrowsException(int quantity)
{
    Assert.Throws<ArgumentException>(() => new OrderItem(..., quantity));
}
```

### 5. Mock Only External Dependencies

Don't mock domain entities or value objects.

### 6. Test Happy and Unhappy Paths

Test both successful scenarios and error cases.

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests in specific project
dotnet test tests/Framework.Domain.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~CreateCustomerCommandHandlerTests"
```

## Summary

The Framework testing approach provides:
- Unit tests for domain logic
- Unit tests for application handlers
- Validator tests
- Integration tests for APIs
- Test utilities and helpers
- Comprehensive coverage

This ensures code quality, reliability, and maintainability throughout the application.
