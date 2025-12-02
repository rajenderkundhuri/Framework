using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Framework.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for logging requests and responses
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[START] {RequestName} ({RequestId})",
            requestName,
            requestId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "[END] {RequestName} ({RequestId}) completed in {ElapsedMilliseconds}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "[ERROR] {RequestName} ({RequestId}) failed after {ElapsedMilliseconds}ms",
                requestName,
                requestId,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}

/// <summary>
/// MediatR pipeline behavior for performance monitoring
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _timer;
    private const int WarningThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new Stopwatch();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > WarningThresholdMs)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogWarning(
                "[PERFORMANCE] {RequestName} took {ElapsedMilliseconds}ms (threshold: {Threshold}ms)",
                requestName,
                elapsedMilliseconds,
                WarningThresholdMs);
        }

        return response;
    }
}
