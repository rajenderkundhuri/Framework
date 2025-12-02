using System.Diagnostics;
using Framework.Application.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.HealthChecks;

/// <summary>
/// Service for running health checks
/// </summary>
public class HealthCheckService
{
    private readonly IEnumerable<IHealthCheck> _healthChecks;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly HealthCheckSettings _settings;

    public HealthCheckService(
        IEnumerable<IHealthCheck> healthChecks,
        ILogger<HealthCheckService> logger,
        IOptions<HealthCheckSettings> settings)
    {
        _healthChecks = healthChecks;
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Runs all health checks
    /// </summary>
    public async Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var report = new HealthReport();

        var checksToRun = _settings.Tags.Any()
            ? _healthChecks.Where(c => c.Tags.Intersect(_settings.Tags).Any())
            : _healthChecks;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        foreach (var check in checksToRun)
        {
            var checkSw = Stopwatch.StartNew();
            HealthCheckResult result;

            try
            {
                result = await check.CheckAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                result = HealthCheckResult.Unhealthy("Health check timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check {Name} threw an exception", check.Name);
                result = HealthCheckResult.Unhealthy("Health check threw an exception", ex);
            }

            checkSw.Stop();
            result.Duration = checkSw.Elapsed;
            report.Entries[check.Name] = result;
        }

        sw.Stop();
        report.TotalDuration = sw.Elapsed;
        report.Status = DetermineOverallStatus(report.Entries.Values);

        return report;
    }

    private static HealthStatus DetermineOverallStatus(IEnumerable<HealthCheckResult> results)
    {
        var statuses = results.Select(r => r.Status).ToList();

        if (statuses.Any(s => s == HealthStatus.Unhealthy))
        {
            return HealthStatus.Unhealthy;
        }

        if (statuses.Any(s => s == HealthStatus.Degraded))
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}

/// <summary>
/// Base class for health checks
/// </summary>
public abstract class BaseHealthCheck : IHealthCheck
{
    public abstract string Name { get; }

    public virtual IEnumerable<string> Tags => Enumerable.Empty<string>();

    public abstract Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Database health check
/// </summary>
public class DatabaseHealthCheck : BaseHealthCheck
{
    private readonly Func<CancellationToken, Task<bool>> _checkConnection;

    public override string Name => "Database";
    public override IEnumerable<string> Tags => new[] { "db", "critical" };

    public DatabaseHealthCheck(Func<CancellationToken, Task<bool>> checkConnection)
    {
        _checkConnection = checkConnection;
    }

    public override async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _checkConnection(cancellationToken);
            return isHealthy
                ? HealthCheckResult.Healthy("Database connection successful")
                : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection error", ex);
        }
    }
}

/// <summary>
/// Memory health check
/// </summary>
public class MemoryHealthCheck : BaseHealthCheck
{
    private readonly long _thresholdBytes;

    public override string Name => "Memory";
    public override IEnumerable<string> Tags => new[] { "memory" };

    public MemoryHealthCheck(long thresholdBytes = 1024 * 1024 * 1024) // 1GB default
    {
        _thresholdBytes = thresholdBytes;
    }

    public override Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(false);

        var result = new HealthCheckResult
        {
            Data = new Dictionary<string, object>
            {
                ["AllocatedBytes"] = allocatedBytes,
                ["ThresholdBytes"] = _thresholdBytes,
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            }
        };

        if (allocatedBytes > _thresholdBytes)
        {
            result.Status = HealthStatus.Degraded;
            result.Description = $"Memory usage ({allocatedBytes:N0} bytes) exceeds threshold ({_thresholdBytes:N0} bytes)";
        }
        else
        {
            result.Status = HealthStatus.Healthy;
            result.Description = $"Memory usage: {allocatedBytes:N0} bytes";
        }

        return Task.FromResult(result);
    }
}

/// <summary>
/// URL health check
/// </summary>
public class UrlHealthCheck : BaseHealthCheck
{
    private readonly string _url;
    private readonly HttpClient _httpClient;

    public override string Name { get; }
    public override IEnumerable<string> Tags => new[] { "url", "external" };

    public UrlHealthCheck(string name, string url, HttpClient? httpClient = null)
    {
        Name = name;
        _url = url;
        _httpClient = httpClient ?? new HttpClient();
    }

    public override async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"URL {_url} returned {(int)response.StatusCode}");
            }

            return HealthCheckResult.Unhealthy($"URL {_url} returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"URL {_url} is unreachable", ex);
        }
    }
}
