using Framework.Application.MultiTenancy;
using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Tests.MultiTenancy;

public class TenantContextTests
{
    private readonly IOptions<MultiTenancySettings> _enabledSettings;
    private readonly IOptions<MultiTenancySettings> _disabledSettings;

    public TenantContextTests()
    {
        _enabledSettings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        _disabledSettings = Options.Create(new MultiTenancySettings { IsEnabled = false });
    }

    [Fact]
    public void NewContext_ShouldHaveNullTenant()
    {
        // Arrange & Act
        var context = new TenantContext(_enabledSettings);

        // Assert
        Assert.Null(context.TenantId);
        Assert.Null(context.CurrentTenant);
        Assert.True(context.IsHost);
    }

    [Fact]
    public void SetTenant_ShouldSetTenantAndTenantId()
    {
        // Arrange
        var context = new TenantContext(_enabledSettings);
        var tenant = new Tenant("Test", "test");

        // Act
        context.SetTenant(tenant);

        // Assert
        Assert.Equal(tenant.Id, context.TenantId);
        Assert.Equal(tenant, context.CurrentTenant);
        Assert.False(context.IsHost);
    }

    [Fact]
    public void SetTenantId_ShouldSetOnlyTenantId()
    {
        // Arrange
        var context = new TenantContext(_enabledSettings);
        var tenantId = Guid.NewGuid();

        // Act
        context.SetTenantId(tenantId);

        // Assert
        Assert.Equal(tenantId, context.TenantId);
        Assert.Null(context.CurrentTenant);
        Assert.False(context.IsHost);
    }

    [Fact]
    public void Clear_ShouldResetContext()
    {
        // Arrange
        var context = new TenantContext(_enabledSettings);
        context.SetTenant(new Tenant("Test", "test"));

        // Act
        context.Clear();

        // Assert
        Assert.Null(context.TenantId);
        Assert.Null(context.CurrentTenant);
        Assert.True(context.IsHost);
    }

    [Fact]
    public void IsEnabled_ShouldReflectSettings()
    {
        // Arrange & Act
        var enabledContext = new TenantContext(_enabledSettings);
        var disabledContext = new TenantContext(_disabledSettings);

        // Assert
        Assert.True(enabledContext.IsEnabled);
        Assert.False(disabledContext.IsEnabled);
    }

    [Fact]
    public void SetTenant_WithNull_ShouldClearContext()
    {
        // Arrange
        var context = new TenantContext(_enabledSettings);
        context.SetTenant(new Tenant("Test", "test"));

        // Act
        context.SetTenant(null);

        // Assert
        Assert.Null(context.TenantId);
        Assert.Null(context.CurrentTenant);
    }

    [Fact]
    public void SetTenantId_WithNull_ShouldSetToHostContext()
    {
        // Arrange
        var context = new TenantContext(_enabledSettings);
        context.SetTenantId(Guid.NewGuid());

        // Act
        context.SetTenantId(null);

        // Assert
        Assert.Null(context.TenantId);
        Assert.True(context.IsHost);
    }
}

public class TenantScopeTests
{
    [Fact]
    public void TenantScope_ShouldTemporarilyChangeTenant()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        var context = new TenantContext(settings);
        var originalTenant = new Tenant("Original", "original");
        var scopedTenant = new Tenant("Scoped", "scoped");
        context.SetTenant(originalTenant);

        // Act
        using (var scope = new TenantScope(context, scopedTenant))
        {
            // Assert - Within scope
            Assert.Equal(scopedTenant.Id, context.TenantId);
            Assert.Equal(scopedTenant, context.CurrentTenant);
        }

        // Assert - After scope
        Assert.Equal(originalTenant.Id, context.TenantId);
        Assert.Equal(originalTenant, context.CurrentTenant);
    }

    [Fact]
    public void TenantScope_WithTenantId_ShouldTemporarilyChangeTenantId()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        var context = new TenantContext(settings);
        var originalId = Guid.NewGuid();
        var scopedId = Guid.NewGuid();
        context.SetTenantId(originalId);

        // Act
        using (var scope = new TenantScope(context, scopedId))
        {
            // Assert - Within scope
            Assert.Equal(scopedId, context.TenantId);
        }

        // Assert - After scope
        Assert.Equal(originalId, context.TenantId);
    }

    [Fact]
    public void TenantScope_FromHostToTenant_ShouldRestoreHostContext()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        var context = new TenantContext(settings);
        var tenant = new Tenant("Scoped", "scoped");

        // Assert - Initially host
        Assert.True(context.IsHost);

        // Act
        using (var scope = new TenantScope(context, tenant))
        {
            Assert.False(context.IsHost);
            Assert.Equal(tenant.Id, context.TenantId);
        }

        // Assert - Back to host
        Assert.True(context.IsHost);
        Assert.Null(context.TenantId);
    }

    [Fact]
    public void TenantScope_NestedScopes_ShouldRestoreCorrectly()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        var context = new TenantContext(settings);
        var tenant1 = new Tenant("Tenant 1", "tenant1");
        var tenant2 = new Tenant("Tenant 2", "tenant2");
        context.SetTenant(tenant1);

        // Act & Assert
        using (var scope1 = new TenantScope(context, tenant2))
        {
            Assert.Equal(tenant2.Id, context.TenantId);

            var tenant3 = new Tenant("Tenant 3", "tenant3");
            using (var scope2 = new TenantScope(context, tenant3))
            {
                Assert.Equal(tenant3.Id, context.TenantId);
            }

            // Back to tenant2
            Assert.Equal(tenant2.Id, context.TenantId);
        }

        // Back to tenant1
        Assert.Equal(tenant1.Id, context.TenantId);
    }

    [Fact]
    public void TenantScope_MultipleDispose_ShouldBeIdempotent()
    {
        // Arrange
        var settings = Options.Create(new MultiTenancySettings { IsEnabled = true });
        var context = new TenantContext(settings);
        var originalId = Guid.NewGuid();
        var scopedId = Guid.NewGuid();
        context.SetTenantId(originalId);

        // Act
        var scope = new TenantScope(context, scopedId);
        scope.Dispose();
        scope.Dispose(); // Second dispose should be no-op

        // Assert
        Assert.Equal(originalId, context.TenantId);
    }
}
