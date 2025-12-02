using Framework.Domain.MultiTenancy;

namespace Framework.Domain.Tests.MultiTenancy;

public class TestMultiTenantEntity : MultiTenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public class TestFullMultiTenantEntity : FullMultiTenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public class MultiTenantEntityTests
{
    [Fact]
    public void MultiTenantEntity_NewEntity_ShouldHaveNullTenantId()
    {
        // Act
        var entity = new TestMultiTenantEntity();

        // Assert
        Assert.Null(entity.TenantId);
    }

    [Fact]
    public void SetTenantId_WithValidId_ShouldSetTenantId()
    {
        // Arrange
        var entity = new TestMultiTenantEntity();
        var tenantId = Guid.NewGuid();

        // Act
        entity.SetTenantId(tenantId);

        // Assert
        Assert.Equal(tenantId, entity.TenantId);
    }

    [Fact]
    public void SetTenantId_WithNull_ShouldClearTenantId()
    {
        // Arrange
        var entity = new TestMultiTenantEntity();
        entity.SetTenantId(Guid.NewGuid());

        // Act
        entity.SetTenantId(null);

        // Assert
        Assert.Null(entity.TenantId);
    }

    [Fact]
    public void MultiTenantEntity_ShouldInheritAuditableEntity()
    {
        // Arrange
        var entity = new TestMultiTenantEntity();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        entity.SetCreated(timestamp, "user1");

        // Assert
        Assert.Equal(timestamp, entity.CreatedAt);
        Assert.Equal("user1", entity.CreatedBy);
    }

    [Fact]
    public void FullMultiTenantEntity_ShouldSupportSoftDelete()
    {
        // Arrange
        var entity = new TestFullMultiTenantEntity();
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        entity.SoftDelete(timestamp, "user1");

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.Equal(timestamp, entity.DeletedAt);
        Assert.Equal("user1", entity.DeletedBy);
    }

    [Fact]
    public void FullMultiTenantEntity_Restore_ShouldClearDeleteFlags()
    {
        // Arrange
        var entity = new TestFullMultiTenantEntity();
        entity.SoftDelete(DateTimeOffset.UtcNow, "user1");

        // Act
        entity.Restore();

        // Assert
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }

    [Fact]
    public void MultiTenantEntity_ImplementsIMultiTenant()
    {
        // Arrange
        var entity = new TestMultiTenantEntity();
        var tenantId = Guid.NewGuid();
        entity.SetTenantId(tenantId);

        // Act
        IMultiTenant multiTenantInterface = entity;

        // Assert
        Assert.Equal(tenantId, multiTenantInterface.TenantId);
    }
}
