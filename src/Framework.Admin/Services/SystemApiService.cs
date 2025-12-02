using System.Net.Http.Json;
using Framework.Application.Common.Models;

namespace Framework.Admin.Services;

public interface ISystemApiService
{
    Task<Result<SystemInfoResponse>> GetSystemInfoAsync();
    Task<Result<HealthReportResponse>> GetHealthStatusAsync();
    Task<Result> ClearCacheAsync();
    Task<Result> ClearCacheByPatternAsync(string pattern);
    Task<Result<GcInfoResponse>> ForceGarbageCollectionAsync();
}

public class SystemApiService : ISystemApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SystemApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<SystemInfoResponse>> GetSystemInfoAsync()
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.GetAsync("api/system/info");

        if (response.IsSuccessStatusCode)
        {
            var info = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
            return Result<SystemInfoResponse>.Success(info!);
        }

        return Result<SystemInfoResponse>.Failure("Failed to get system info");
    }

    public async Task<Result<HealthReportResponse>> GetHealthStatusAsync()
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.GetAsync("api/system/health");

        if (response.IsSuccessStatusCode)
        {
            var report = await response.Content.ReadFromJsonAsync<HealthReportResponse>();
            return Result<HealthReportResponse>.Success(report!);
        }

        return Result<HealthReportResponse>.Failure("Failed to get health status");
    }

    public async Task<Result> ClearCacheAsync()
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.PostAsync("api/system/cache/clear", null);

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return Result.Failure("Failed to clear cache");
    }

    public async Task<Result> ClearCacheByPatternAsync(string pattern)
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.PostAsync($"api/system/cache/clear/{Uri.EscapeDataString(pattern)}", null);

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return Result.Failure("Failed to clear cache");
    }

    public async Task<Result<GcInfoResponse>> ForceGarbageCollectionAsync()
    {
        var client = _httpClientFactory.CreateClient("API");
        var response = await client.PostAsync("api/system/gc", null);

        if (response.IsSuccessStatusCode)
        {
            var info = await response.Content.ReadFromJsonAsync<GcInfoResponse>();
            return Result<GcInfoResponse>.Success(info!);
        }

        return Result<GcInfoResponse>.Failure("Failed to force garbage collection");
    }
}

public record SystemInfoResponse
{
    public string ApplicationName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string OsVersion { get; init; } = string.Empty;
    public int ProcessorCount { get; init; }
    public DateTime StartTime { get; init; }
    public long WorkingSet { get; init; }
    public string DotNetVersion { get; init; } = string.Empty;
}

public record HealthReportResponse
{
    public string Status { get; init; } = string.Empty;
    public TimeSpan TotalDuration { get; init; }
    public DateTime Timestamp { get; init; }
    public List<HealthCheckEntryResponse> Entries { get; init; } = new();
}

public record HealthCheckEntryResponse
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TimeSpan Duration { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
}

public record GcInfoResponse
{
    public long BeforeBytes { get; init; }
    public long AfterBytes { get; init; }
    public long FreedBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}
