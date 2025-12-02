using System.Net.Http.Json;
using System.Text.Json;
using Framework.Application.Common.Models;
using Framework.Application.Identity;

namespace Framework.Admin.Services;

public interface IApiKeyApiService
{
    Task<Result<PagedList<ApiKeyResponse>>> GetMyApiKeysAsync(ApiKeyListRequest request);
    Task<Result<PagedList<ApiKeyResponse>>> GetAllApiKeysAsync(ApiKeyListRequest request, Guid? userId = null);
    Task<Result<ApiKeyResponse>> GetByIdAsync(Guid id);
    Task<Result<CreateApiKeyResponse>> CreateMyApiKeyAsync(CreateApiKeyRequest request);
    Task<Result<CreateApiKeyResponse>> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request);
    Task<Result> UpdateAsync(Guid id, UpdateApiKeyRequest request);
    Task<Result> RevokeAsync(Guid id);
    Task<Result> DeleteAsync(Guid id);
}

public class ApiKeyApiService : IApiKeyApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiKeyApiService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _jsonOptions = jsonOptions;
    }

    public async Task<Result<PagedList<ApiKeyResponse>>> GetMyApiKeysAsync(ApiKeyListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<ApiKeyResponse>>($"api/apikeys/my{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<ApiKeyResponse>>.Success(response)
                : Result<PagedList<ApiKeyResponse>>.Failure("Failed to get API keys", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<ApiKeyResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<PagedList<ApiKeyResponse>>> GetAllApiKeysAsync(ApiKeyListRequest request, Guid? userId = null)
    {
        try
        {
            var query = BuildQueryString(request);
            if (userId.HasValue)
                query += (query.Contains('?') ? "&" : "?") + $"userId={userId.Value}";

            var response = await _httpClient.GetFromJsonAsync<PagedList<ApiKeyResponse>>($"api/apikeys{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<ApiKeyResponse>>.Success(response)
                : Result<PagedList<ApiKeyResponse>>.Failure("Failed to get API keys", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<ApiKeyResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<ApiKeyResponse>> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiKeyResponse>($"api/apikeys/{id}", _jsonOptions);
            return response != null
                ? Result<ApiKeyResponse>.Success(response)
                : Result<ApiKeyResponse>.NotFound("API key not found");
        }
        catch (Exception ex)
        {
            return Result<ApiKeyResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<CreateApiKeyResponse>> CreateMyApiKeyAsync(CreateApiKeyRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/apikeys/my", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>(_jsonOptions);
                return result != null
                    ? Result<CreateApiKeyResponse>.Success(result)
                    : Result<CreateApiKeyResponse>.Failure("Failed to create API key", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<CreateApiKeyResponse>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<CreateApiKeyResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<CreateApiKeyResponse>> CreateApiKeyAsync(Guid userId, CreateApiKeyRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/apikeys?userId={userId}", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>(_jsonOptions);
                return result != null
                    ? Result<CreateApiKeyResponse>.Success(result)
                    : Result<CreateApiKeyResponse>.Failure("Failed to create API key", "API_ERROR");
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<CreateApiKeyResponse>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<CreateApiKeyResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateApiKeyRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/apikeys/{id}", request, _jsonOptions);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> RevokeAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/apikeys/{id}/revoke", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/apikeys/{id}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(ApiKeyListRequest request)
    {
        var query = new List<string>();

        if (request.IsActive.HasValue)
            query.Add($"isActive={request.IsActive.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
