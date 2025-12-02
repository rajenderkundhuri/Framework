using System.Net.Http.Json;
using System.Text.Json;
using Framework.Application.Common.Models;
using Framework.Application.Notifications;
using Framework.Domain.Notifications;

namespace Framework.Admin.Services;

public interface INotificationApiService
{
    Task<Result<PagedList<NotificationResponse>>> GetNotificationsAsync(NotificationListRequest request, Guid? userId = null);
    Task<Result<NotificationResponse>> GetByIdAsync(Guid id);
    Task<Result<Guid>> CreateAsync(CreateNotificationRequest request);
    Task<Result<int>> CreateBulkAsync(CreateBulkNotificationRequest request);
    Task<Result> MarkAsReadAsync(Guid id);
    Task<Result> MarkAsUnreadAsync(Guid id);
    Task<Result> MarkAllAsReadAsync(Guid userId);
    Task<Result> DeleteAsync(Guid id);
    Task<Result<int>> DeleteAllReadAsync(Guid userId);
    Task<Result<int>> GetUnreadCountAsync();
    Task<Result<PagedList<NotificationResponse>>> GetMyNotificationsAsync(NotificationListRequest request);
}

public class NotificationApiService : INotificationApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationApiService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient("API");
        _jsonOptions = jsonOptions;
    }

    public async Task<Result<PagedList<NotificationResponse>>> GetNotificationsAsync(NotificationListRequest request, Guid? userId = null)
    {
        try
        {
            var query = BuildQueryString(request);
            if (userId.HasValue)
                query += (query.Contains('?') ? "&" : "?") + $"userId={userId.Value}";

            var response = await _httpClient.GetFromJsonAsync<PagedList<NotificationResponse>>($"api/notifications{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<NotificationResponse>>.Success(response)
                : Result<PagedList<NotificationResponse>>.Failure("Failed to get notifications", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<NotificationResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<PagedList<NotificationResponse>>> GetMyNotificationsAsync(NotificationListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<NotificationResponse>>($"api/notifications/my{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<NotificationResponse>>.Success(response)
                : Result<PagedList<NotificationResponse>>.Failure("Failed to get notifications", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<NotificationResponse>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<int>("api/notifications/my/unread-count", _jsonOptions);
            return Result<int>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<NotificationResponse>> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<NotificationResponse>($"api/notifications/{id}", _jsonOptions);
            return response != null
                ? Result<NotificationResponse>.Success(response)
                : Result<NotificationResponse>.NotFound("Notification not found");
        }
        catch (Exception ex)
        {
            return Result<NotificationResponse>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<Guid>> CreateAsync(CreateNotificationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/notifications", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Guid>(_jsonOptions);
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

    public async Task<Result<int>> CreateBulkAsync(CreateBulkNotificationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/notifications/bulk", request, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<int>(_jsonOptions);
                return Result<int>.Success(result);
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<int>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> MarkAsReadAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/notifications/{id}/mark-read", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> MarkAsUnreadAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/notifications/{id}/mark-unread", null);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.PostAsync("api/notifications/my/mark-all-read", null);
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
            var response = await _httpClient.DeleteAsync($"api/notifications/{id}");
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(await response.Content.ReadAsStringAsync(), "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<int>> DeleteAllReadAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/notifications/my/read");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<int>(_jsonOptions);
                return Result<int>.Success(result);
            }
            var error = await response.Content.ReadAsStringAsync();
            return Result<int>.Failure(error, "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(NotificationListRequest request)
    {
        var query = new List<string>();

        if (request.IsRead.HasValue)
            query.Add($"isRead={request.IsRead.Value}");
        if (request.Type.HasValue)
            query.Add($"type={request.Type.Value}");
        if (request.Severity.HasValue)
            query.Add($"severity={request.Severity.Value}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");
        query.Add($"sortDescending={request.SortDescending}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
