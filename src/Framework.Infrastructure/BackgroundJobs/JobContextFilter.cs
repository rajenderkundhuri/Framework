using Framework.Application.BackgroundJobs;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.MultiTenancy;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire filter that captures and restores job context (tenant, user, etc.)
/// </summary>
public class JobContextFilter : IClientFilter, IServerFilter, IElectStateFilter
{
    private const string TenantIdKey = "TenantId";
    private const string UserIdKey = "UserId";
    private const string UserNameKey = "UserName";
    private const string CorrelationIdKey = "CorrelationId";
    private const string CultureKey = "Culture";

    private readonly IServiceProvider _serviceProvider;

    public JobContextFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Called when job is being created
    public void OnCreating(CreatingContext filterContext)
    {
        // Capture current context when job is created
        using var scope = _serviceProvider.CreateScope();

        var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUser>();

        if (tenantContext?.TenantId != null)
        {
            filterContext.SetJobParameter(TenantIdKey, tenantContext.TenantId.Value.ToString());
        }

        if (currentUser != null)
        {
            if (!string.IsNullOrEmpty(currentUser.UserId))
                filterContext.SetJobParameter(UserIdKey, currentUser.UserId);

            if (!string.IsNullOrEmpty(currentUser.UserName))
                filterContext.SetJobParameter(UserNameKey, currentUser.UserName);
        }

        filterContext.SetJobParameter(CorrelationIdKey, Guid.NewGuid().ToString("N"));
        filterContext.SetJobParameter(CultureKey, Thread.CurrentThread.CurrentCulture.Name);
    }

    public void OnCreated(CreatedContext filterContext)
    {
        // No action needed
    }

    // Called when job is being executed on server
    public void OnPerforming(PerformingContext filterContext)
    {
        // Restore context when job is executing
        using var scope = _serviceProvider.CreateScope();

        var tenantIdStr = filterContext.GetJobParameter<string>(TenantIdKey);
        var userId = filterContext.GetJobParameter<string>(UserIdKey);
        var userName = filterContext.GetJobParameter<string>(UserNameKey);
        var correlationId = filterContext.GetJobParameter<string>(CorrelationIdKey);
        var culture = filterContext.GetJobParameter<string>(CultureKey);

        // Set tenant context
        if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tenantId))
        {
            var tenantContext = scope.ServiceProvider.GetService<TenantContext>();
            tenantContext?.SetTenantId(tenantId);
        }

        // Set job context accessor
        var jobContextAccessor = scope.ServiceProvider.GetService<IJobContextAccessor>();
        if (jobContextAccessor != null)
        {
            jobContextAccessor.CurrentContext = new JobContext
            {
                TenantId = !string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var tid) ? tid : null,
                UserId = userId,
                UserName = userName,
                CorrelationId = correlationId,
                Culture = culture
            };
        }

        // Set culture
        if (!string.IsNullOrEmpty(culture))
        {
            try
            {
                var cultureInfo = new System.Globalization.CultureInfo(culture);
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
            }
            catch
            {
                // Ignore invalid culture
            }
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        // Clean up context after job execution
        using var scope = _serviceProvider.CreateScope();

        var jobContextAccessor = scope.ServiceProvider.GetService<IJobContextAccessor>();
        if (jobContextAccessor != null)
        {
            jobContextAccessor.CurrentContext = null;
        }
    }

    public void OnStateElection(ElectStateContext context)
    {
        // No action needed for state election
    }
}

/// <summary>
/// Hangfire filter that handles job retries with exponential backoff
/// </summary>
public class AutomaticRetryFilter : IElectStateFilter
{
    private readonly int[] _retryDelaysInSeconds;
    private readonly int _maxRetries;

    public AutomaticRetryFilter(int maxRetries = 3, int[]? retryDelaysInSeconds = null)
    {
        _maxRetries = maxRetries;
        _retryDelaysInSeconds = retryDelaysInSeconds ?? new[] { 30, 60, 300, 900, 3600 };
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is FailedState failedState)
        {
            var retryCount = context.GetJobParameter<int>("RetryCount");

            if (retryCount < _maxRetries)
            {
                // Calculate delay for next retry
                var delayIndex = Math.Min(retryCount, _retryDelaysInSeconds.Length - 1);
                var delay = TimeSpan.FromSeconds(_retryDelaysInSeconds[delayIndex]);

                context.SetJobParameter("RetryCount", retryCount + 1);
                context.CandidateState = new ScheduledState(delay)
                {
                    Reason = $"Retry #{retryCount + 1} after {delay.TotalSeconds} seconds"
                };
            }
        }
    }
}

/// <summary>
/// Attribute to configure job-specific retry behavior
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JobRetryAttribute : JobFilterAttribute, IElectStateFilter
{
    public int MaxRetries { get; set; } = 3;
    public int[] RetryDelaysInSeconds { get; set; } = { 30, 60, 300 };

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is FailedState)
        {
            var retryCount = context.GetJobParameter<int>("RetryCount");

            if (retryCount < MaxRetries)
            {
                var delayIndex = Math.Min(retryCount, RetryDelaysInSeconds.Length - 1);
                var delay = TimeSpan.FromSeconds(RetryDelaysInSeconds[delayIndex]);

                context.SetJobParameter("RetryCount", retryCount + 1);
                context.CandidateState = new ScheduledState(delay);
            }
        }
    }
}
