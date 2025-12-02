using Framework.Application.MultiTenancy;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.MultiTenancy;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Framework.Infrastructure.Tests.MultiTenancy;

public class TenantManagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly TenantManagementService _service;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<TenantManagementService> _logger;

    public TenantManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.UserId.Returns("test-user");
        _dateTime = Substitute.For<IDateTime>();
        _dateTime.Now.Returns(DateTimeOffset.UtcNow);
        _logger = Substitute.For<ILogger<TenantManagementService>>();

        _service = new TenantManagementService(
            _context,
            _currentUser,
            _dateTime,
            _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetTenantsAsync Tests

    [Fact]
    public async Task GetTenantsAsync_ReturnsPagedList()
    {
        // Arrange
        await SeedTestTenants();
        var request = new TenantListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetTenantsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetTenantsAsync_WithSearchTerm_FiltersTenants()
    {
        // Arrange
        await SeedTestTenants();
        var request = new TenantListRequest { SearchTerm = "acme", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetTenantsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task GetTenantsAsync_WithActiveFilter_FiltersTenants()
    {
        // Arrange
        await SeedTestTenants();
        var request = new TenantListRequest { IsActive = true, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetTenantsAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    #endregion

    #region GetTenantByIdAsync Tests

    [Fact]
    public async Task GetTenantByIdAsync_ExistingTenant_ReturnsTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");

        // Act
        var result = await _service.GetTenantByIdAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant.Name, result.Value!.Name);
    }

    [Fact]
    public async Task GetTenantByIdAsync_NonExistingTenant_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetTenantByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region GetTenantByIdentifierAsync Tests

    [Fact]
    public async Task GetTenantByIdentifierAsync_ExistingTenant_ReturnsTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");

        // Act
        var result = await _service.GetTenantByIdentifierAsync("test-tenant");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetTenantByIdentifierAsync_NonExistingTenant_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetTenantByIdentifierAsync("nonexistent");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region CreateTenantAsync Tests

    [Fact]
    public async Task CreateTenantAsync_ValidRequest_CreatesTenant()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            Name = "New Tenant",
            Identifier = "new-tenant",
            AdminEmail = "admin@newtenant.com",
            IsActive = true,
            Features = new List<SetFeatureRequest>()
        };

        // Act
        var result = await _service.CreateTenantAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var tenant = await _context.Set<Tenant>().FindAsync(result.Value);
        Assert.NotNull(tenant);
        Assert.Equal("New Tenant", tenant.Name);
    }

    [Fact]
    public async Task CreateTenantAsync_DuplicateIdentifier_ReturnsConflict()
    {
        // Arrange
        await CreateTestTenant("Existing Tenant", "existing-tenant");
        var request = new CreateTenantRequest
        {
            Name = "Another Tenant",
            Identifier = "existing-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true,
            Features = new List<SetFeatureRequest>()
        };

        // Act
        var result = await _service.CreateTenantAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    [Fact]
    public async Task CreateTenantAsync_WithFeatures_CreatesFeatures()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            Name = "New Tenant",
            Identifier = "new-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true,
            Features = new List<SetFeatureRequest>
            {
                new SetFeatureRequest { FeatureName = "Feature1", IsEnabled = true },
                new SetFeatureRequest { FeatureName = "Feature2", IsEnabled = false }
            }
        };

        // Act
        var result = await _service.CreateTenantAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var features = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == result.Value)
            .ToListAsync();
        Assert.Equal(2, features.Count);
    }

    #endregion

    #region UpdateTenantAsync Tests

    [Fact]
    public async Task UpdateTenantAsync_ExistingTenant_UpdatesTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        var request = new UpdateTenantRequest
        {
            Name = "Updated Tenant",
            AdminEmail = "updated@test.com"
        };

        // Act
        var result = await _service.UpdateTenantAsync(tenant.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedTenant = await _context.Set<Tenant>().FindAsync(tenant.Id);
        Assert.Equal("Updated Tenant", updatedTenant!.Name);
    }

    [Fact]
    public async Task UpdateTenantAsync_NonExistingTenant_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateTenantRequest { Name = "Updated" };

        // Act
        var result = await _service.UpdateTenantAsync(Guid.NewGuid(), request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region DeleteTenantAsync Tests

    [Fact]
    public async Task DeleteTenantAsync_ExistingTenant_SoftDeletesTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");

        // Act
        var result = await _service.DeleteTenantAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedTenant = await _context.Set<Tenant>().FindAsync(tenant.Id);
        Assert.True(deletedTenant!.IsDeleted);
    }

    #endregion

    #region ActivateTenantAsync / DeactivateTenantAsync Tests

    [Fact]
    public async Task ActivateTenantAsync_ExistingTenant_ActivatesTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant", isActive: false);

        // Act
        var result = await _service.ActivateTenantAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var activatedTenant = await _context.Set<Tenant>().FindAsync(tenant.Id);
        Assert.True(activatedTenant!.IsActive);
    }

    [Fact]
    public async Task DeactivateTenantAsync_ExistingTenant_DeactivatesTenant()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant", isActive: true);

        // Act
        var result = await _service.DeactivateTenantAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deactivatedTenant = await _context.Set<Tenant>().FindAsync(tenant.Id);
        Assert.False(deactivatedTenant!.IsActive);
    }

    #endregion

    #region Feature Management Tests

    [Fact]
    public async Task GetTenantFeaturesAsync_TenantWithFeatures_ReturnsFeatures()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        await CreateTestFeature(tenant.Id, "Feature1", isEnabled: true);
        await CreateTestFeature(tenant.Id, "Feature2", isEnabled: false);

        // Act
        var result = await _service.GetTenantFeaturesAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count());
    }

    [Fact]
    public async Task EnableFeatureAsync_ExistingFeature_EnablesFeature()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        await CreateTestFeature(tenant.Id, "TestFeature", isEnabled: false);

        // Act
        var result = await _service.EnableFeatureAsync(tenant.Id, "TestFeature", null);

        // Assert
        Assert.True(result.IsSuccess);
        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenant.Id && f.FeatureName == "TestFeature");
        Assert.True(feature!.IsEnabled);
    }

    [Fact]
    public async Task DisableFeatureAsync_ExistingFeature_DisablesFeature()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        await CreateTestFeature(tenant.Id, "TestFeature", isEnabled: true);

        // Act
        var result = await _service.DisableFeatureAsync(tenant.Id, "TestFeature");

        // Assert
        Assert.True(result.IsSuccess);
        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenant.Id && f.FeatureName == "TestFeature");
        Assert.False(feature!.IsEnabled);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_EnabledFeature_ReturnsTrue()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        await CreateTestFeature(tenant.Id, "TestFeature", isEnabled: true);

        // Act
        var result = await _service.IsFeatureEnabledAsync(tenant.Id, "TestFeature");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_DisabledFeature_ReturnsFalse()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        await CreateTestFeature(tenant.Id, "TestFeature", isEnabled: false);

        // Act
        var result = await _service.IsFeatureEnabledAsync(tenant.Id, "TestFeature");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task SetFeaturesAsync_ValidFeatures_SetsFeatures()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");
        var features = new List<SetFeatureRequest>
        {
            new() { FeatureName = "Feature1", IsEnabled = true },
            new() { FeatureName = "Feature2", IsEnabled = false, Configuration = "{\"key\":\"value\"}" }
        };

        // Act
        var result = await _service.SetFeaturesAsync(tenant.Id, features);

        // Assert
        Assert.True(result.IsSuccess);
        var tenantFeatures = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == tenant.Id)
            .ToListAsync();
        Assert.Equal(2, tenantFeatures.Count);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetTenantStatisticsAsync_ExistingTenant_ReturnsStatistics()
    {
        // Arrange
        var tenant = await CreateTestTenant("Test Tenant", "test-tenant");

        // Act
        var result = await _service.GetTenantStatisticsAsync(tenant.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetDashboardStatisticsAsync_ReturnsStatistics()
    {
        // Arrange
        await SeedTestTenants();

        // Act
        var result = await _service.GetDashboardStatisticsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    #endregion

    #region Helper Methods

    private async Task<Tenant> CreateTestTenant(string name, string identifier, bool isActive = true)
    {
        var tenant = new Tenant(name, identifier, $"admin@{identifier}.com");
        if (!isActive)
            tenant.Deactivate();
        tenant.SetCreated(_dateTime.Now, "test");

        _context.Set<Tenant>().Add(tenant);
        await _context.SaveChangesAsync();

        return tenant;
    }

    private async Task<TenantFeature> CreateTestFeature(Guid tenantId, string featureName, bool isEnabled)
    {
        var feature = new TenantFeature(Guid.NewGuid(), tenantId, featureName, isEnabled);
        feature.SetCreated(_dateTime.Now, "test");

        _context.Set<TenantFeature>().Add(feature);
        await _context.SaveChangesAsync();

        return feature;
    }

    private async Task SeedTestTenants()
    {
        await CreateTestTenant("Acme Corp", "acme", isActive: true);
        await CreateTestTenant("Beta Inc", "beta", isActive: true);
        await CreateTestTenant("Inactive LLC", "inactive", isActive: false);
    }

    #endregion
}
