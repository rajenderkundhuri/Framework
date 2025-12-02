using System.Security.Claims;
using Framework.Api.Controllers;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Application.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Framework.Api.Tests.Controllers;

public class TenantsControllerTests
{
    private readonly ITenantManagementService _tenantManagementService;
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _tenantManagementService = Substitute.For<ITenantManagementService>();
        _controller = new TenantsController(_tenantManagementService);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetTenants Tests

    [Fact]
    public async Task GetTenants_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var request = new TenantListRequest { PageNumber = 1, PageSize = 10 };
        var tenants = new PagedList<TenantListResponse>(new List<TenantListResponse>(), 0, 1, 10);
        _tenantManagementService.GetTenantsAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<PagedList<TenantListResponse>>.Success(tenants));

        // Act
        var result = await _controller.GetTenants(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTenants_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new TenantListRequest();

        // Act
        var result = await _controller.GetTenants(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetTenant Tests

    [Fact]
    public async Task GetTenant_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var tenantId = Guid.NewGuid();
        var tenant = new TenantDetailResponse { Id = tenantId, Name = "Test Tenant" };
        _tenantManagementService.GetTenantByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result<TenantDetailResponse>.Success(tenant));

        // Act
        var result = await _controller.GetTenant(tenantId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<TenantDetailResponse>();
        response.Id.ShouldBe(tenantId);
    }

    [Fact]
    public async Task GetTenant_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var tenantId = Guid.NewGuid();
        _tenantManagementService.GetTenantByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result<TenantDetailResponse>.Failure("Tenant not found", "NOT_FOUND"));

        // Act
        var result = await _controller.GetTenant(tenantId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetTenant(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetTenantByIdentifier Tests

    [Fact]
    public async Task GetTenantByIdentifier_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var identifier = "test-tenant";
        var tenant = new TenantDetailResponse { Id = Guid.NewGuid(), Identifier = identifier };
        _tenantManagementService.GetTenantByIdentifierAsync(identifier, Arg.Any<CancellationToken>())
            .Returns(Result<TenantDetailResponse>.Success(tenant));

        // Act
        var result = await _controller.GetTenantByIdentifier(identifier, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<TenantDetailResponse>();
        response.Identifier.ShouldBe(identifier);
    }

    [Fact]
    public async Task GetTenantByIdentifier_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetTenantByIdentifier("test", CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region CreateTenant Tests

    [Fact]
    public async Task CreateTenant_WithPermission_ReturnsCreated()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsCreate);
        var request = new CreateTenantRequest { Name = "New Tenant", Identifier = "new-tenant" };
        var tenantId = Guid.NewGuid();
        _tenantManagementService.CreateTenantAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(tenantId));

        // Act
        var result = await _controller.CreateTenant(request, CancellationToken.None);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(tenantId);
    }

    [Fact]
    public async Task CreateTenant_Conflict_ReturnsConflict()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsCreate);
        var request = new CreateTenantRequest { Name = "Existing", Identifier = "existing" };
        _tenantManagementService.CreateTenantAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Tenant already exists", "CONFLICT"));

        // Act
        var result = await _controller.CreateTenant(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new CreateTenantRequest { Name = "New" };

        // Act
        var result = await _controller.CreateTenant(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateTenant Tests

    [Fact]
    public async Task UpdateTenant_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsEdit);
        var tenantId = Guid.NewGuid();
        var request = new UpdateTenantRequest { Name = "Updated" };
        _tenantManagementService.UpdateTenantAsync(tenantId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateTenant(tenantId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateTenant_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsEdit);
        var tenantId = Guid.NewGuid();
        var request = new UpdateTenantRequest { Name = "Updated" };
        _tenantManagementService.UpdateTenantAsync(tenantId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Tenant not found", "NOT_FOUND"));

        // Act
        var result = await _controller.UpdateTenant(tenantId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new UpdateTenantRequest { Name = "Updated" };

        // Act
        var result = await _controller.UpdateTenant(Guid.NewGuid(), request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteTenant Tests

    [Fact]
    public async Task DeleteTenant_WithPermission_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsDelete);
        var tenantId = Guid.NewGuid();
        _tenantManagementService.DeleteTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DeleteTenant(tenantId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.DeleteTenant(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region ActivateTenant Tests

    [Fact]
    public async Task ActivateTenant_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsEdit);
        var tenantId = Guid.NewGuid();
        _tenantManagementService.ActivateTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.ActivateTenant(tenantId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task ActivateTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.ActivateTenant(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region DeactivateTenant Tests

    [Fact]
    public async Task DeactivateTenant_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsEdit);
        var tenantId = Guid.NewGuid();
        _tenantManagementService.DeactivateTenantAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DeactivateTenant(tenantId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeactivateTenant_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.DeactivateTenant(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetTenantFeatures Tests

    [Fact]
    public async Task GetTenantFeatures_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var tenantId = Guid.NewGuid();
        var features = new List<TenantFeatureResponse>
        {
            new() { FeatureName = "Feature1", IsEnabled = true }
        };
        _tenantManagementService.GetTenantFeaturesAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<TenantFeatureResponse>>.Success(features));

        // Act
        var result = await _controller.GetTenantFeatures(tenantId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTenantFeatures_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetTenantFeatures(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region SetTenantFeatures Tests

    [Fact]
    public async Task SetTenantFeatures_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsManageFeatures);
        var tenantId = Guid.NewGuid();
        var features = new List<SetFeatureRequest>
        {
            new() { FeatureName = "Feature1", IsEnabled = true }
        };
        _tenantManagementService.SetFeaturesAsync(tenantId, features, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.SetTenantFeatures(tenantId, features, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task SetTenantFeatures_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var features = new List<SetFeatureRequest>();

        // Act
        var result = await _controller.SetTenantFeatures(Guid.NewGuid(), features, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region EnableFeature Tests

    [Fact]
    public async Task EnableFeature_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsManageFeatures);
        var tenantId = Guid.NewGuid();
        var featureName = "Feature1";
        _tenantManagementService.EnableFeatureAsync(tenantId, featureName, null, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.EnableFeature(tenantId, featureName, null, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task EnableFeature_WithConfiguration_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsManageFeatures);
        var tenantId = Guid.NewGuid();
        var featureName = "Feature1";
        var config = new FeatureConfigurationRequest { Configuration = "{\"limit\": 100}" };
        _tenantManagementService.EnableFeatureAsync(tenantId, featureName, config.Configuration, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.EnableFeature(tenantId, featureName, config, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task EnableFeature_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.EnableFeature(Guid.NewGuid(), "Feature1", null, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region DisableFeature Tests

    [Fact]
    public async Task DisableFeature_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsManageFeatures);
        var tenantId = Guid.NewGuid();
        var featureName = "Feature1";
        _tenantManagementService.DisableFeatureAsync(tenantId, featureName, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DisableFeature(tenantId, featureName, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DisableFeature_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.DisableFeature(Guid.NewGuid(), "Feature1", CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region IsFeatureEnabled Tests

    [Fact]
    public async Task IsFeatureEnabled_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var tenantId = Guid.NewGuid();
        _tenantManagementService.IsFeatureEnabledAsync(tenantId, "Feature1", Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        // Act
        var result = await _controller.IsFeatureEnabled(tenantId, "Feature1", CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(true);
    }

    [Fact]
    public async Task IsFeatureEnabled_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.IsFeatureEnabled(Guid.NewGuid(), "Feature1", CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetTenantStatistics Tests

    [Fact]
    public async Task GetTenantStatistics_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.TenantsView);
        var tenantId = Guid.NewGuid();
        var stats = new TenantStatisticsResponse { TotalUsers = 10, ActiveUsers = 5 };
        _tenantManagementService.GetTenantStatisticsAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(Result<TenantStatisticsResponse>.Success(stats));

        // Act
        var result = await _controller.GetTenantStatistics(tenantId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetTenantStatistics_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetTenantStatistics(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@test.com")
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = claimsPrincipal;
    }

    #endregion
}

public class DashboardControllerTests
{
    private readonly ITenantManagementService _tenantManagementService;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _tenantManagementService = Substitute.For<ITenantManagementService>();
        _controller = new DashboardController(_tenantManagementService);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetStatistics_ReturnsOk()
    {
        // Arrange
        var stats = new DashboardStatisticsResponse
        {
            TotalUsers = 100,
            ActiveUsers = 80,
            TotalTenants = 10,
            ActiveTenants = 8
        };
        _tenantManagementService.GetDashboardStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<DashboardStatisticsResponse>.Success(stats));

        // Act
        var result = await _controller.GetStatistics(CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<DashboardStatisticsResponse>();
        response.TotalUsers.ShouldBe(100);
    }
}
