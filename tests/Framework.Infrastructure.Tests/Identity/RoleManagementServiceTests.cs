using Framework.Application.Identity;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Framework.Infrastructure.Identity;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Framework.Infrastructure.Tests.Identity;

public class RoleManagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManagementService _service;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.UserId.Returns("test-user");
        _dateTime = Substitute.For<IDateTime>();
        _dateTime.Now.Returns(DateTimeOffset.UtcNow);
        _logger = Substitute.For<ILogger<RoleManagementService>>();

        _service = new RoleManagementService(
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

    #region GetRolesAsync Tests

    [Fact]
    public async Task GetRolesAsync_ReturnsPagedList()
    {
        // Arrange
        await SeedTestRoles();
        var request = new RoleListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetRolesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetRolesAsync_WithSearchTerm_FiltersRoles()
    {
        // Arrange
        await SeedTestRoles();
        var request = new RoleListRequest { SearchTerm = "Admin", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetRolesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task GetRolesAsync_WithSystemFilter_FiltersRoles()
    {
        // Arrange
        await SeedTestRoles();
        var request = new RoleListRequest { IsSystem = true, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetRolesAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    #endregion

    #region GetRoleByIdAsync Tests

    [Fact]
    public async Task GetRoleByIdAsync_ExistingRole_ReturnsRole()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.GetRoleByIdAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(role.Name, result.Value!.Name);
    }

    [Fact]
    public async Task GetRoleByIdAsync_NonExistingRole_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetRoleByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region GetRoleByNameAsync Tests

    [Fact]
    public async Task GetRoleByNameAsync_ExistingRole_ReturnsRole()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.GetRoleByNameAsync("TestRole");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(role.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetRoleByNameAsync_NonExistingRole_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetRoleByNameAsync("NonExistent");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region CreateRoleAsync Tests

    [Fact]
    public async Task CreateRoleAsync_ValidRequest_CreatesRole()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "NewRole",
            Description = "A new test role",
            IsDefault = false,
            Permissions = new List<string> { Permissions.UsersView }
        };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == result.Value);
        Assert.NotNull(role);
        Assert.Equal("NewRole", role.Name);
        Assert.Single(role.Permissions);
    }

    [Fact]
    public async Task CreateRoleAsync_DuplicateName_ReturnsConflict()
    {
        // Arrange
        await CreateTestRole("ExistingRole");
        var request = new CreateRoleRequest
        {
            Name = "ExistingRole",
            Description = "Duplicate role",
            Permissions = new List<string>()
        };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    [Fact]
    public async Task CreateRoleAsync_AsDefault_UnsetsOtherDefaults()
    {
        // Arrange
        var existingDefault = await CreateTestRole("ExistingDefault", isDefault: true);
        var request = new CreateRoleRequest
        {
            Name = "NewDefault",
            Description = "New default role",
            IsDefault = true,
            Permissions = new List<string>()
        };

        // Act
        var result = await _service.CreateRoleAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var oldDefault = await _context.Set<ApplicationRole>().FindAsync(existingDefault.Id);
        Assert.False(oldDefault!.IsDefault);
    }

    #endregion

    #region UpdateRoleAsync Tests

    [Fact]
    public async Task UpdateRoleAsync_ExistingRole_UpdatesRole()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var request = new UpdateRoleRequest
        {
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateRoleAsync(role.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await _context.Set<ApplicationRole>().FindAsync(role.Id);
        Assert.Equal("UpdatedRole", updatedRole!.Name);
    }

    [Fact]
    public async Task UpdateRoleAsync_SystemRole_ReturnsForbidden()
    {
        // Arrange
        var role = await CreateTestRole("SystemRole", isSystem: true);
        var request = new UpdateRoleRequest { Name = "Updated" };

        // Act
        var result = await _service.UpdateRoleAsync(role.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task UpdateRoleAsync_NonExistingRole_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateRoleRequest { Name = "Updated" };

        // Act
        var result = await _service.UpdateRoleAsync(Guid.NewGuid(), request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region DeleteRoleAsync Tests

    [Fact]
    public async Task DeleteRoleAsync_ExistingRole_DeletesRole()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.DeleteRoleAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedRole = await _context.Set<ApplicationRole>().FindAsync(role.Id);
        Assert.Null(deletedRole);
    }

    [Fact]
    public async Task DeleteRoleAsync_SystemRole_ReturnsForbidden()
    {
        // Arrange
        var role = await CreateTestRole("SystemRole", isSystem: true);

        // Act
        var result = await _service.DeleteRoleAsync(role.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteRoleAsync_RoleWithUsers_ReturnsConflict()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var user = await CreateTestUser("test@example.com");
        _context.Set<ApplicationUserRole>().Add(new ApplicationUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteRoleAsync(role.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    #endregion

    #region Permission Management Tests

    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsAllPermissions()
    {
        // Act
        var result = await _service.GetAllPermissionsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }

    [Fact]
    public async Task GetPermissionsByGroupAsync_ReturnsGroupedPermissions()
    {
        // Act
        var result = await _service.GetPermissionsByGroupAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_ValidPermission_AssignsPermission()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.AssignPermissionToRoleAsync(role.Id, Permissions.UsersView);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == role.Id);
        Assert.Single(updatedRole!.Permissions);
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_InvalidPermission_ReturnsFailure()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.AssignPermissionToRoleAsync(role.Id, "Invalid.Permission");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_PERMISSION", result.ErrorCode);
    }

    [Fact]
    public async Task RemovePermissionFromRoleAsync_ExistingPermission_RemovesPermission()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        await _service.AssignPermissionToRoleAsync(role.Id, Permissions.UsersView);

        // Act
        var result = await _service.RemovePermissionFromRoleAsync(role.Id, Permissions.UsersView);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == role.Id);
        Assert.Empty(updatedRole!.Permissions);
    }

    [Fact]
    public async Task SetRolePermissionsAsync_ValidPermissions_SetsPermissions()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var permissions = new[] { Permissions.UsersView, Permissions.UsersCreate };

        // Act
        var result = await _service.SetRolePermissionsAsync(role.Id, permissions);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == role.Id);
        Assert.Equal(2, updatedRole!.Permissions.Count);
    }

    #endregion

    #region SetDefaultRoleAsync Tests

    [Fact]
    public async Task SetDefaultRoleAsync_ExistingRole_SetsDefault()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.SetDefaultRoleAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedRole = await _context.Set<ApplicationRole>().FindAsync(role.Id);
        Assert.True(updatedRole!.IsDefault);
    }

    #endregion

    #region GetUsersInRoleAsync Tests

    [Fact]
    public async Task GetUsersInRoleAsync_RoleWithUsers_ReturnsUsers()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var user = await CreateTestUser("test@example.com");
        _context.Set<ApplicationUserRole>().Add(new ApplicationUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUsersInRoleAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetUserCountInRoleAsync_ReturnsCorrectCount()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var user1 = await CreateTestUser("user1@example.com");
        var user2 = await CreateTestUser("user2@example.com");
        _context.Set<ApplicationUserRole>().AddRange(
            new ApplicationUserRole { UserId = user1.Id, RoleId = role.Id },
            new ApplicationUserRole { UserId = user2.Id, RoleId = role.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserCountInRoleAsync(role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationRole> CreateTestRole(string name, bool isDefault = false, bool isSystem = false)
    {
        var role = new ApplicationRole(Guid.NewGuid())
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = $"Test role: {name}",
            IsDefault = isDefault,
            IsSystem = isSystem
        };
        role.SetCreated(_dateTime.Now, "test");

        _context.Set<ApplicationRole>().Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    private async Task<ApplicationUser> CreateTestUser(string email)
    {
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = email.Split('@')[0],
            LastName = "Test",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        user.SetCreated(_dateTime.Now, "test");

        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task SeedTestRoles()
    {
        await CreateTestRole("Admin");
        await CreateTestRole("User");
        await CreateTestRole("SystemRole", isSystem: true);
    }

    #endregion
}
