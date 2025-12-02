using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.Identity;

namespace Framework.Admin.Services;

public interface IRoleApiService
{
    Task<Result<PagedList<RoleListResponse>>> GetRolesAsync(RoleListRequest request);
    Task<Result<RoleDetailResponse>> GetRoleByIdAsync(Guid roleId);
    Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request);
    Task<Result> DeleteRoleAsync(Guid roleId);
    Task<Result<IEnumerable<PermissionResponse>>> GetAllPermissionsAsync();
    Task<Result<IEnumerable<PermissionGroupResponse>>> GetPermissionsByGroupAsync();
    Task<Result> SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions);
}

public class RoleApiService : IRoleApiService
{
    private readonly HttpClient _httpClient;

    public RoleApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<PagedList<RoleListResponse>>> GetRolesAsync(RoleListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<RoleListResponse>>($"api/roles{query}");
            return response != null
                ? Result<PagedList<RoleListResponse>>.Success(response)
                : Result<PagedList<RoleListResponse>>.Failure("Failed to get roles", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<RoleListResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<RoleDetailResponse>> GetRoleByIdAsync(Guid roleId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<RoleDetailResponse>($"api/roles/{roleId}");
            return response != null
                ? Result<RoleDetailResponse>.Success(response)
                : Result<RoleDetailResponse>.NotFound("Role not found");
        }
        catch (Exception ex)
        {
            return Result<RoleDetailResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateRoleAsync(CreateRoleRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/roles", request);
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

    public async Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/roles/{roleId}", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteRoleAsync(Guid roleId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/roles/{roleId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionResponse>>> GetAllPermissionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<PermissionResponse>>("api/permissions");
            return response != null
                ? Result<IEnumerable<PermissionResponse>>.Success(response)
                : Result<IEnumerable<PermissionResponse>>.Failure("Failed to get permissions", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PermissionResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<PermissionGroupResponse>>> GetPermissionsByGroupAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<PermissionGroupResponse>>("api/permissions/grouped");
            return response != null
                ? Result<IEnumerable<PermissionGroupResponse>>.Success(response)
                : Result<IEnumerable<PermissionGroupResponse>>.Failure("Failed to get permissions", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PermissionGroupResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/roles/{roleId}/permissions", permissions);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(RoleListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (request.IsSystem.HasValue)
            query.Add($"isSystem={request.IsSystem.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
