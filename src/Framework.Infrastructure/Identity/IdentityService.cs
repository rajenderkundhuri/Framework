using System.Security.Cryptography;
using Framework.Application.Common.Models;
using Framework.Application.Identity.Interfaces;
using Framework.Application.Identity.Models;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// Identity management service implementation
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly DbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IDateTime _dateTime;
    private readonly ICurrentUser _currentUser;

    public IdentityService(
        DbContext context,
        ITokenService tokenService,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IDateTime dateTime,
        ICurrentUser currentUser)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _dateTime = dateTime;
        _currentUser = currentUser;
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

        if (user == null)
            return Result<TokenResponse>.Unauthorized("Invalid email or password");

        if (!user.IsActive)
            return Result<TokenResponse>.Unauthorized("User account is disabled");

        if (user.IsDeleted)
            return Result<TokenResponse>.Unauthorized("User account has been deleted");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > _dateTime.Now)
            return Result<TokenResponse>.Unauthorized($"Account is locked until {user.LockoutEnd}");

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            user.AccessFailedCount++;
            if (user.LockoutEnabled && user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = _dateTime.Now.AddMinutes(15);
            }
            await _context.SaveChangesAsync(cancellationToken);
            return Result<TokenResponse>.Unauthorized("Invalid email or password");
        }

        // Reset access failed count on successful login
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync(cancellationToken);

        return await _tokenService.GenerateTokenAsync(user.Id, cancellationToken);
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

        if (existingUser != null)
            return Result<Guid>.Conflict("A user with this email already exists");

        var user = new ApplicationUser
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
            IsActive = true,
            LockoutEnabled = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        // Assign default role if exists
        var defaultRole = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.IsDefault, cancellationToken);

        if (defaultRole != null)
        {
            user.UserRoles.Add(new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = defaultRole.Id
            });
        }

        _context.Set<ApplicationUser>().Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }

    public async Task<Result<UserProfileResponse>> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result<UserProfileResponse>.NotFound("User not found");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.Permissions)
            .Select(p => p.Permission)
            .Distinct()
            .ToList();

        // Get user profile for theme settings
        var userProfile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var themeSettings = new ThemeSettingsResponse
        {
            IsDarkMode = userProfile?.Theme == ThemePreference.Dark,
            ThemeColorName = userProfile?.ThemeColorName ?? "Default"
        };

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Roles = roles,
            Permissions = permissions,
            ThemeSettings = themeSettings
        };
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
        user.SetModified(_dateTime.Now, _currentUser.UserId ?? "system");

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
            return Result.Unauthorized("Current password is incorrect");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.SetModified(_dateTime.Now, _currentUser.UserId ?? "system");

        // Invalidate refresh token on password change
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);

        if (user == null)
            return Result<string>.NotFound("User not found");

        // Generate a simple reset token (in production, use a more secure method)
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Store token in SecurityStamp for simplicity (in production, use a dedicated table)
        user.SecurityStamp = token;
        await _context.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == request.Email.ToUpperInvariant(), cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        if (user.SecurityStamp != request.Token)
            return Result.Unauthorized("Invalid or expired reset token");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        user.SetModified(_dateTime.Now, "system");

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        // Simple token validation (in production, use a proper email confirmation flow)
        if (user.SecurityStamp != token)
            return Result.Unauthorized("Invalid confirmation token");

        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.SetModified(_dateTime.Now, userId.ToString());

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ApplicationUser>()
            .AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Result<IEnumerable<string>>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result<IEnumerable<string>>.NotFound("User not found");

        var roles = user.UserRoles.Select(ur => ur.Role.Name);
        return Result<IEnumerable<string>>.Success(roles);
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        var role = await _context.Set<ApplicationRole>()
            .FirstOrDefaultAsync(r => r.NormalizedName == roleName.ToUpperInvariant(), cancellationToken);

        if (role == null)
            return Result.NotFound("Role not found");

        if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
            return Result.Conflict("User already has this role");

        user.UserRoles.Add(new ApplicationUserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        var userRole = user.UserRoles.FirstOrDefault(ur => ur.Role.NormalizedName == roleName.ToUpperInvariant());
        if (userRole == null)
            return Result.NotFound("User does not have this role");

        user.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Set<ApplicationUser>()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result<IEnumerable<string>>.NotFound("User not found");

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.Permissions)
            .Select(p => p.Permission)
            .Distinct();

        return Result<IEnumerable<string>>.Success(permissions);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissionsResult = await GetUserPermissionsAsync(userId, cancellationToken);
        if (permissionsResult.IsFailure)
            return false;

        return permissionsResult.Value?.Contains(permission) ?? false;
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _tokenService.RevokeTokenAsync(userId, cancellationToken);
    }

    public async Task<Result> UpdateThemeSettingsAsync(Guid userId, bool isDarkMode, string themeColorName, CancellationToken cancellationToken = default)
    {
        var userProfile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (userProfile == null)
        {
            // Create a new user profile if it doesn't exist
            userProfile = new UserProfile(Guid.NewGuid(), userId);
            _context.Set<UserProfile>().Add(userProfile);
        }

        userProfile.SetThemeSettings(
            isDarkMode ? ThemePreference.Dark : ThemePreference.Light,
            themeColorName);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ThemeSettingsResponse>> GetThemeSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userProfile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return new ThemeSettingsResponse
        {
            IsDarkMode = userProfile?.Theme == ThemePreference.Dark,
            ThemeColorName = userProfile?.ThemeColorName ?? "Default"
        };
    }
}
