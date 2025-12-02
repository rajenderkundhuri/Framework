using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.Identity;

namespace Framework.Admin.Services;

public interface IUserApiService
{
    Task<Result<PagedList<UserListResponse>>> GetUsersAsync(UserListRequest request);
    Task<Result<UserDetailResponse>> GetUserByIdAsync(Guid userId);
    Task<Result<Guid>> CreateUserAsync(CreateUserRequest request);
    Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<Result> DeleteUserAsync(Guid userId);
    Task<Result> ActivateUserAsync(Guid userId);
    Task<Result> DeactivateUserAsync(Guid userId);
    Task<Result<IEnumerable<RoleResponse>>> GetUserRolesAsync(Guid userId);
    Task<Result> AssignRoleAsync(Guid userId, Guid roleId);
    Task<Result> RemoveRoleAsync(Guid userId, Guid roleId);
    Task<Result> ResetPasswordAsync(Guid userId, string newPassword);
}

public class UserApiService : IUserApiService
{
    private readonly HttpClient _httpClient;

    public UserApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<PagedList<UserListResponse>>> GetUsersAsync(UserListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<UserListResponse>>($"api/users{query}");
            return response != null
                ? Result<PagedList<UserListResponse>>.Success(response)
                : Result<PagedList<UserListResponse>>.Failure("Failed to get users", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<UserListResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<UserDetailResponse>> GetUserByIdAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<UserDetailResponse>($"api/users/{userId}");
            return response != null
                ? Result<UserDetailResponse>.Success(response)
                : Result<UserDetailResponse>.NotFound("User not found");
        }
        catch (Exception ex)
        {
            return Result<UserDetailResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Guid>();
                return Result<Guid>.Success(result);
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<Guid>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{userId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ActivateUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/users/{userId}/activate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeactivateUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/users/{userId}/deactivate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<RoleResponse>>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<RoleResponse>>($"api/users/{userId}/roles");
            return response != null
                ? Result<IEnumerable<RoleResponse>>.Success(response)
                : Result<IEnumerable<RoleResponse>>.Failure("Failed to get roles", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<RoleResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> AssignRoleAsync(Guid userId, Guid roleId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/users/{userId}/roles/{roleId}", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{userId}/roles/{roleId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/users/{userId}/reset-password", new { NewPassword = newPassword });
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(UserListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (request.IsActive.HasValue)
            query.Add($"isActive={request.IsActive.Value}");
        if (request.RoleId.HasValue)
            query.Add($"roleId={request.RoleId.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; } = default!;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}
