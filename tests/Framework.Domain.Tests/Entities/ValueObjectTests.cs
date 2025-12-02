using Framework.Domain.Common.Entities;

namespace Framework.Domain.Tests.Entities;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }

    public Address(string street, string city, string postalCode)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return PostalCode;
    }
}

public class Money : SingleValueObject<decimal>
{
    public Money(decimal value) : base(value) { }
}

public class ValueObjectTests
{
    [Fact]
    public void ValueObject_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.Equal(address1, address2);
        Assert.True(address1 == address2);
    }

    [Fact]
    public void ValueObject_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("456 Oak Ave", "New York", "10001");

        // Act & Assert
        Assert.NotEqual(address1, address2);
        Assert.True(address1 != address2);
    }

    [Fact]
    public void ValueObject_GetHashCode_ShouldBeSameForEqualObjects()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "10001");
        var address2 = new Address("123 Main St", "New York", "10001");

        // Act & Assert
        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
    }

    [Fact]
    public void SingleValueObject_ShouldWrapValue()
    {
        // Arrange
        var money = new Money(100.50m);

        // Act & Assert
        Assert.Equal(100.50m, money.Value);
    }

    [Fact]
    public void SingleValueObject_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100.50m);
        var money2 = new Money(100.50m);

        // Act & Assert
        Assert.Equal(money1, money2);
    }

    [Fact]
    public void SingleValueObject_ImplicitConversion_ShouldWork()
    {
        // Arrange
        var money = new Money(100.50m);

        // Act
        decimal value = money;

        // Assert
        Assert.Equal(100.50m, value);
    }
}
