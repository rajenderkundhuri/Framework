using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// User management service implementation
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly DbContext _context;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        DbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ICurrentUser currentUser,
        IDateTime dateTime,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    #region User CRUD

    public async Task<Result<PagedList<UserListResponse>>> GetUsersAsync(
        UserListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(search)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(search)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(search)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId.Value));
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "email" => request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "firstname" => request.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "lastname" => request.SortDescending ? query.OrderByDescending(u => u.LastName) : query.OrderBy(u => u.LastName),
            "isactive" => request.SortDescending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            _ => request.SortDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListResponse
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles.Select(ur => ur.Role.Name ?? string.Empty).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedList<UserListResponse>(users, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<Result<UserDetailResponse>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result<UserDetailResponse>.NotFound("User not found");

        var profile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.Permissions.Select(p => p.Permission))
            .Distinct()
            .ToList();

        return new UserDetailResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            CreatedAt = user.CreatedAt,
            CreatedBy = user.CreatedBy,
            LastModifiedAt = user.LastModifiedAt,
            LastModifiedBy = user.LastModifiedBy,
            Roles = user.UserRoles.Select(ur => new RoleResponse
            {
                Id = ur.Role.Id,
                Name = ur.Role.Name ?? string.Empty,
                Description = ur.Role.Description,
                IsDefault = ur.Role.IsDefault,
                IsSystem = ur.Role.IsSystem,
                Permissions = ur.Role.Permissions.Select(p => p.Permission).ToList()
            }).ToList(),
            Permissions = permissions,
            Preferences = profile != null ? MapToPreferencesResponse(profile) : null
        };
    }

    public async Task<Result<Guid>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for existing email
        var existingUser = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

        if (existingUser != null)
            return Result<Guid>.Conflict("A user with this email already exists");

        var user = new ApplicationUser(Guid.NewGuid())
        {
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            UserName = request.Email,
            NormalizedUserName = request.Email.ToUpperInvariant(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = request.IsActive,
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        // Assign roles
        foreach (var roleId in request.RoleIds)
        {
            var role = await _context.Set<ApplicationRole>()
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

            if (role != null)
            {
                user.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }
        }

        // Create default profile
        var profile = new UserProfile(Guid.NewGuid(), user.Id);
        profile.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        _context.Set<ApplicationUser>().Add(user);
        _context.Set<UserProfile>().Add(profile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created user {UserId} with email {Email}", user.Id, user.Email);

        return user.Id;
    }

    public async Task<Result> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.PhoneNumber != null)
            user.PhoneNumber = request.PhoneNumber;

        if (request.ProfilePictureUrl != null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = _dateTime.Now;
        user.DeletedBy = _currentUser.UserId;
        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted user {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result> ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        user.IsActive = true;
        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated user {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result> DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        user.IsActive = false;
        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated user {UserId}", userId);

        return Result.Success();
    }

    #endregion

    #region Preferences Management

    public async Task<Result<UserPreferencesResponse>> GetUserPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
        {
            // Create default profile if not exists
            profile = new UserProfile(Guid.NewGuid(), userId);
            profile.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");
            _context.Set<UserProfile>().Add(profile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToPreferencesResponse(profile);
    }

    public async Task<Result> UpdateUserPreferencesAsync(
        Guid userId,
        UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile == null)
        {
            profile = new UserProfile(Guid.NewGuid(), userId);
            profile.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");
            _context.Set<UserProfile>().Add(profile);
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
                profile.SetTimeZone(request.TimeZoneId);

            if (!string.IsNullOrWhiteSpace(request.DateFormat))
                profile.SetDateFormat(request.DateFormat);

            if (!string.IsNullOrWhiteSpace(request.TimeFormat))
                profile.SetTimeFormat(request.TimeFormat);

            if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
                profile.SetCurrency(request.CurrencyCode);

            if (!string.IsNullOrWhiteSpace(request.Locale))
                profile.SetLocale(request.Locale);

            if (!string.IsNullOrWhiteSpace(request.NumberFormatLocale))
                profile.SetNumberFormatLocale(request.NumberFormatLocale);

            if (request.Theme.HasValue)
                profile.SetTheme((ThemePreference)request.Theme.Value);

            if (request.EmailNotificationsEnabled.HasValue)
                profile.SetEmailNotifications(request.EmailNotificationsEnabled.Value);

            if (request.PushNotificationsEnabled.HasValue)
                profile.SetPushNotifications(request.PushNotificationsEnabled.Value);

            if (request.PreferredTwoFactorMethod.HasValue)
                profile.SetPreferredTwoFactorMethod((TwoFactorMethod)request.PreferredTwoFactorMethod.Value);

            profile.SetModified(_dateTime.Now, _currentUser.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated profile for user {UserId}", userId);

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, "VALIDATION_ERROR");
        }
    }

    #endregion

    #region Role Assignment

    public async Task<Result> AssignRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        var role = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (user.UserRoles.Any(ur => ur.RoleId == roleId))
            return Result.Conflict("User already has this role");

        user.UserRoles.Add(new ApplicationUserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        user.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned role {RoleId} to user {UserId}", roleId, userId);

        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var userRole = await _context.Set<ApplicationUserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (userRole == null)
            return Result.NotFound("User does not have this role");

        _context.Set<ApplicationUserRole>().Remove(userRole);

        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user != null)
            user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, userId);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<RoleResponse>>> GetUserRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result<IEnumerable<RoleResponse>>.NotFound("User not found");

        var roles = user.UserRoles.Select(ur => new RoleResponse
        {
            Id = ur.Role.Id,
            Name = ur.Role.Name ?? string.Empty,
            Description = ur.Role.Description,
            IsDefault = ur.Role.IsDefault,
            IsSystem = ur.Role.IsSystem,
            Permissions = ur.Role.Permissions.Select(p => p.Permission).ToList()
        });

        return Result<IEnumerable<RoleResponse>>.Success(roles);
    }

    #endregion

    #region Permission Check

    public async Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result<IEnumerable<string>>.NotFound("User not found");

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.Permissions.Select(p => p.Permission))
            .Distinct();

        return Result<IEnumerable<string>>.Success(permissions);
    }

    public async Task<Result<bool>> HasPermissionAsync(
        Guid userId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var hasPermission = await _context.Set<ApplicationUser>()
            .Where(u => u.Id == userId && !u.IsDeleted && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .SelectMany(ur => ur.Role.Permissions)
            .AnyAsync(p => p.Permission == permission, cancellationToken);

        return hasPermission;
    }

    #endregion

    #region Password Management

    public async Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reset password for user {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result> ForcePasswordChangeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        // Set security stamp to invalidate existing tokens
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Forced password change for user {UserId}", userId);

        return Result.Success();
    }

    #endregion

    #region Bulk Operations

    public async Task<Result<BulkOperationResult>> BulkActivateUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalRequested = userIds.Count() };

        foreach (var userId in userIds)
        {
            var activateResult = await ActivateUserAsync(userId, cancellationToken);
            if (activateResult.IsSuccess)
                result.SuccessCount++;
            else
            {
                result.FailureCount++;
                result.Errors.Add(new BulkOperationError
                {
                    EntityId = userId,
                    ErrorMessage = activateResult.Error ?? "Unknown error"
                });
            }
        }

        return result;
    }

    public async Task<Result<BulkOperationResult>> BulkDeactivateUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalRequested = userIds.Count() };

        foreach (var userId in userIds)
        {
            var deactivateResult = await DeactivateUserAsync(userId, cancellationToken);
            if (deactivateResult.IsSuccess)
                result.SuccessCount++;
            else
            {
                result.FailureCount++;
                result.Errors.Add(new BulkOperationError
                {
                    EntityId = userId,
                    ErrorMessage = deactivateResult.Error ?? "Unknown error"
                });
            }
        }

        return result;
    }

    public async Task<Result<BulkOperationResult>> BulkAssignRoleAsync(
        IEnumerable<Guid> userIds,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var result = new BulkOperationResult { TotalRequested = userIds.Count() };

        foreach (var userId in userIds)
        {
            var assignResult = await AssignRoleAsync(userId, roleId, cancellationToken);
            if (assignResult.IsSuccess)
                result.SuccessCount++;
            else
            {
                result.FailureCount++;
                result.Errors.Add(new BulkOperationError
                {
                    EntityId = userId,
                    ErrorMessage = assignResult.Error ?? "Unknown error"
                });
            }
        }

        return result;
    }

    #endregion

    #region Private Methods

    private static UserPreferencesResponse MapToPreferencesResponse(UserProfile profile)
    {
        return new UserPreferencesResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            TimeZoneId = profile.TimeZoneId,
            DateFormat = profile.DateFormat,
            TimeFormat = profile.TimeFormat,
            DateTimeFormat = profile.DateTimeFormat,
            CurrencyCode = profile.CurrencyCode,
            Locale = profile.Locale,
            NumberFormatLocale = profile.NumberFormatLocale,
            Theme = (int)profile.Theme,
            EmailNotificationsEnabled = profile.EmailNotificationsEnabled,
            PushNotificationsEnabled = profile.PushNotificationsEnabled,
            PreferredTwoFactorMethod = (int)profile.PreferredTwoFactorMethod
        };
    }

    #endregion
}
