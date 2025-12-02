# Domain Layer Guide

The Domain layer is the heart of the Framework, containing business logic, domain models, and business rules. It has no dependencies on other layers or external frameworks, ensuring business logic remains pure and testable.

## Overview

The Domain layer represents the core business domain:
- **Location**: `src/Framework.Domain/`
- **Dependencies**: None (pure C#)
- **Purpose**: Encapsulate business logic and domain models
- **Principles**: Domain-Driven Design (DDD)

## Key Components

### 1. Entities

Entities are objects with identity that persists over time. Two entities with the same property values but different IDs are considered different objects.

#### Base Entity

```csharp
namespace Framework.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return (GetType().ToString() + Id).GetHashCode();
    }

    public static bool operator ==(BaseEntity? a, BaseEntity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(BaseEntity? a, BaseEntity? b)
    {
        return !(a == b);
    }
}
```

#### Auditable Entity

Entities that track creation and modification:

```csharp
namespace Framework.Domain.Entities;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public void SetCreatedBy(string userId)
    {
        CreatedBy = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetUpdatedBy(string userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### Example Entity

```csharp
namespace Framework.Domain.Entities;

public class Customer : AuditableEntity, IAggregateRoot
{
    private readonly List<Order> _orders = new();

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // Private constructor for EF Core
    private Customer() { }

    public Customer(string name, string email, Address address)
    {
        SetName(name);
        SetEmail(email);
        Address = address ?? throw new ArgumentNullException(nameof(address));
        IsActive = true;
        IsDeleted = false;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name cannot be empty");

        if (name.Length > 100)
            throw new DomainException("Customer name cannot exceed 100 characters");

        Name = name;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        if (!IsValidEmail(email))
            throw new DomainException("Invalid email format");

        Email = email;
    }

    public void UpdateAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public void Activate()
    {
        if (IsDeleted)
            throw new DomainException("Cannot activate a deleted customer");

        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Delete()
    {
        IsDeleted = true;
        IsActive = false;
    }

    public void AddOrder(Order order)
    {
        if (!IsActive)
            throw new DomainException("Cannot add order to inactive customer");

        if (IsDeleted)
            throw new DomainException("Cannot add order to deleted customer");

        _orders.Add(order);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### 2. Value Objects

Value objects are immutable and defined by their attributes rather than identity. Two value objects with the same attribute values are considered equal.

#### Base Value Object

```csharp
namespace Framework.Domain.ValueObjects;

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
```

#### Example Value Objects

```csharp
namespace Framework.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }
    public string Country { get; private set; }

    private Address() { } // For EF Core

    public Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        State = state ?? throw new ArgumentNullException(nameof(state));
        ZipCode = zipCode ?? throw new ArgumentNullException(nameof(zipCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
        yield return Country;
    }

    public override string ToString()
    {
        return $"{Street}, {City}, {State} {ZipCode}, {Country}";
    }
}

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money() { } // For EF Core

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");

        return new Money(Amount - other.Amount, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public override string ToString() => $"{Amount:N2} {Currency}";
}

public class Email : ValueObject
{
    public string Value { get; private set; }

    private Email() { } // For EF Core

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);

    public override string ToString() => Value;
}
```

### 3. Aggregate Roots

Aggregates are clusters of domain objects that are treated as a single unit. The aggregate root is the entry point for all operations on the aggregate.

#### IAggregateRoot Interface

```csharp
namespace Framework.Domain.Interfaces;

public interface IAggregateRoot
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

#### Aggregate Root Implementation

```csharp
namespace Framework.Domain.Entities;

public class Order : AuditableEntity, IAggregateRoot
{
    private readonly List<OrderItem> _items = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; } = null!;
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order() { } // For EF Core

    public Order(Guid customerId, string orderNumber)
    {
        CustomerId = customerId;
        OrderNumber = orderNumber ?? throw new ArgumentNullException(nameof(orderNumber));
        Status = OrderStatus.Pending;
        Total = new Money(0, "USD");

        AddDomainEvent(new OrderCreatedEvent(Id, CustomerId));
    }

    public void AddItem(Guid productId, string productName, Money price, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Cannot add items to a processed order");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var item = new OrderItem(productId, productName, price, quantity);
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Cannot remove items from a processed order");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new DomainException("Item not found in order");

        _items.Remove(item);
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Only pending orders can be confirmed");

        if (!_items.Any())
            throw new DomainException("Cannot confirm an order without items");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
    }

    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException("Only confirmed orders can be shipped");

        Status = OrderStatus.Shipped;
        ShippedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderShippedEvent(Id));
    }

    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainException("Only shipped orders can be delivered");

        Status = OrderStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        AddDomainEvent(new OrderDeliveredEvent(Id));
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
            throw new DomainException("Cannot cancel delivered orders");

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id));
    }

    private void RecalculateTotal()
    {
        var amount = _items.Sum(i => i.Subtotal.Amount);
        Total = new Money(amount, "USD");
    }

    private void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money Subtotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    private OrderItem() { } // For EF Core

    public OrderItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        SetQuantity(quantity);
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Quantity += amount;
    }

    public void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (Quantity - amount < 0)
            throw new DomainException("Quantity cannot be negative");

        Quantity -= amount;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        Quantity = quantity;
    }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

### 4. Domain Events

Domain events represent significant occurrences in the domain. They enable loose coupling between aggregates and trigger side effects.

#### Base Domain Event

```csharp
namespace Framework.Domain.Events;

public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

#### Example Domain Events

```csharp
namespace Framework.Domain.Events;

public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public Guid CustomerId { get; }

    public OrderCreatedEvent(Guid orderId, Guid customerId)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }
}

public class OrderConfirmedEvent : DomainEvent
{
    public Guid OrderId { get; }

    public OrderConfirmedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}

public class OrderShippedEvent : DomainEvent
{
    public Guid OrderId { get; }

    public OrderShippedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}

public class CustomerCreatedEvent : DomainEvent
{
    public Guid CustomerId { get; }
    public string CustomerName { get; }
    public string Email { get; }

    public CustomerCreatedEvent(Guid customerId, string customerName, string email)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        Email = email;
    }
}
```

### 5. Specifications

Specifications encapsulate query logic in reusable, composable objects.

#### Base Specification

```csharp
namespace Framework.Domain.Specifications;

public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

public class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpression, parameter),
            Expression.Invoke(rightExpression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.Not(Expression.Invoke(expression, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
```

#### Example Specifications

```csharp
namespace Framework.Domain.Specifications;

public class ActiveCustomersSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.IsActive && !customer.IsDeleted;
    }
}

public class CustomerByEmailSpecification : Specification<Customer>
{
    private readonly string _email;

    public CustomerByEmailSpecification(string email)
    {
        _email = email;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Email == _email;
    }
}

public class PendingOrdersSpecification : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return order => order.Status == OrderStatus.Pending;
    }
}

// Usage
var activeCustomerSpec = new ActiveCustomersSpecification();
var emailSpec = new CustomerByEmailSpecification("john@example.com");
var combinedSpec = activeCustomerSpec.And(emailSpec);
```

### 6. Domain Exceptions

Custom exceptions for domain-specific errors:

```csharp
namespace Framework.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found")
    {
    }
}

public class BusinessRuleValidationException : DomainException
{
    public BusinessRuleValidationException(string message)
        : base(message)
    {
    }
}
```

### 7. Domain Services

When business logic doesn't naturally fit in an entity:

```csharp
namespace Framework.Domain.Services;

public interface IOrderNumberGenerator
{
    string GenerateOrderNumber();
}

public interface IPricingService
{
    Money CalculateDiscount(Order order, Customer customer);
    Money CalculateTax(Order order);
}
```

## Best Practices

### 1. Encapsulation

Keep entity state private and expose behavior through methods:

```csharp
// Bad
public class Customer
{
    public string Email { get; set; }
}

// Good
public class Customer
{
    public string Email { get; private set; }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        Email = email;
    }
}
```

### 2. Always Valid Entities

Ensure entities are always in a valid state:

```csharp
public class Product
{
    public string Name { get; private set; }
    public Money Price { get; private set; }

    public Product(string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        if (price.Amount < 0)
            throw new DomainException("Price cannot be negative");

        Name = name;
        Price = price;
    }
}
```

### 3. Use Value Objects

Replace primitives with value objects for domain concepts:

```csharp
// Bad
public string Email { get; set; }

// Good
public Email Email { get; private set; }
```

### 4. Aggregate Consistency

Modify aggregates through the root entity only:

```csharp
// Bad
order.Items.Add(new OrderItem(...));

// Good
order.AddItem(productId, price, quantity);
```

### 5. Raise Domain Events

Signal important domain occurrences:

```csharp
public void Confirm()
{
    Status = OrderStatus.Confirmed;
    AddDomainEvent(new OrderConfirmedEvent(Id));
}
```

## Summary

The Domain layer provides:
- Pure business logic without infrastructure dependencies
- Rich domain models with encapsulated behavior
- Value objects for domain concepts
- Domain events for decoupled communication
- Specifications for reusable query logic
- Aggregate roots for transactional consistency

This design ensures that business rules are centralized, testable, and independent of technical concerns.
