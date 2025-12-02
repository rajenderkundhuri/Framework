using System.Security.Claims;
using Framework.Api.Controllers;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Framework.Api.Tests.Controllers;

public class RolesControllerTests
{
    private readonly IRoleManagementService _roleManagementService;
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _roleManagementService = Substitute.For<IRoleManagementService>();
        _controller = new RolesController(_roleManagementService);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetRoles Tests

    [Fact]
    public async Task GetRoles_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var request = new RoleListRequest { PageNumber = 1, PageSize = 10 };
        var roles = new PagedList<RoleListResponse>(new List<RoleListResponse>(), 0, 1, 10);
        _roleManagementService.GetRolesAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<PagedList<RoleListResponse>>.Success(roles));

        // Act
        var result = await _controller.GetRoles(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoles_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new RoleListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _controller.GetRoles(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetRole Tests

    [Fact]
    public async Task GetRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var roleId = Guid.NewGuid();
        var role = new RoleDetailResponse { Id = roleId, Name = "Admin" };
        _roleManagementService.GetRoleByIdAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<RoleDetailResponse>.Success(role));

        // Act
        var result = await _controller.GetRole(roleId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<RoleDetailResponse>();
        response.Id.ShouldBe(roleId);
    }

    [Fact]
    public async Task GetRole_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var roleId = Guid.NewGuid();
        _roleManagementService.GetRoleByIdAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<RoleDetailResponse>.Failure("Role not found", "NOT_FOUND"));

        // Act
        var result = await _controller.GetRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.GetRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetRoleByName Tests

    [Fact]
    public async Task GetRoleByName_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var roleName = "Admin";
        var role = new RoleDetailResponse { Id = Guid.NewGuid(), Name = roleName };
        _roleManagementService.GetRoleByNameAsync(roleName, Arg.Any<CancellationToken>())
            .Returns(Result<RoleDetailResponse>.Success(role));

        // Act
        var result = await _controller.GetRoleByName(roleName, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<RoleDetailResponse>();
        response.Name.ShouldBe(roleName);
    }

    [Fact]
    public async Task GetRoleByName_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleName = "Admin";

        // Act
        var result = await _controller.GetRoleByName(roleName, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region CreateRole Tests

    [Fact]
    public async Task CreateRole_WithPermission_ReturnsCreated()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesCreate);
        var request = new CreateRoleRequest { Name = "NewRole", Description = "New role description" };
        var roleId = Guid.NewGuid();
        _roleManagementService.CreateRoleAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(roleId));

        // Act
        var result = await _controller.CreateRole(request, CancellationToken.None);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(roleId);
    }

    [Fact]
    public async Task CreateRole_Conflict_ReturnsConflict()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesCreate);
        var request = new CreateRoleRequest { Name = "ExistingRole" };
        _roleManagementService.CreateRoleAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Role already exists", "CONFLICT"));

        // Act
        var result = await _controller.CreateRole(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new CreateRoleRequest { Name = "NewRole" };

        // Act
        var result = await _controller.CreateRole(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateRole Tests

    [Fact]
    public async Task UpdateRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesEdit);
        var roleId = Guid.NewGuid();
        var request = new UpdateRoleRequest { Name = "UpdatedRole", Description = "Updated description" };
        _roleManagementService.UpdateRoleAsync(roleId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateRole(roleId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateRole_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesEdit);
        var roleId = Guid.NewGuid();
        var request = new UpdateRoleRequest { Name = "UpdatedRole" };
        _roleManagementService.UpdateRoleAsync(roleId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Role not found", "NOT_FOUND"));

        // Act
        var result = await _controller.UpdateRole(roleId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();
        var request = new UpdateRoleRequest { Name = "UpdatedRole" };

        // Act
        var result = await _controller.UpdateRole(roleId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region DeleteRole Tests

    [Fact]
    public async Task DeleteRole_WithPermission_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesDelete);
        var roleId = Guid.NewGuid();
        _roleManagementService.DeleteRoleAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DeleteRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.DeleteRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region SetDefaultRole Tests

    [Fact]
    public async Task SetDefaultRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesEdit);
        var roleId = Guid.NewGuid();
        _roleManagementService.SetDefaultRoleAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.SetDefaultRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task SetDefaultRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.SetDefaultRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetUsersInRole Tests

    [Fact]
    public async Task GetUsersInRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var roleId = Guid.NewGuid();
        var users = new List<UserListResponse>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@test.com" },
            new() { Id = Guid.NewGuid(), Email = "user2@test.com" }
        };
        _roleManagementService.GetUsersInRoleAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<UserListResponse>>.Success(users));

        // Act
        var result = await _controller.GetUsersInRole(roleId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeAssignableTo<IEnumerable<UserListResponse>>();
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetUsersInRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.GetUsersInRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetUserCountInRole Tests

    [Fact]
    public async Task GetUserCountInRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var roleId = Guid.NewGuid();
        _roleManagementService.GetUserCountInRoleAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(5));

        // Act
        var result = await _controller.GetUserCountInRole(roleId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(5);
    }

    [Fact]
    public async Task GetUserCountInRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.GetUserCountInRole(roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region SetRolePermissions Tests

    [Fact]
    public async Task SetRolePermissions_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesManagePermissions);
        var roleId = Guid.NewGuid();
        var permissions = new List<string> { "Permission1", "Permission2" };
        _roleManagementService.SetRolePermissionsAsync(roleId, permissions, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.SetRolePermissions(roleId, permissions, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task SetRolePermissions_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();
        var permissions = new List<string> { "Permission1" };

        // Act
        var result = await _controller.SetRolePermissions(roleId, permissions, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region AddPermissionToRole Tests

    [Fact]
    public async Task AddPermissionToRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesManagePermissions);
        var roleId = Guid.NewGuid();
        var permission = "NewPermission";
        _roleManagementService.AssignPermissionToRoleAsync(roleId, permission, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.AddPermissionToRole(roleId, permission, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task AddPermissionToRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();
        var permission = "NewPermission";

        // Act
        var result = await _controller.AddPermissionToRole(roleId, permission, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region RemovePermissionFromRole Tests

    [Fact]
    public async Task RemovePermissionFromRole_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesManagePermissions);
        var roleId = Guid.NewGuid();
        var permission = "PermissionToRemove";
        _roleManagementService.RemovePermissionFromRoleAsync(roleId, permission, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.RemovePermissionFromRole(roleId, permission, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task RemovePermissionFromRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();
        var permission = "PermissionToRemove";

        // Act
        var result = await _controller.RemovePermissionFromRole(roleId, permission, CancellationToken.None);

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

public class PermissionsControllerTests
{
    private readonly IRoleManagementService _roleManagementService;
    private readonly PermissionsController _controller;

    public PermissionsControllerTests()
    {
        _roleManagementService = Substitute.For<IRoleManagementService>();
        _controller = new PermissionsController(_roleManagementService);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAllPermissions_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var permissions = new List<PermissionResponse>
        {
            new() { Name = "Permission1", Description = "Desc1" },
            new() { Name = "Permission2", Description = "Desc2" }
        };
        _roleManagementService.GetAllPermissionsAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<PermissionResponse>>.Success(permissions));

        // Act
        var result = await _controller.GetAllPermissions(CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAllPermissions_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetAllPermissions(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPermissionsByGroup_WithPermission_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.RolesView);
        var groups = new List<PermissionGroupResponse>
        {
            new() { GroupName = "Users", Permissions = new List<PermissionResponse>() }
        };
        _roleManagementService.GetPermissionsByGroupAsync(Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<PermissionGroupResponse>>.Success(groups));

        // Act
        var result = await _controller.GetPermissionsByGroup(CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPermissionsByGroup_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());

        // Act
        var result = await _controller.GetPermissionsByGroup(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

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
}
