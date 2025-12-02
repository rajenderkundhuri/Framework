using Framework.Application.Common.Models;
using Framework.Application.OpenIdConnect;
using Framework.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.OpenIdConnect;

/// <summary>
/// External login service implementation
/// </summary>
public class ExternalLoginService : IExternalLoginService
{
    private readonly DbContext _context;
    private readonly OpenIdConnectSettings _settings;
    private readonly ILogger<ExternalLoginService> _logger;

    public ExternalLoginService(
        DbContext context,
        IOptions<OpenIdConnectSettings> settings,
        ILogger<ExternalLoginService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ExternalLoginResult?> FindByLoginAsync(
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var login = await _context.Set<ExternalLogin>()
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey, cancellationToken);

        if (login == null)
            return null;

        return new ExternalLoginResult
        {
            UserId = login.UserId,
            Email = login.User.Email,
            UserFound = true,
            IsLinked = true,
            Provider = provider,
            ProviderKey = providerKey
        };
    }

    public async Task<Result> LinkLoginAsync(
        Guid userId,
        string provider,
        string providerKey,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user exists
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return Result.NotFound("User not found");

        // Check if login is already linked
        var existingLogin = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.Provider == provider && e.ProviderKey == providerKey, cancellationToken);

        if (existingLogin != null)
        {
            if (existingLogin.UserId == userId)
                return Result.Success(); // Already linked to this user

            return Result.Conflict("This external login is already linked to another user");
        }

        // Check if user already has this provider linked
        var userLogin = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        if (userLogin != null)
            return Result.Conflict("User already has this provider linked");

        // Create new link
        var login = new ExternalLogin(
            Guid.NewGuid(),
            userId,
            provider,
            providerKey,
            displayName);

        _context.Set<ExternalLogin>().Add(login);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Linked external login {Provider} to user {UserId}", provider, userId);

        return Result.Success();
    }

    public async Task<Result> UnlinkLoginAsync(
        Guid userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var login = await _context.Set<ExternalLogin>()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        if (login == null)
            return Result.NotFound("External login not found");

        _context.Set<ExternalLogin>().Remove(login);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Unlinked external login {Provider} from user {UserId}", provider, userId);

        return Result.Success();
    }

    public async Task<IEnumerable<ExternalLoginInfo>> GetLoginsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var logins = await _context.Set<ExternalLogin>()
            .Where(e => e.UserId == userId)
            .Select(e => new ExternalLoginInfo
            {
                Provider = e.Provider,
                ProviderKey = e.ProviderKey,
                DisplayName = e.ProviderDisplayName,
                LinkedAt = e.LinkedAt
            })
            .ToListAsync(cancellationToken);

        return logins;
    }

    public async Task<Result<ExternalLoginResult>> ProcessExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string? name = null,
        IDictionary<string, string>? claims = null,
        CancellationToken cancellationToken = default)
    {
        // First, check if this external login is already linked
        var existingLogin = await FindByLoginAsync(provider, providerKey, cancellationToken);
        if (existingLogin != null)
        {
            _logger.LogInformation("External login {Provider}:{ProviderKey} found for user {UserId}",
                provider, providerKey, existingLogin.UserId);
            return existingLogin;
        }

        // Find user by email
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user == null)
        {
            if (_settings.ExternalProviders.RequireExistingUser)
            {
                _logger.LogWarning("External login attempt from {Provider} for non-existing user {Email}",
                    provider, email);

                return Result<ExternalLoginResult>.Unauthorized(
                    "User must exist in the system before linking external login. Please register first or contact administrator.");
            }

            if (!_settings.ExternalProviders.AutoCreateUser)
            {
                return Result<ExternalLoginResult>.Unauthorized(
                    "User not found and auto-creation is disabled");
            }

            // Auto-create user (if enabled)
            user = new ApplicationUser(Guid.NewGuid())
            {
                Email = email,
                UserName = email,
                NormalizedEmail = email.ToUpperInvariant(),
                NormalizedUserName = email.ToUpperInvariant(),
                FirstName = name?.Split(' ').FirstOrDefault() ?? email.Split('@')[0],
                LastName = name?.Split(' ').Skip(1).FirstOrDefault() ?? string.Empty,
                EmailConfirmed = true, // External provider already verified email
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            user.SetCreated(DateTimeOffset.UtcNow, "external-login");

            _context.Set<ApplicationUser>().Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Auto-created user {UserId} from external login {Provider}", user.Id, provider);
        }

        // Link the external login
        var linkResult = await LinkLoginAsync(user.Id, provider, providerKey, name, cancellationToken);
        if (!linkResult.IsSuccess)
        {
            return Result<ExternalLoginResult>.Failure(linkResult.Error!);
        }

        return new ExternalLoginResult
        {
            UserId = user.Id,
            Email = user.Email,
            UserFound = true,
            IsLinked = true,
            Provider = provider,
            ProviderKey = providerKey
        };
    }
}
