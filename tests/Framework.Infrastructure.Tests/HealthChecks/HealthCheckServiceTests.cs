using Framework.Application.HealthChecks;
using Framework.Infrastructure.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.HealthChecks;

public class HealthCheckServiceTests
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IOptions<HealthCheckSettings> _options;

    public HealthCheckServiceTests()
    {
        _logger = Substitute.For<ILogger<HealthCheckService>>();
        _options = Options.Create(new HealthCheckSettings());
    }

    private HealthCheckService CreateService(params IHealthCheck[] checks)
    {
        return new HealthCheckService(checks, _logger, _options);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoChecks_ShouldReturnHealthy()
    {
        // Arrange
        var service = CreateService();

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Healthy);
        report.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthyCheck_ShouldReturnHealthy()
    {
        // Arrange
        var healthyCheck = new TestHealthCheck("test", HealthCheckResult.Healthy("All good"));
        var service = CreateService(healthyCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Healthy);
        report.Entries.ShouldContainKey("test");
        report.Entries["test"].Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnhealthyCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var unhealthyCheck = new TestHealthCheck("db", HealthCheckResult.Unhealthy("Connection failed"));
        var service = CreateService(unhealthyCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries["db"].Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithDegradedCheck_ShouldReturnDegraded()
    {
        // Arrange
        var degradedCheck = new TestHealthCheck("cache", HealthCheckResult.Degraded("Slow response"));
        var service = CreateService(degradedCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Degraded);
        report.Entries["cache"].Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WithMixedChecks_ShouldReturnWorstStatus()
    {
        // Arrange
        var healthyCheck = new TestHealthCheck("api", HealthCheckResult.Healthy());
        var degradedCheck = new TestHealthCheck("cache", HealthCheckResult.Degraded("Slow"));
        var service = CreateService(healthyCheck, degradedCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Degraded);
        report.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnhealthyAndDegraded_ShouldReturnUnhealthy()
    {
        // Arrange
        var degradedCheck = new TestHealthCheck("cache", HealthCheckResult.Degraded("Slow"));
        var unhealthyCheck = new TestHealthCheck("db", HealthCheckResult.Unhealthy("Down"));
        var service = CreateService(degradedCheck, unhealthyCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithThrowingCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var throwingCheck = new ThrowingHealthCheck("failing");
        var service = CreateService(throwingCheck);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries["failing"].Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries["failing"].Exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WithTagsInSettings_ShouldFilterChecks()
    {
        // Arrange
        var options = Options.Create(new HealthCheckSettings { Tags = new List<string> { "database" } });
        var dbCheck = new TaggedHealthCheck("db", new[] { "database" });
        var cacheCheck = new TaggedHealthCheck("cache", new[] { "cache" });
        var service = new HealthCheckService(new IHealthCheck[] { dbCheck, cacheCheck }, _logger, options);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Entries.ShouldContainKey("db");
        report.Entries.ShouldNotContainKey("cache");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldSetTotalDuration()
    {
        // Arrange
        var check = new TestHealthCheck("test", HealthCheckResult.Healthy());
        var service = CreateService(check);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.TotalDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldSetTimestamp()
    {
        // Arrange
        var check = new TestHealthCheck("test", HealthCheckResult.Healthy());
        var service = CreateService(check);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Timestamp.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public async Task CheckHealthAsync_WithTimeout_ShouldTimeoutSlowChecks()
    {
        // Arrange
        var options = Options.Create(new HealthCheckSettings { TimeoutSeconds = 1 });
        var slowCheck = new SlowHealthCheck("slow", TimeSpan.FromSeconds(10));
        var service = new HealthCheckService(new[] { slowCheck }, _logger, options);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Entries["slow"].Status.ShouldBe(HealthStatus.Unhealthy);
        report.Entries["slow"].Description.ShouldContain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleChecks_ShouldRunAll()
    {
        // Arrange
        var check1 = new TestHealthCheck("check1", HealthCheckResult.Healthy());
        var check2 = new TestHealthCheck("check2", HealthCheckResult.Healthy());
        var check3 = new TestHealthCheck("check3", HealthCheckResult.Healthy());
        var service = CreateService(check1, check2, check3);

        // Act
        var report = await service.CheckHealthAsync();

        // Assert
        report.Entries.Count.ShouldBe(3);
        report.Status.ShouldBe(HealthStatus.Healthy);
    }

    private class TestHealthCheck : IHealthCheck
    {
        private readonly HealthCheckResult _result;

        public TestHealthCheck(string name, HealthCheckResult result)
        {
            Name = name;
            _result = result;
        }

        public string Name { get; }
        public IEnumerable<string> Tags => Enumerable.Empty<string>();

        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private class TaggedHealthCheck : IHealthCheck
    {
        public TaggedHealthCheck(string name, IEnumerable<string> tags)
        {
            Name = name;
            Tags = tags;
        }

        public string Name { get; }
        public IEnumerable<string> Tags { get; }

        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(HealthCheckResult.Healthy());
    }

    private class ThrowingHealthCheck : IHealthCheck
    {
        public ThrowingHealthCheck(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public IEnumerable<string> Tags => Enumerable.Empty<string>();

        public Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Health check failed");
    }

    private class SlowHealthCheck : IHealthCheck
    {
        private readonly TimeSpan _delay;

        public SlowHealthCheck(string name, TimeSpan delay)
        {
            Name = name;
            _delay = delay;
        }

        public string Name { get; }
        public IEnumerable<string> Tags => Enumerable.Empty<string>();

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken);
            return HealthCheckResult.Healthy();
        }
    }
}
