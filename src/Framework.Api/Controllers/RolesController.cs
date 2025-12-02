using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Role management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class RolesController : ApiControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RolesController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    /// <summary>
    /// Get paginated list of roles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<RoleListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles([FromQuery] RoleListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetRolesAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetRoleByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleByName(string name, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetRoleByNameAsync(name, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesCreate))
            return Forbid();

        var result = await _roleManagementService.CreateRoleAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetRole), new { id = result.Value });
    }

    /// <summary>
    /// Update a role
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesEdit))
            return Forbid();

        var result = await _roleManagementService.UpdateRoleAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesDelete))
            return Forbid();

        var result = await _roleManagementService.DeleteRoleAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Set role as default
    /// </summary>
    [HttpPost("{id:guid}/set-default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultRole(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesEdit))
            return Forbid();

        var result = await _roleManagementService.SetDefaultRoleAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get users in role
    /// </summary>
    [HttpGet("{id:guid}/users")]
    [ProducesResponseType(typeof(IEnumerable<UserListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsersInRole(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetUsersInRoleAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user count in role
    /// </summary>
    [HttpGet("{id:guid}/user-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserCountInRole(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetUserCountInRoleAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Set role permissions (replace all)
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetRolePermissions(Guid id, [FromBody] List<string> permissions, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesManagePermissions))
            return Forbid();

        var result = await _roleManagementService.SetRolePermissionsAsync(id, permissions, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Add permission to role
    /// </summary>
    [HttpPost("{id:guid}/permissions/{permission}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPermissionToRole(Guid id, string permission, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesManagePermissions))
            return Forbid();

        var result = await _roleManagementService.AssignPermissionToRoleAsync(id, permission, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    [HttpDelete("{id:guid}/permissions/{permission}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemovePermissionFromRole(Guid id, string permission, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesManagePermissions))
            return Forbid();

        var result = await _roleManagementService.RemovePermissionFromRoleAsync(id, permission, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Permissions controller for listing available permissions
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ApiControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public PermissionsController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    /// <summary>
    /// Get all available permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetAllPermissionsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get permissions grouped by category
    /// </summary>
    [HttpGet("grouped")]
    [ProducesResponseType(typeof(IEnumerable<PermissionGroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionsByGroup(CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.RolesView))
            return Forbid();

        var result = await _roleManagementService.GetPermissionsByGroupAsync(cancellationToken);
        return HandleResult(result);
    }
}
