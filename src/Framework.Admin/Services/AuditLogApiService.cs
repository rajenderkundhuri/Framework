using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Framework.Application.Auditing;
using Framework.Application.Common.Models;
using Framework.Domain.Auditing;

namespace Framework.Admin.Services;

public interface IAuditLogApiService
{
    Task<Result<PagedList<AuditLogDto>>> GetLogsAsync(AuditLogListRequest request);
    Task<Result<IEnumerable<AuditActionInfo>>> GetActionsAsync();
    Task<Result<IEnumerable<string>>> GetEntityTypesAsync();
}

public record AuditLogListRequest
{
    public Guid? TenantId { get; init; }
    public string? UserId { get; init; }
    public AuditAction? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public bool? IsSuccess { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record AuditActionInfo
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class AuditLogApiService : IAuditLogApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuditLogApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("API");
    }

    public async Task<Result<PagedList<AuditLogDto>>> GetLogsAsync(AuditLogListRequest request)
    {
        try
        {
            var query = BuildQueryString(request);
            var response = await _httpClient.GetFromJsonAsync<PagedList<AuditLogDto>>($"api/auditlogs{query}", _jsonOptions);
            return response != null
                ? Result<PagedList<AuditLogDto>>.Success(response)
                : Result<PagedList<AuditLogDto>>.Failure("Failed to get audit logs", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<PagedList<AuditLogDto>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<AuditActionInfo>>> GetActionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<AuditActionInfo>>("api/auditlogs/actions");
            return response != null
                ? Result<IEnumerable<AuditActionInfo>>.Success(response)
                : Result<IEnumerable<AuditActionInfo>>.Failure("Failed to get actions", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<AuditActionInfo>>.Failure(ex.Message, "API_ERROR");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetEntityTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<string>>("api/auditlogs/entity-types");
            return response != null
                ? Result<IEnumerable<string>>.Success(response)
                : Result<IEnumerable<string>>.Failure("Failed to get entity types", "API_ERROR");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<string>>.Failure(ex.Message, "API_ERROR");
        }
    }

    private static string BuildQueryString(AuditLogListRequest request)
    {
        var query = new List<string>();

        if (request.TenantId.HasValue)
            query.Add($"tenantId={request.TenantId}");
        if (!string.IsNullOrEmpty(request.UserId))
            query.Add($"userId={Uri.EscapeDataString(request.UserId)}");
        if (request.Action.HasValue)
            query.Add($"action={(int)request.Action.Value}");
        if (!string.IsNullOrEmpty(request.EntityType))
            query.Add($"entityType={Uri.EscapeDataString(request.EntityType)}");
        if (request.FromDate.HasValue)
            query.Add($"fromDate={Uri.EscapeDataString(request.FromDate.Value.ToString("O"))}");
        if (request.ToDate.HasValue)
            query.Add($"toDate={Uri.EscapeDataString(request.ToDate.Value.ToString("O"))}");
        if (request.IsSuccess.HasValue)
            query.Add($"isSuccess={request.IsSuccess.Value}");
        if (!string.IsNullOrEmpty(request.SearchTerm))
            query.Add($"searchTerm={Uri.EscapeDataString(request.SearchTerm)}");
        query.Add($"pageNumber={request.PageNumber}");
        query.Add($"pageSize={request.PageSize}");

        return query.Count > 0 ? "?" + string.Join("&", query) : "";
    }
}
