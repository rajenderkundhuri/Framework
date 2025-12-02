using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// User management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<UserListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] UserListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersView))
            return Forbid();

        var result = await _userManagementService.GetUsersAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersView))
            return Forbid();

        var result = await _userManagementService.GetUserByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersCreate))
            return Forbid();

        var result = await _userManagementService.CreateUserAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetUser), new { id = result.Value });
    }

    /// <summary>
    /// Update a user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.UpdateUserAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersDelete))
            return Forbid();

        var result = await _userManagementService.DeleteUserAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Activate a user
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.ActivateUserAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivate a user
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.DeactivateUserAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's roles
    /// </summary>
    [HttpGet("{id:guid}/roles")]
    [ProducesResponseType(typeof(IEnumerable<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRoles(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersView))
            return Forbid();

        var result = await _userManagementService.GetUserRolesAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Assign a role to user
    /// </summary>
    [HttpPost("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersManageRoles))
            return Forbid();

        var result = await _userManagementService.AssignRoleAsync(id, roleId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove a role from user
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersManageRoles))
            return Forbid();

        var result = await _userManagementService.RemoveRoleAsync(id, roleId, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user's permissions
    /// </summary>
    [HttpGet("{id:guid}/permissions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPermissions(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersView))
            return Forbid();

        var result = await _userManagementService.GetUserPermissionsAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reset user's password
    /// </summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordAdminRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersResetPassword))
            return Forbid();

        var result = await _userManagementService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    [HttpGet("{id:guid}/preferences")]
    [ProducesResponseType(typeof(UserPreferencesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserPreferences(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersView))
            return Forbid();

        var result = await _userManagementService.GetUserPreferencesAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update user preferences
    /// </summary>
    [HttpPut("{id:guid}/preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserPreferences(Guid id, [FromBody] UpdateUserPreferencesRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.UpdateUserPreferencesAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk activate users
    /// </summary>
    [HttpPost("bulk/activate")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkActivateUsers([FromBody] BulkUserRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.BulkActivateUsersAsync(request.UserIds, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk deactivate users
    /// </summary>
    [HttpPost("bulk/deactivate")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkDeactivateUsers([FromBody] BulkUserRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersEdit))
            return Forbid();

        var result = await _userManagementService.BulkDeactivateUsersAsync(request.UserIds, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk assign role to users
    /// </summary>
    [HttpPost("bulk/assign-role/{roleId:guid}")]
    [ProducesResponseType(typeof(BulkOperationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkAssignRole(Guid roleId, [FromBody] BulkUserRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.UsersManageRoles))
            return Forbid();

        var result = await _userManagementService.BulkAssignRoleAsync(request.UserIds, roleId, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Admin reset password request
/// </summary>
public record ResetPasswordAdminRequest
{
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Bulk user operation request
/// </summary>
public record BulkUserRequest
{
    public List<Guid> UserIds { get; init; } = new();
}
