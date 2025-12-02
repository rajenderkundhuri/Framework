using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.Identity.Models;

namespace Framework.Admin.Services;

public interface IProfileApiService
{
    Task<Result<UserProfileResponse>> GetProfileAsync();
    Task<Result> UpdateProfileAsync(UpdateProfileRequest request);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request);
    Task<Result<ThemeSettingsResponse>> GetThemeSettingsAsync();
    Task<Result> UpdateThemeSettingsAsync(bool isDarkMode, string themeColorName);
}

public record UpdateProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}

public class ProfileApiService : IProfileApiService
{
    private readonly HttpClient _httpClient;

    public ProfileApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<UserProfileResponse>("api/auth/profile");
            return response != null
                ? Result<UserProfileResponse>.Success(response)
                : Result<UserProfileResponse>.Failure("Failed to get profile", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<UserProfileResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/auth/profile", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<ThemeSettingsResponse>> GetThemeSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ThemeSettingsResponse>("api/auth/theme-settings");
            return response != null
                ? Result<ThemeSettingsResponse>.Success(response)
                : Result<ThemeSettingsResponse>.Failure("Failed to get theme settings", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<ThemeSettingsResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> UpdateThemeSettingsAsync(bool isDarkMode, string themeColorName)
    {
        try
        {
            var request = new UpdateThemeSettingsRequest
            {
                IsDarkMode = isDarkMode,
                ThemeColorName = themeColorName
            };
            var response = await _httpClient.PutAsJsonAsync("api/auth/theme-settings", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }
}
