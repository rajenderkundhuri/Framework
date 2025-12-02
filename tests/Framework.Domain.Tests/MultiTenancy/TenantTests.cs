using Framework.Domain.MultiTenancy;

namespace Framework.Domain.Tests.MultiTenancy;

public class TenantTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateTenant()
    {
        // Act
        var tenant = new Tenant("Test Tenant", "test-tenant", "admin@test.com");

        // Assert
        Assert.Equal("Test Tenant", tenant.Name);
        Assert.Equal("TEST TENANT", tenant.NormalizedName);
        Assert.Equal("test-tenant", tenant.Identifier);
        Assert.Equal("admin@test.com", tenant.AdminEmail);
        Assert.True(tenant.IsActive);
        Assert.False(tenant.IsDeleted);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Tenant("", "test-tenant"));
        Assert.Throws<ArgumentException>(() => new Tenant("  ", "test-tenant"));
    }

    [Fact]
    public void Constructor_WithEmptyIdentifier_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Tenant("Test Tenant", ""));
        Assert.Throws<ArgumentException>(() => new Tenant("Test Tenant", "  "));
    }

    [Fact]
    public void Constructor_ShouldNormalizeIdentifierToLowercase()
    {
        // Act
        var tenant = new Tenant("Test", "TEST-TENANT");

        // Assert
        Assert.Equal("test-tenant", tenant.Identifier);
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateNameAndNormalizedName()
    {
        // Arrange
        var tenant = new Tenant("Original Name", "test");

        // Act
        tenant.UpdateName("New Name");

        // Assert
        Assert.Equal("New Name", tenant.Name);
        Assert.Equal("NEW NAME", tenant.NormalizedName);
    }

    [Fact]
    public void UpdateName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var tenant = new Tenant("Original", "test");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tenant.UpdateName(""));
    }

    [Fact]
    public void SetConnectionString_ShouldUpdateConnectionString()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");

        // Act
        tenant.SetConnectionString("Server=localhost;Database=TestDb");

        // Assert
        Assert.Equal("Server=localhost;Database=TestDb", tenant.ConnectionString);
    }

    [Fact]
    public void SetConnectionString_WithNull_ShouldClearConnectionString()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.SetConnectionString("Server=localhost");

        // Act
        tenant.SetConnectionString(null);

        // Assert
        Assert.Null(tenant.ConnectionString);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.Deactivate();

        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void SetValidity_ShouldUpdateValidUpto()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        var validUntil = DateTimeOffset.UtcNow.AddMonths(1);

        // Act
        tenant.SetValidity(validUntil);

        // Assert
        Assert.Equal(validUntil, tenant.ValidUpto);
    }

    [Fact]
    public void IsValid_WithActiveTenant_ShouldReturnTrue()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");

        // Act & Assert
        Assert.True(tenant.IsValid());
    }

    [Fact]
    public void IsValid_WithInactiveTenant_ShouldReturnFalse()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.Deactivate();

        // Act & Assert
        Assert.False(tenant.IsValid());
    }

    [Fact]
    public void IsValid_WithDeletedTenant_ShouldReturnFalse()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.SoftDelete(DateTimeOffset.UtcNow, "user");

        // Act & Assert
        Assert.False(tenant.IsValid());
    }

    [Fact]
    public void IsValid_WithExpiredTenant_ShouldReturnFalse()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.SetValidity(DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert
        Assert.False(tenant.IsValid());
    }

    [Fact]
    public void IsValid_WithFutureValidity_ShouldReturnTrue()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.SetValidity(DateTimeOffset.UtcNow.AddDays(30));

        // Act & Assert
        Assert.True(tenant.IsValid());
    }

    [Fact]
    public void IsValid_WithNullValidity_ShouldReturnTrue()
    {
        // Arrange
        var tenant = new Tenant("Test", "test");
        tenant.SetValidity(null);

        // Act & Assert
        Assert.True(tenant.IsValid());
    }
}
