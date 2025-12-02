using System.Security.Claims;
using Framework.Api.Controllers;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace Framework.Api.Tests.Controllers;

public class UsersControllerTests
{
    private readonly IUserManagementService _userManagementService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userManagementService = Substitute.For<IUserManagementService>();
        _controller = new UsersController(_userManagementService);

        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_WithPermission_ReturnsPagedList()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var request = new UserListRequest { PageNumber = 1, PageSize = 10 };
        var pagedList = new PagedList<UserListResponse>(
            new List<UserListResponse>
            {
                new() { Id = Guid.NewGuid(), Email = "test@test.com" }
            }, 1, 1, 10);

        _userManagementService.GetUsersAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<PagedList<UserListResponse>>.Success(pagedList));

        // Act
        var result = await _controller.GetUsers(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<PagedList<UserListResponse>>();
        response.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetUsers_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid()); // No permissions
        var request = new UserListRequest();

        // Act
        var result = await _controller.GetUsers(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_ExistingUser_ReturnsUserDetail()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var userId = Guid.NewGuid();
        var userDetail = new UserDetailResponse
        {
            Id = userId,
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        _userManagementService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDetailResponse>.Success(userDetail));

        // Act
        var result = await _controller.GetUser(userId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<UserDetailResponse>();
        response.Id.ShouldBe(userId);
    }

    [Fact]
    public async Task GetUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var userId = Guid.NewGuid();

        _userManagementService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserDetailResponse>.Failure("User not found", "NOT_FOUND"));

        // Act
        var result = await _controller.GetUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUser_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.GetUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreated()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersCreate);
        var request = new CreateUserRequest
        {
            Email = "newuser@test.com",
            FirstName = "New",
            LastName = "User",
            Password = "Password123!"
        };
        var newUserId = Guid.NewGuid();

        _userManagementService.CreateUserAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(newUserId));

        // Act
        var result = await _controller.CreateUser(request, CancellationToken.None);

        // Assert
        var createdResult = result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(newUserId);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersCreate);
        var request = new CreateUserRequest
        {
            Email = "existing@test.com",
            FirstName = "New",
            LastName = "User",
            Password = "Password123!"
        };

        _userManagementService.CreateUserAsync(request, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure("Email already exists", "CONFLICT"));

        // Act
        var result = await _controller.CreateUser(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateUser_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var request = new CreateUserRequest();

        // Act
        var result = await _controller.CreateUser(request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };

        _userManagementService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateUser(userId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task UpdateUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest();

        _userManagementService.UpdateUserAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("User not found", "NOT_FOUND"));

        // Act
        var result = await _controller.UpdateUser(userId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersDelete);
        var userId = Guid.NewGuid();

        _userManagementService.DeleteUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersDelete);
        var userId = Guid.NewGuid();

        _userManagementService.DeleteUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("User not found", "NOT_FOUND"));

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region ActivateUser Tests

    [Fact]
    public async Task ActivateUser_ExistingUser_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var userId = Guid.NewGuid();

        _userManagementService.ActivateUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.ActivateUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task DeactivateUser_ExistingUser_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var userId = Guid.NewGuid();

        _userManagementService.DeactivateUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.DeactivateUser(userId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region User Roles Tests

    [Fact]
    public async Task GetUserRoles_ExistingUser_ReturnsRoles()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var userId = Guid.NewGuid();
        var roles = new List<RoleResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin" }
        };

        _userManagementService.GetUserRolesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<RoleResponse>>.Success(roles));

        // Act
        var result = await _controller.GetUserRoles(userId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeAssignableTo<IEnumerable<RoleResponse>>();
        response.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AssignRole_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersManageRoles);
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userManagementService.AssignRoleAsync(userId, roleId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.AssignRole(userId, roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task AssignRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        // Act
        var result = await _controller.AssignRole(userId, roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RemoveRole_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersManageRoles);
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _userManagementService.RemoveRoleAsync(userId, roleId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.RemoveRole(userId, roleId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region User Permissions Tests

    [Fact]
    public async Task GetUserPermissions_ExistingUser_ReturnsPermissions()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var userId = Guid.NewGuid();
        var permissions = new List<string> { "Users.View", "Users.Create" };

        _userManagementService.GetUserPermissionsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<IEnumerable<string>>.Success(permissions));

        // Act
        var result = await _controller.GetUserPermissions(userId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeAssignableTo<IEnumerable<string>>();
        response.ShouldContain("Users.View");
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersResetPassword);
        var userId = Guid.NewGuid();
        var request = new ResetPasswordAdminRequest { NewPassword = "NewPassword123!" };

        _userManagementService.ResetPasswordAsync(userId, request.NewPassword, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.ResetPassword(userId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public async Task ResetPassword_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var userId = Guid.NewGuid();
        var request = new ResetPasswordAdminRequest { NewPassword = "NewPassword123!" };

        // Act
        var result = await _controller.ResetPassword(userId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region User Preferences Tests

    [Fact]
    public async Task GetUserPreferences_ExistingUser_ReturnsPreferences()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersView);
        var userId = Guid.NewGuid();
        var preferences = new UserPreferencesResponse
        {
            UserId = userId,
            Locale = "en-US",
            TimeZoneId = "UTC"
        };

        _userManagementService.GetUserPreferencesAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<UserPreferencesResponse>.Success(preferences));

        // Act
        var result = await _controller.GetUserPreferences(userId, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<UserPreferencesResponse>();
        response.Locale.ShouldBe("en-US");
    }

    [Fact]
    public async Task UpdateUserPreferences_ValidRequest_ReturnsOk()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var userId = Guid.NewGuid();
        var request = new UpdateUserPreferencesRequest
        {
            Locale = "de-DE",
            TimeZoneId = "Europe/Berlin"
        };

        _userManagementService.UpdateUserPreferencesAsync(userId, request, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _controller.UpdateUserPreferences(userId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkActivateUsers_ValidRequest_ReturnsBulkResult()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var request = new BulkUserRequest { UserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };
        var bulkResult = new BulkOperationResult
        {
            SuccessCount = 2,
            FailureCount = 0
        };

        _userManagementService.BulkActivateUsersAsync(request.UserIds, Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResult>.Success(bulkResult));

        // Act
        var result = await _controller.BulkActivateUsers(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<BulkOperationResult>();
        response.SuccessCount.ShouldBe(2);
    }

    [Fact]
    public async Task BulkDeactivateUsers_ValidRequest_ReturnsBulkResult()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersEdit);
        var request = new BulkUserRequest { UserIds = new List<Guid> { Guid.NewGuid() } };
        var bulkResult = new BulkOperationResult { SuccessCount = 1 };

        _userManagementService.BulkDeactivateUsersAsync(request.UserIds, Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResult>.Success(bulkResult));

        // Act
        var result = await _controller.BulkDeactivateUsers(request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<BulkOperationResult>();
        response.SuccessCount.ShouldBe(1);
    }

    [Fact]
    public async Task BulkAssignRole_ValidRequest_ReturnsBulkResult()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid(), Permissions.UsersManageRoles);
        var roleId = Guid.NewGuid();
        var request = new BulkUserRequest { UserIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };
        var bulkResult = new BulkOperationResult { SuccessCount = 2 };

        _userManagementService.BulkAssignRoleAsync(request.UserIds, roleId, Arg.Any<CancellationToken>())
            .Returns(Result<BulkOperationResult>.Success(bulkResult));

        // Act
        var result = await _controller.BulkAssignRole(roleId, request, CancellationToken.None);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<BulkOperationResult>();
        response.SuccessCount.ShouldBe(2);
    }

    [Fact]
    public async Task BulkAssignRole_WithoutPermission_ReturnsForbid()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid());
        var roleId = Guid.NewGuid();
        var request = new BulkUserRequest { UserIds = new List<Guid> { Guid.NewGuid() } };

        // Act
        var result = await _controller.BulkAssignRole(roleId, request, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ForbidResult>();
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@test.com")
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
