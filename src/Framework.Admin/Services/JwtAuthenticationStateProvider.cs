using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Framework.Admin.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    private const string TokenKey = "authToken";

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(_anonymous);
            }

            var claims = ParseClaimsFromJwt(token);
            if (claims == null)
            {
                return new AuthenticationState(_anonymous);
            }

            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var exp))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expDate < DateTimeOffset.UtcNow)
                {
                    await _localStorage.RemoveItemAsync(TokenKey);
                    return new AuthenticationState(_anonymous);
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        if (claims != null)
        {
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);
        }
    }

    public void NotifyUserLogout()
    {
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims;
        }
        catch
        {
            return null;
        }
    }
}
