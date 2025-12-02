using Framework.Application.Common.Models;

namespace Framework.Application.Identity;

/// <summary>
/// User management service interface
/// </summary>
public interface IUserManagementService
{
    // User CRUD
    Task<Result<PagedList<UserListResponse>>> GetUsersAsync(UserListRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDetailResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Preferences Management
    Task<Result<UserPreferencesResponse>> GetUserPreferencesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> UpdateUserPreferencesAsync(Guid userId, UpdateUserPreferencesRequest request, CancellationToken cancellationToken = default);

    // Role Assignment
    Task<Result> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<RoleResponse>>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    // Permission Check
    Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    // Password Management
    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> ForcePasswordChangeAsync(Guid userId, CancellationToken cancellationToken = default);

    // Bulk Operations
    Task<Result<BulkOperationResult>> BulkActivateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<Result<BulkOperationResult>> BulkDeactivateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
    Task<Result<BulkOperationResult>> BulkAssignRoleAsync(IEnumerable<Guid> userIds, Guid roleId, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// User list request with filtering and pagination
/// </summary>
public class UserListRequest
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public Guid? RoleId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Create user request
/// </summary>
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool SendWelcomeEmail { get; set; } = true;
    public List<Guid> RoleIds { get; set; } = new();
}

/// <summary>
/// Update user request
/// </summary>
public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

/// <summary>
/// Update user preferences request
/// </summary>
public class UpdateUserPreferencesRequest
{
    public string? TimeZoneId { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Locale { get; set; }
    public string? NumberFormatLocale { get; set; }
    public int? Theme { get; set; }
    public bool? EmailNotificationsEnabled { get; set; }
    public bool? PushNotificationsEnabled { get; set; }
    public int? PreferredTwoFactorMethod { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// User list item response
/// </summary>
public class UserListResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// User detail response
/// </summary>
public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public List<RoleResponse> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public UserPreferencesResponse? Preferences { get; set; }
}

/// <summary>
/// User preferences response (timezone, currency, locale settings)
/// </summary>
public class UserPreferencesResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TimeZoneId { get; set; } = "UTC";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string CurrencyCode { get; set; } = "USD";
    public string Locale { get; set; } = "en-US";
    public string NumberFormatLocale { get; set; } = "en-US";
    public int Theme { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public int PreferredTwoFactorMethod { get; set; }
}

/// <summary>
/// Role response
/// </summary>
public class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSystem { get; set; }
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResult
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
}

/// <summary>
/// Bulk operation error
/// </summary>
public class BulkOperationError
{
    public Guid EntityId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

#endregion
