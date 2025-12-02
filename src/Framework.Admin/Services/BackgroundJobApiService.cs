using System.Net.Http.Json;
using System.Text.Json;
using Framework.Application.BackgroundJobs;
using Framework.Application.Common.Models;
using Framework.Domain.BackgroundJobs;

namespace Framework.Admin.Services;

public interface IBackgroundJobApiService
{
    Task<Result<JobQueryResult>> GetJobsAsync(JobFilter filter);
    Task<Result<JobInfo>> GetJobByIdAsync(string jobId);
    Task<Result<JobStatistics>> GetStatisticsAsync();
    Task<Result> RequeueJobAsync(string jobId);
    Task<Result> TriggerRecurringJobAsync(string jobId);
    Task<Result> DeleteJobAsync(string jobId);
    Task<Result> RemoveRecurringJobAsync(string jobId);
}

public class BackgroundJobApiService : IBackgroundJobApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BackgroundJobApiService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _jsonOptions = jsonOptions;
    }

    public async Task<Result<JobQueryResult>> GetJobsAsync(JobFilter filter)
    {
        try
        {
            var query = BuildQueryString(filter);
            var response = await _httpClient.GetFromJsonAsync<JobQueryResult>($"api/backgroundjobs{query}", _jsonOptions);
            return response != null
                ? Result<JobQueryResult>.Success(response)
                : Result<JobQueryResult>.Failure("Failed to get jobs", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<JobQueryResult>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<JobInfo>> GetJobByIdAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<JobInfo>($"api/backgroundjobs/{jobId}", _jsonOptions);
            return response != null
                ? Result<JobInfo>.Success(response)
                : Result<JobInfo>.NotFound("Job not found");
        }
        catch (Exception ex)
        {
            return Result<JobInfo>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<JobStatistics>> GetStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<JobStatistics>($"api/backgroundjobs/statistics", _jsonOptions);
            return response != null
                ? Result<JobStatistics>.Success(response)
                : Result<JobStatistics>.Failure("Failed to get statistics", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<JobStatistics>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> RequeueJobAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/backgroundjobs/{jobId}/requeue", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> TriggerRecurringJobAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/backgroundjobs/recurring/{jobId}/trigger", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> DeleteJobAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/backgroundjobs/{jobId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> RemoveRecurringJobAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/backgroundjobs/recurring/{jobId}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(JobFilter filter)
    {
        var query = new List<string>();

        if (filter.Status.HasValue)
            query.Add($"status={filter.Status.Value}");
        if (filter.Type.HasValue)
            query.Add($"type={filter.Type.Value}");
        if (!string.IsNullOrEmpty(filter.Queue))
            query.Add($"queue={Uri.EscapeDataString(filter.Queue)}");
        if (!string.IsNullOrEmpty(filter.NameContains))
            query.Add($"nameContains={Uri.EscapeDataString(filter.NameContains)}");
        if (filter.FromDate.HasValue)
            query.Add($"fromDate={filter.FromDate.Value:o}");
        if (filter.ToDate.HasValue)
            query.Add($"toDate={filter.ToDate.Value:o}");
        if (filter.TenantId.HasValue)
            query.Add($"tenantId={filter.TenantId.Value}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
