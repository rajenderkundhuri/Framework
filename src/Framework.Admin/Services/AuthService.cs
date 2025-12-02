using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Framework.Admin.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<AuthResult> RefreshTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;

    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, result.AccessToken);
                    await _localStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken);

                    ((JwtAuthenticationStateProvider)_authStateProvider).NotifyUserAuthentication(result.AccessToken);

                    return new AuthResult { Succeeded = true };
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            return new AuthResult { Succeeded = false, Error = error };
        }
        catch (Exception ex)
        {
            return new AuthResult { Succeeded = false, Error = ex.Message };
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(RefreshTokenKey);
        ((JwtAuthenticationStateProvider)_authStateProvider).NotifyUserLogout();
    }

    public async Task<AuthResult> RefreshTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenKey);
            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
            {
                return new AuthResult { Succeeded = false, Error = "No tokens found" };
            }

            var response = await _httpClient.PostAsJsonAsync("api/auth/refresh", new RefreshTokenRequest
            {
                AccessToken = token,
                RefreshToken = refreshToken
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
                if (result != null)
                {
                    await _localStorage.SetItemAsync(TokenKey, result.AccessToken);
                    await _localStorage.SetItemAsync(RefreshTokenKey, result.RefreshToken);

                    ((JwtAuthenticationStateProvider)_authStateProvider).NotifyUserAuthentication(result.AccessToken);

                    return new AuthResult { Succeeded = true };
                }
            }

            await LogoutAsync();
            return new AuthResult { Succeeded = false, Error = "Failed to refresh token" };
        }
        catch
        {
            await LogoutAsync();
            return new AuthResult { Succeeded = false, Error = "Failed to refresh token" };
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);
        return !string.IsNullOrEmpty(token);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class AuthResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}
