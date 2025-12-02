using System.Reflection;
using Framework.Application.Caching;
using Framework.Application.HealthChecks;
using Framework.Application.Identity;
using Framework.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// System administration endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class SystemController : ApiControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ICacheService _cacheService;

    public SystemController(
        HealthCheckService healthCheckService,
        ICacheService cacheService)
    {
        _healthCheckService = healthCheckService;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get system information
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public IActionResult GetSystemInfo()
    {
        if (!HasPermission(Permissions.SystemSettings))
            return Forbid();

        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        var info = new SystemInfoResponse
        {
            ApplicationName = "Framework API",
            Version = version,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            StartTime = System.Diagnostics.Process.GetCurrentProcess().StartTime,
            WorkingSet = Environment.WorkingSet,
            DotNetVersion = Environment.Version.ToString()
        };

        return Ok(info);
    }

    /// <summary>
    /// Get health status
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthReportResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthStatus(CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemHealthCheck))
            return Forbid();

        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        var response = new HealthReportResponse
        {
            Status = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Timestamp = report.Timestamp,
            Entries = report.Entries.Select(e => new HealthCheckEntryResponse
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                Data = e.Value.Data
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Clear all caches
    /// </summary>
    [HttpPost("cache/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCache(CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemSettings))
            return Forbid();

        await _cacheService.ClearAsync(cancellationToken);
        return Ok(new { Message = "Cache cleared successfully" });
    }

    /// <summary>
    /// Clear cache by pattern
    /// </summary>
    [HttpPost("cache/clear/{pattern}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCacheByPattern(string pattern, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.SystemSettings))
            return Forbid();

        await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
        return Ok(new { Message = $"Cache entries matching '{pattern}' cleared successfully" });
    }

    /// <summary>
    /// Force garbage collection
    /// </summary>
    [HttpPost("gc")]
    [ProducesResponseType(typeof(GcInfoResponse), StatusCodes.Status200OK)]
    public IActionResult ForceGarbageCollection()
    {
        if (!HasPermission(Permissions.SystemSettings))
            return Forbid();

        var beforeMemory = GC.GetTotalMemory(false);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var afterMemory = GC.GetTotalMemory(true);

        return Ok(new GcInfoResponse
        {
            BeforeBytes = beforeMemory,
            AfterBytes = afterMemory,
            FreedBytes = beforeMemory - afterMemory,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        });
    }
}

/// <summary>
/// System information response
/// </summary>
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

/// <summary>
/// Health report response
/// </summary>
public record HealthReportResponse
{
    public string Status { get; init; } = string.Empty;
    public TimeSpan TotalDuration { get; init; }
    public DateTime Timestamp { get; init; }
    public List<HealthCheckEntryResponse> Entries { get; init; } = new();
}

/// <summary>
/// Health check entry response
/// </summary>
public record HealthCheckEntryResponse
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TimeSpan Duration { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// GC info response
/// </summary>
public record GcInfoResponse
{
    public long BeforeBytes { get; init; }
    public long AfterBytes { get; init; }
    public long FreedBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}
