using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.MultiTenancy;

namespace Framework.Admin.Services;

public interface ITenantApiService
{
    Task<Result<PagedList<TenantListResponse>>> GetTenantsAsync(TenantListRequest request);
    Task<Result<TenantDetailResponse>> GetTenantByIdAsync(Guid tenantId);
    Task<Result<Guid>> CreateTenantAsync(CreateTenantRequest request);
    Task<Result> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request);
    Task<Result> DeleteTenantAsync(Guid tenantId);
    Task<Result> ActivateTenantAsync(Guid tenantId);
    Task<Result> DeactivateTenantAsync(Guid tenantId);
    Task<Result<IEnumerable<TenantFeatureResponse>>> GetTenantFeaturesAsync(Guid tenantId);
    Task<Result> SetFeaturesAsync(Guid tenantId, IEnumerable<SetFeatureRequest> features);
    Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync();
}

public class TenantApiService : ITenantApiService
{
    private readonly HttpClient _httpClient;

    public TenantApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<PagedList<TenantListResponse>>> GetTenantsAsync(TenantListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<TenantListResponse>>($"api/tenants{query}");
            return response != null
                ? Result<PagedList<TenantListResponse>>.Success(response)
                : Result<PagedList<TenantListResponse>>.Failure("Failed to get tenants", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<TenantListResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<TenantDetailResponse>> GetTenantByIdAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TenantDetailResponse>($"api/tenants/{tenantId}");
            return response != null
                ? Result<TenantDetailResponse>.Success(response)
                : Result<TenantDetailResponse>.NotFound("Tenant not found");
        }
        catch (Exception ex)
        {
            return Result<TenantDetailResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateTenantAsync(CreateTenantRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/tenants", request);
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

    public async Task<Result> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/tenants/{tenantId}", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteTenantAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/tenants/{tenantId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ActivateTenantAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/tenants/{tenantId}/activate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeactivateTenantAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/tenants/{tenantId}/deactivate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<TenantFeatureResponse>>> GetTenantFeaturesAsync(Guid tenantId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<TenantFeatureResponse>>($"api/tenants/{tenantId}/features");
            return response != null
                ? Result<IEnumerable<TenantFeatureResponse>>.Success(response)
                : Result<IEnumerable<TenantFeatureResponse>>.Failure("Failed to get features", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<TenantFeatureResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> SetFeaturesAsync(Guid tenantId, IEnumerable<SetFeatureRequest> features)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/tenants/{tenantId}/features", features);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DashboardStatisticsResponse>("api/dashboard/statistics");
            return response != null
                ? Result<DashboardStatisticsResponse>.Success(response)
                : Result<DashboardStatisticsResponse>.Failure("Failed to get statistics", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<DashboardStatisticsResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(TenantListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (request.IsActive.HasValue)
            query.Add($"isActive={request.IsActive.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
