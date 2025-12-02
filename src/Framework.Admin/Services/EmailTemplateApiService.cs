using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Framework.Application.Common.Models;
using Framework.Application.Email;
using Framework.Domain.Email;

namespace Framework.Admin.Services;

public interface IEmailTemplateApiService
{
    Task<Result<PagedList<EmailTemplateResponse>>> GetTemplatesAsync(EmailTemplateListRequest request);
    Task<Result<EmailTemplateResponse>> GetByIdAsync(Guid id);
    Task<Result<Guid>> CreateAsync(CreateEmailTemplateRequest request);
    Task<Result> UpdateAsync(Guid id, UpdateEmailTemplateRequest request);
    Task<Result> DeleteAsync(Guid id);
    Task<Result> ActivateAsync(Guid id);
    Task<Result> DeactivateAsync(Guid id);
    Task<Result<PagedList<EmailLogResponse>>> GetEmailLogsAsync(EmailLogListRequest request);
    Task<Result<EmailLogResponse>> GetEmailLogByIdAsync(Guid id);
}

public class EmailTemplateApiService : IEmailTemplateApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EmailTemplateApiService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _jsonOptions = jsonOptions;
    }

    public async Task<Result<PagedList<EmailTemplateResponse>>> GetTemplatesAsync(EmailTemplateListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<EmailTemplateResponse>>($"api/emailtemplates{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<EmailTemplateResponse>>.Success(response)
                : Result<PagedList<EmailTemplateResponse>>.Failure("Failed to get templates", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<EmailTemplateResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<EmailTemplateResponse>> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EmailTemplateResponse>($"api/emailtemplates/{id}", _jsonOptions);
            return response != null
                ? Result<EmailTemplateResponse>.Success(response)
                : Result<EmailTemplateResponse>.NotFound("Template not found");
        }
        catch (Exception ex)
        {
            return Result<EmailTemplateResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateAsync(CreateEmailTemplateRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/emailtemplates", request);
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

    public async Task<Result> UpdateAsync(Guid id, UpdateEmailTemplateRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/emailtemplates/{id}", request);
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
            var response = await _httpClient.DeleteAsync($"api/emailtemplates/{id}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> ActivateAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/emailtemplates/{id}/activate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeactivateAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/emailtemplates/{id}/deactivate", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<PagedList<EmailLogResponse>>> GetEmailLogsAsync(EmailLogListRequest request)
    {
        try
        {
            var query = BuildEmailLogQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<EmailLogResponse>>($"api/emailtemplates/logs{query}");
            return response != null
                ? Result<PagedList<EmailLogResponse>>.Success(response)
                : Result<PagedList<EmailLogResponse>>.Failure("Failed to get email logs", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<EmailLogResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<EmailLogResponse>> GetEmailLogByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EmailLogResponse>($"api/emailtemplates/logs/{id}");
            return response != null
                ? Result<EmailLogResponse>.Success(response)
                : Result<EmailLogResponse>.NotFound("Email log not found");
        }
        catch (Exception ex)
        {
            return Result<EmailLogResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(EmailTemplateListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (request.Type.HasValue)
            query.Add($"type={request.Type.Value}");
        if (request.IsActive.HasValue)
            query.Add($"isActive={request.IsActive.Value}");
        if (!string.IsNullOrEmpty(request.LanguageCode))
            query.Add($"languageCode={Uri.EscapeDataString(request.LanguageCode)}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortBy={request.SortBy}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }

    private static string BuildEmailLogQueryString(EmailLogListRequest request)
    {
        var query = new List<string>();

        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        if (request.Status.HasValue)
            query.Add($"status={request.Status.Value}");
        if (request.TemplateId.HasValue)
            query.Add($"templateId={request.TemplateId.Value}");
        if (request.FromDate.HasValue)
            query.Add($"fromDate={request.FromDate.Value:o}");
        if (request.ToDate.HasValue)
            query.Add($"toDate={request.ToDate.Value:o}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
