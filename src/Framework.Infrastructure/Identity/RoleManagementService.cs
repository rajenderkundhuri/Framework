using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// Role and permission management service implementation
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly DbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        DbContext context,
        ICurrentUser currentUser,
        IDateTime dateTime,
        ILogger<RoleManagementService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    #region Role CRUD

    public async Task<Result<PagedList<RoleListResponse>>> GetRolesAsync(
        RoleListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(r =>
                (r.Name != null && r.Name.ToLower().Contains(search)) ||
                (r.Description != null && r.Description.ToLower().Contains(search)));
        }

        if (request.IsSystem.HasValue)
        {
            query = query.Where(r => r.IsSystem == request.IsSystem.Value);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "description" => request.SortDescending
                ? query.OrderByDescending(r => r.Description)
                : query.OrderBy(r => r.Description),
            "createdat" => request.SortDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt),
            _ => request.SortDescending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var roles = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RoleListResponse
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description,
                IsDefault = r.IsDefault,
                IsSystem = r.IsSystem,
                UserCount = _context.Set<ApplicationUserRole>().Count(ur => ur.RoleId == r.Id),
                PermissionCount = r.Permissions.Count,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedList<RoleListResponse>(roles, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<Result<RoleDetailResponse>> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result<RoleDetailResponse>.NotFound("Role not found");

        var userCount = await _context.Set<ApplicationUserRole>()
            .CountAsync(ur => ur.RoleId == roleId, cancellationToken);

        return new RoleDetailResponse
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsDefault = role.IsDefault,
            IsSystem = role.IsSystem,
            UserCount = userCount,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            LastModifiedAt = role.LastModifiedAt,
            LastModifiedBy = role.LastModifiedBy,
            Permissions = role.Permissions.Select(p => new PermissionResponse
            {
                Name = p.Permission,
                DisplayName = Permissions.GetDisplayName(p.Permission),
                Description = Permissions.GetDescription(p.Permission),
                Group = GetPermissionGroup(p.Permission)
            }).ToList()
        };
    }

    public async Task<Result<RoleDetailResponse>> GetRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);

        if (role == null)
            return Result<RoleDetailResponse>.NotFound("Role not found");

        return await GetRoleByIdAsync(role.Id, cancellationToken);
    }

    public async Task<Result<Guid>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for existing role
        var existingRole = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.NormalizedName == request.Name.ToUpperInvariant(), cancellationToken);

        if (existingRole != null)
            return Result<Guid>.Conflict("A role with this name already exists");

        var role = new ApplicationRole(Guid.NewGuid())
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpperInvariant(),
            Description = request.Description,
            IsDefault = request.IsDefault,
            IsSystem = false
        };

        role.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        // If this is the default role, unset other defaults
        if (request.IsDefault)
        {
            var currentDefaults = await _context.Set<ApplicationRole>()
                .Where(r => r.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var defaultRole in currentDefaults)
            {
                defaultRole.IsDefault = false;
                defaultRole.SetModified(_dateTime.Now, _currentUser.UserId);
            }
        }

        // Add permissions
        foreach (var permission in request.Permissions)
        {
            if (Permissions.GetAll().Contains(permission))
            {
                role.Permissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Permission = permission
                });
            }
        }

        _context.Set<ApplicationRole>().Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created role {RoleId} with name {RoleName}", role.Id, role.Name);

        return role.Id;
    }

    public async Task<Result> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (role.IsSystem)
            return Result.Forbidden("Cannot modify system roles");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Check for duplicate name
            var existingRole = await _context.Set<ApplicationRole>()
                .FirstOrDefaultAsync(r => r.NormalizedName == request.Name.ToUpperInvariant() && r.Id != roleId, cancellationToken);

            if (existingRole != null)
                return Result.Conflict("A role with this name already exists");

            role.Name = request.Name;
            role.NormalizedName = request.Name.ToUpperInvariant();
        }

        if (request.Description != null)
            role.Description = request.Description;

        if (request.IsDefault.HasValue)
        {
            if (request.IsDefault.Value)
            {
                // Unset other defaults
                var currentDefaults = await _context.Set<ApplicationRole>()
                    .Where(r => r.IsDefault && r.Id != roleId)
                    .ToListAsync(cancellationToken);

                foreach (var defaultRole in currentDefaults)
                {
                    defaultRole.IsDefault = false;
                    defaultRole.SetModified(_dateTime.Now, _currentUser.UserId);
                }
            }

            role.IsDefault = request.IsDefault.Value;
        }

        role.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated role {RoleId}", roleId);

        return Result.Success();
    }

    public async Task<Result> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (role.IsSystem)
            return Result.Forbidden("Cannot delete system roles");

        // Check if role has users
        var hasUsers = await _context.Set<ApplicationUserRole>()
            .AnyAsync(ur => ur.RoleId == roleId, cancellationToken);

        if (hasUsers)
            return Result.Conflict("Cannot delete role that has assigned users");

        // Remove permissions
        _context.Set<RolePermission>().RemoveRange(role.Permissions);

        // Remove role
        _context.Set<ApplicationRole>().Remove(role);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted role {RoleId}", roleId);

        return Result.Success();
    }

    #endregion

    #region Permission Management

    public Task<Result<IEnumerable<PermissionResponse>>> GetAllPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = Permissions.GetAll().Select(p => new PermissionResponse
        {
            Name = p,
            DisplayName = Permissions.GetDisplayName(p),
            Description = Permissions.GetDescription(p),
            Group = GetPermissionGroup(p)
        });

        return Task.FromResult(Result<IEnumerable<PermissionResponse>>.Success(permissions));
    }

    public Task<Result<IEnumerable<PermissionGroupResponse>>> GetPermissionsByGroupAsync(
        CancellationToken cancellationToken = default)
    {
        var groups = Permissions.GetGrouped().Select(g => new PermissionGroupResponse
        {
            GroupName = g.Name,
            DisplayName = g.DisplayName,
            Permissions = g.Permissions.Select(p => new PermissionResponse
            {
                Name = p,
                DisplayName = Permissions.GetDisplayName(p),
                Description = Permissions.GetDescription(p),
                Group = g.Name
            }).ToList()
        });

        return Task.FromResult(Result<IEnumerable<PermissionGroupResponse>>.Success(groups));
    }

    public async Task<Result> AssignPermissionToRoleAsync(
        Guid roleId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (role.IsSystem)
            return Result.Forbidden("Cannot modify system role permissions");

        if (!Permissions.GetAll().Contains(permission))
            return Result.Failure("Invalid permission", "INVALID_PERMISSION");

        if (role.Permissions.Any(p => p.Permission == permission))
            return Result.Conflict("Role already has this permission");

        role.Permissions.Add(new RolePermission
        {
            RoleId = roleId,
            Permission = permission
        });

        role.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Assigned permission {Permission} to role {RoleId}", permission, roleId);

        return Result.Success();
    }

    public async Task<Result> RemovePermissionFromRoleAsync(
        Guid roleId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (role.IsSystem)
            return Result.Forbidden("Cannot modify system role permissions");

        var rolePermission = role.Permissions.FirstOrDefault(p => p.Permission == permission);
        if (rolePermission == null)
            return Result.NotFound("Role does not have this permission");

        role.Permissions.Remove(rolePermission);
        _context.Set<RolePermission>().Remove(rolePermission);

        role.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed permission {Permission} from role {RoleId}", permission, roleId);

        return Result.Success();
    }

    public async Task<Result> SetRolePermissionsAsync(
        Guid roleId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (role.IsSystem)
            return Result.Forbidden("Cannot modify system role permissions");

        // Validate permissions
        var validPermissions = Permissions.GetAll().ToHashSet();
        var invalidPermissions = permissions.Where(p => !validPermissions.Contains(p)).ToList();
        if (invalidPermissions.Any())
            return Result.Failure($"Invalid permissions: {string.Join(", ", invalidPermissions)}", "INVALID_PERMISSION");

        // Remove existing permissions
        _context.Set<RolePermission>().RemoveRange(role.Permissions);

        // Add new permissions
        foreach (var permission in permissions.Distinct())
        {
            role.Permissions.Add(new RolePermission
            {
                RoleId = roleId,
                Permission = permission
            });
        }

        role.SetModified(_dateTime.Now, _currentUser.UserId);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set {Count} permissions for role {RoleId}", permissions.Count(), roleId);

        return Result.Success();
    }

    #endregion

    #region Role Hierarchy

    public async Task<Result> SetDefaultRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        // Unset current defaults
        var currentDefaults = await _context.Set<ApplicationRole>()
            .Where(r => r.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var defaultRole in currentDefaults)
        {
            defaultRole.IsDefault = false;
            defaultRole.SetModified(_dateTime.Now, _currentUser.UserId);
        }

        // Set new default
        role.IsDefault = true;
        role.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set role {RoleId} as default", roleId);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<UserListResponse>>> GetUsersInRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await _context.Set<ApplicationRole>()
            .AnyAsync(r => r.Id == roleId, cancellationToken);

        if (!roleExists)
            return Result<IEnumerable<UserListResponse>>.NotFound("Role not found");

        var users = await _context.Set<ApplicationUserRole>()
            .Where(ur => ur.RoleId == roleId)
            .Include(ur => ur.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(ur => !ur.User.IsDeleted)
            .Select(ur => new UserListResponse
            {
                Id = ur.User.Id,
                Email = ur.User.Email ?? string.Empty,
                FirstName = ur.User.FirstName,
                LastName = ur.User.LastName,
                FullName = ur.User.FullName,
                IsActive = ur.User.IsActive,
                EmailConfirmed = ur.User.EmailConfirmed,
                CreatedAt = ur.User.CreatedAt,
                Roles = ur.User.UserRoles.Select(r => r.Role.Name ?? string.Empty).ToList()
            })
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<UserListResponse>>.Success(users);
    }

    public async Task<Result<int>> GetUserCountInRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var roleExists = await _context.Set<ApplicationRole>()
            .AnyAsync(r => r.Id == roleId, cancellationToken);

        if (!roleExists)
            return Result<int>.NotFound("Role not found");

        var count = await _context.Set<ApplicationUserRole>()
            .Where(ur => ur.RoleId == roleId)
            .Join(_context.Set<ApplicationUser>().Where(u => !u.IsDeleted),
                ur => ur.UserId,
                u => u.Id,
                (ur, u) => ur)
            .CountAsync(cancellationToken);

        return count;
    }

    #endregion

    #region Private Methods

    private static string GetPermissionGroup(string permission)
    {
        var parts = permission.Split('.');
        return parts.Length > 0 ? parts[0] : "Other";
    }

    #endregion
}
