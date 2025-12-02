using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.MultiTenancy;

namespace Framework.Infrastructure.Tests.MultiTenancy;

public class InMemoryTenantStoreTests
{
    [Fact]
    public async Task GetByIdentifier_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new Tenant("Test Tenant", "test-tenant");
        store.AddTenant(tenant);

        // Act
        var result = await store.GetByIdentifierAsync("test-tenant");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Tenant", result.Name);
        Assert.Equal("test-tenant", result.Identifier);
    }

    [Fact]
    public async Task GetByIdentifier_WithNonExistingTenant_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.GetByIdentifierAsync("non-existing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdentifier_IsCaseInsensitive()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new Tenant("Test", "test-tenant");
        store.AddTenant(tenant);

        // Act
        var result = await store.GetByIdentifierAsync("TEST-TENANT");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIdentifier_WithDeletedTenant_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new Tenant("Test", "test-tenant");
        tenant.SoftDelete(DateTimeOffset.UtcNow, "user");
        store.AddTenant(tenant);

        // Act
        var result = await store.GetByIdentifierAsync("test-tenant");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new Tenant("Test", "test-tenant");
        store.AddTenant(tenant);

        // Act
        var result = await store.GetByIdAsync(tenant.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenant.Id, result.Id);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_WithDeletedTenant_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tenant = new Tenant("Test", "test-tenant");
        tenant.SoftDelete(DateTimeOffset.UtcNow, "user");
        store.AddTenant(tenant);

        // Act
        var result = await store.GetByIdAsync(tenant.Id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllNonDeletedTenants()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddTenant(new Tenant("Tenant A", "tenant-a"));
        store.AddTenant(new Tenant("Tenant B", "tenant-b"));
        var deletedTenant = new Tenant("Deleted", "deleted");
        deletedTenant.SoftDelete(DateTimeOffset.UtcNow, "user");
        store.AddTenant(deletedTenant);

        // Act
        var result = await store.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_ShouldReturnTenantsOrderedByName()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddTenant(new Tenant("Zebra", "zebra"));
        store.AddTenant(new Tenant("Apple", "apple"));
        store.AddTenant(new Tenant("Middle", "middle"));

        // Act
        var result = await store.GetAllAsync();

        // Assert
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Middle", result[1].Name);
        Assert.Equal("Zebra", result[2].Name);
    }

    [Fact]
    public async Task GetAll_WithEmptyStore_ShouldReturnEmptyList()
    {
        // Arrange
        var store = new InMemoryTenantStore();

        // Act
        var result = await store.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Clear_ShouldRemoveAllTenants()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        store.AddTenant(new Tenant("Test 1", "test-1"));
        store.AddTenant(new Tenant("Test 2", "test-2"));

        // Act
        store.Clear();
        var result = store.GetAllAsync().GetAwaiter().GetResult();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Store_ShouldBeThreadSafe()
    {
        // Arrange
        var store = new InMemoryTenantStore();
        var tasks = new List<Task>();

        // Act - Add multiple tenants concurrently
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() => store.AddTenant(new Tenant($"Tenant {index}", $"tenant-{index}"))));
        }
        await Task.WhenAll(tasks);

        // Assert
        var result = await store.GetAllAsync();
        Assert.Equal(100, result.Count);
    }
}
