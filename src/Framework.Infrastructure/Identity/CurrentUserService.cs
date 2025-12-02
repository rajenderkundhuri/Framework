using System.Security.Claims;
using Framework.Domain.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// Service for accessing current user information from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name) ?? User?.Identity?.Name;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public string? TenantId => User?.FindFirstValue("tenant_id") ?? User?.FindFirstValue("TenantId");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> Roles =>
        User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public string? GetClaim(string claimType) => User?.FindFirstValue(claimType);

    public IEnumerable<string> GetClaims(string claimType) =>
        User?.FindAll(claimType).Select(c => c.Value) ?? [];
}
