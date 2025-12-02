using Framework.Application.Identity;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Framework.Infrastructure.Identity;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Framework.Infrastructure.Tests.Identity;

public class UserManagementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserManagementService _service;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new PasswordHasher<ApplicationUser>();
        _currentUser = Substitute.For<ICurrentUser>();
        _currentUser.UserId.Returns("test-user");
        _dateTime = Substitute.For<IDateTime>();
        _dateTime.Now.Returns(DateTimeOffset.UtcNow);
        _logger = Substitute.For<ILogger<UserManagementService>>();

        _service = new UserManagementService(
            _context,
            _passwordHasher,
            _currentUser,
            _dateTime,
            _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_ReturnsPagedList()
    {
        // Arrange
        await SeedTestUsers();
        var request = new UserListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchTerm_FiltersUsers()
    {
        // Arrange
        await SeedTestUsers();
        var request = new UserListRequest { SearchTerm = "john", PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task GetUsersAsync_WithActiveFilter_FiltersUsers()
    {
        // Arrange
        await SeedTestUsers();
        var request = new UserListRequest { IsActive = true, PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");

        // Act
        var result = await _service.GetUserByIdAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Email, result.Value!.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistingUser_ReturnsNotFound()
    {
        // Act
        var result = await _service.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_ValidRequest_CreatesUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            IsActive = true,
            RoleIds = new List<Guid>()
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var user = await _context.Set<ApplicationUser>().FindAsync(result.Value);
        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        await CreateTestUser("existing@example.com");
        var request = new CreateUserRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            IsActive = true,
            RoleIds = new List<Guid>()
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    [Fact]
    public async Task CreateUserAsync_WithRoles_AssignsRoles()
    {
        // Arrange
        var role = await CreateTestRole("TestRole");
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            IsActive = true,
            RoleIds = new List<Guid> { role.Id }
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == result.Value);
        Assert.NotNull(user);
        Assert.Single(user.UserRoles);
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_ExistingUser_UpdatesUser()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var request = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "Name"
        };

        // Act
        var result = await _service.UpdateUserAsync(user.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedUser = await _context.Set<ApplicationUser>().FindAsync(user.Id);
        Assert.Equal("Updated", updatedUser!.FirstName);
    }

    [Fact]
    public async Task UpdateUserAsync_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest { FirstName = "Updated" };

        // Act
        var result = await _service.UpdateUserAsync(Guid.NewGuid(), request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_SoftDeletesUser()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");

        // Act
        var result = await _service.DeleteUserAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deletedUser = await _context.Set<ApplicationUser>().FindAsync(user.Id);
        Assert.True(deletedUser!.IsDeleted);
    }

    #endregion

    #region ActivateUserAsync / DeactivateUserAsync Tests

    [Fact]
    public async Task ActivateUserAsync_ExistingUser_ActivatesUser()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com", isActive: false);

        // Act
        var result = await _service.ActivateUserAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var activatedUser = await _context.Set<ApplicationUser>().FindAsync(user.Id);
        Assert.True(activatedUser!.IsActive);
    }

    [Fact]
    public async Task DeactivateUserAsync_ExistingUser_DeactivatesUser()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com", isActive: true);

        // Act
        var result = await _service.DeactivateUserAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var deactivatedUser = await _context.Set<ApplicationUser>().FindAsync(user.Id);
        Assert.False(deactivatedUser!.IsActive);
    }

    #endregion

    #region Role Assignment Tests

    [Fact]
    public async Task AssignRoleAsync_ValidUserAndRole_AssignsRole()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var role = await CreateTestRole("TestRole");

        // Act
        var result = await _service.AssignRoleAsync(user.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var userRoles = await _context.Set<ApplicationUserRole>()
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task AssignRoleAsync_DuplicateRole_ReturnsConflict()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var role = await CreateTestRole("TestRole");
        await _service.AssignRoleAsync(user.Id, role.Id);

        // Act
        var result = await _service.AssignRoleAsync(user.Id, role.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("CONFLICT", result.ErrorCode);
    }

    [Fact]
    public async Task RemoveRoleAsync_ExistingUserRole_RemovesRole()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var role = await CreateTestRole("TestRole");
        await _service.AssignRoleAsync(user.Id, role.Id);

        // Act
        var result = await _service.RemoveRoleAsync(user.Id, role.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var userRoles = await _context.Set<ApplicationUserRole>()
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync();
        Assert.Empty(userRoles);
    }

    [Fact]
    public async Task GetUserRolesAsync_UserWithRoles_ReturnsRoles()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var role = await CreateTestRole("TestRole");
        await _service.AssignRoleAsync(user.Id, role.Id);

        // Act
        var result = await _service.GetUserRolesAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    #endregion

    #region Password Management Tests

    [Fact]
    public async Task ResetPasswordAsync_ExistingUser_ResetsPassword()
    {
        // Arrange
        var user = await CreateTestUser("test@example.com");
        var oldPasswordHash = user.PasswordHash;

        // Act
        var result = await _service.ResetPasswordAsync(user.Id, "NewPassword123!");

        // Assert
        Assert.True(result.IsSuccess);
        var updatedUser = await _context.Set<ApplicationUser>().FindAsync(user.Id);
        Assert.NotEqual(oldPasswordHash, updatedUser!.PasswordHash);
    }

    #endregion

    #region Bulk Operations Tests

    [Fact]
    public async Task BulkActivateUsersAsync_MultipleUsers_ActivatesAll()
    {
        // Arrange
        var user1 = await CreateTestUser("user1@example.com", isActive: false);
        var user2 = await CreateTestUser("user2@example.com", isActive: false);

        // Act
        var result = await _service.BulkActivateUsersAsync(new[] { user1.Id, user2.Id });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.SuccessCount);
        Assert.Equal(0, result.Value.FailureCount);
    }

    [Fact]
    public async Task BulkDeactivateUsersAsync_MultipleUsers_DeactivatesAll()
    {
        // Arrange
        var user1 = await CreateTestUser("user1@example.com", isActive: true);
        var user2 = await CreateTestUser("user2@example.com", isActive: true);

        // Act
        var result = await _service.BulkDeactivateUsersAsync(new[] { user1.Id, user2.Id });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.SuccessCount);
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUser(string email, bool isActive = true)
    {
        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = email.Split('@')[0],
            LastName = "Test",
            IsActive = isActive,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
        user.SetCreated(_dateTime.Now, "test");

        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    private async Task<ApplicationRole> CreateTestRole(string name)
    {
        var role = new ApplicationRole(Guid.NewGuid())
        {
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Description = $"Test role: {name}"
        };
        role.SetCreated(_dateTime.Now, "test");

        _context.Set<ApplicationRole>().Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    private async Task SeedTestUsers()
    {
        await CreateTestUser("john@example.com", isActive: true);
        await CreateTestUser("jane@example.com", isActive: true);
        await CreateTestUser("inactive@example.com", isActive: false);
    }

    #endregion
}
