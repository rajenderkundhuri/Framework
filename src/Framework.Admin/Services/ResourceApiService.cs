using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.Localization;

namespace Framework.Admin.Services;

public interface IResourceApiService
{
    Task<Result<PagedList<ResourceResponse>>> GetResourcesAsync(ResourceListRequest request);
    Task<Result<ResourceResponse>> GetResourceByIdAsync(Guid resourceId);
    Task<Result<Guid>> CreateResourceAsync(CreateResourceRequest request);
    Task<Result> UpdateResourceAsync(Guid resourceId, UpdateResourceRequest request);
    Task<Result> DeleteResourceAsync(Guid resourceId);
    Task<Result<IEnumerable<string>>> GetResourceGroupsAsync();
    Task<Result<LocalizationStatisticsResponse>> GetStatisticsAsync();
    Task<Result<BulkResourceResult>> ImportResourcesAsync(IEnumerable<ImportResourceRequest> resources);
    Task<Result<IEnumerable<ExportResourceResponse>>> ExportResourcesAsync(ExportResourceRequest request);
}

public class ResourceApiService : IResourceApiService
{
    private readonly HttpClient _httpClient;

    public ResourceApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<PagedList<ResourceResponse>>> GetResourcesAsync(ResourceListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<ResourceResponse>>($"api/resources{query}");
            return response != null
                ? Result<PagedList<ResourceResponse>>.Success(response)
                : Result<PagedList<ResourceResponse>>.Failure("Failed to get resources", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<ResourceResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<ResourceResponse>> GetResourceByIdAsync(Guid resourceId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ResourceResponse>($"api/resources/{resourceId}");
            return response != null
                ? Result<ResourceResponse>.Success(response)
                : Result<ResourceResponse>.NotFound("Resource not found");
        }
        catch (Exception ex)
        {
            return Result<ResourceResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateResourceAsync(CreateResourceRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/resources", request);
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

    public async Task<Result> UpdateResourceAsync(Guid resourceId, UpdateResourceRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/resources/{resourceId}", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteResourceAsync(Guid resourceId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/resources/{resourceId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetResourceGroupsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/resources/groups");
            return response != null
                ? Result<IEnumerable<string>>.Success(response)
                : Result<IEnumerable<string>>.Failure("Failed to get groups", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<LocalizationStatisticsResponse>> GetStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<LocalizationStatisticsResponse>("api/resources/statistics");
            return response != null
                ? Result<LocalizationStatisticsResponse>.Success(response)
                : Result<LocalizationStatisticsResponse>.Failure("Failed to get statistics", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<LocalizationStatisticsResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<BulkResourceResult>> ImportResourcesAsync(IEnumerable<ImportResourceRequest> resources)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/resources/import", resources);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<BulkResourceResult>();
                return result != null
                    ? Result<BulkResourceResult>.Success(result)
                    : Result<BulkResourceResult>.Failure("Failed to import resources", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<BulkResourceResult>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<BulkResourceResult>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<ExportResourceResponse>>> ExportResourcesAsync(ExportResourceRequest request)
    {
        try
        {
            var query = BuildExportQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<ExportResourceResponse>>($"api/resources/export{query}");
            return response != null
                ? Result<IEnumerable<ExportResourceResponse>>.Success(response)
                : Result<IEnumerable<ExportResourceResponse>>.Failure("Failed to export resources", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<ExportResourceResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(ResourceListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (!string.IsNullOrEmpty(request.CultureName))
            query.Add($"cultureName={Uri.EscapeDataString(request.CultureName)}");
        if (!string.IsNullOrEmpty(request.Group))
            query.Add($"group={Uri.EscapeDataString(request.Group)}");
        if (request.IsSystem.HasValue)
            query.Add($"isSystem={request.IsSystem.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }

    private static string BuildExportQueryString(ExportResourceRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.CultureName))
            query.Add($"cultureName={Uri.EscapeDataString(request.CultureName)}");
        if (!string.IsNullOrEmpty(request.Group))
            query.Add($"group={Uri.EscapeDataString(request.Group)}");
        query.Add($"format={request.Format}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
