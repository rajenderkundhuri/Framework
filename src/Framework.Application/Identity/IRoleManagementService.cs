using Framework.Application.Common.Models;

namespace Framework.Application.Identity;

/// <summary>
/// Role and permission management service interface
/// </summary>
public interface IRoleManagementService
{
    // Role CRUD
    Task<Result<PagedList<RoleListResponse>>> GetRolesAsync(RoleListRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleDetailResponse>> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<RoleDetailResponse>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    // Permission Management
    Task<Result<IEnumerable<PermissionResponse>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PermissionGroupResponse>>> GetPermissionsByGroupAsync(CancellationToken cancellationToken = default);
    Task<Result> AssignPermissionToRoleAsync(Guid roleId, string permission, CancellationToken cancellationToken = default);
    Task<Result> RemovePermissionFromRoleAsync(Guid roleId, string permission, CancellationToken cancellationToken = default);
    Task<Result> SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    // Role Hierarchy
    Task<Result> SetDefaultRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserListResponse>>> GetUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<int>> GetUserCountInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// Role list request
/// </summary>
public class RoleListRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsSystem { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Create role request
/// </summary>
public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Update role request
/// </summary>
public class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsDefault { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Role list response
/// </summary>
public class RoleListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Role detail response
/// </summary>
public class RoleDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = new();
}

/// <summary>
/// Permission response
/// </summary>
public class PermissionResponse
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty;
}

/// <summary>
/// Permission group response
/// </summary>
public class PermissionGroupResponse
{
    public string GroupName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<PermissionResponse> Permissions { get; set; } = new();
}

#endregion
