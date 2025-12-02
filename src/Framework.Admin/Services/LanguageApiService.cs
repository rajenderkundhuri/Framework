using System.Net.Http.Json;
using Framework.Application.Common.Models;
using Framework.Application.Localization;

namespace Framework.Admin.Services;

public interface ILanguageApiService
{
    Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync();
    Task<Result<Guid>> AddLanguageAsync(AddLanguageRequest request);
    Task<Result> UpdateLanguageAsync(Guid languageId, UpdateLanguageRequest request);
    Task<Result> SetDefaultLanguageAsync(Guid languageId);
    Task<Result> EnableLanguageAsync(Guid languageId);
    Task<Result> DisableLanguageAsync(Guid languageId);
}

public class LanguageApiService : ILanguageApiService
{
    private readonly HttpClient _httpClient;

    public LanguageApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<LanguageResponse>>("api/languages");
            return response != null
                ? Result<IEnumerable<LanguageResponse>>.Success(response)
                : Result<IEnumerable<LanguageResponse>>.Failure("Failed to get languages", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<LanguageResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> AddLanguageAsync(AddLanguageRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/languages", request);
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

    public async Task<Result> UpdateLanguageAsync(Guid languageId, UpdateLanguageRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/languages/{languageId}", request);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> SetDefaultLanguageAsync(Guid languageId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/languages/{languageId}/default", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> EnableLanguageAsync(Guid languageId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/languages/{languageId}/enable", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DisableLanguageAsync(Guid languageId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/languages/{languageId}/disable", null);
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
