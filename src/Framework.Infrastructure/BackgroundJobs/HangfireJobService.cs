using System.Linq.Expressions;
using Framework.Application.BackgroundJobs;
using Framework.Domain.BackgroundJobs;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-based implementation of IJobService
/// </summary>
public class HangfireJobService : IJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly JobStorage _jobStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundJobSettings _settings;

    public HangfireJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        JobStorage jobStorage,
        IServiceProvider serviceProvider,
        IOptions<BackgroundJobSettings> settings)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _jobStorage = jobStorage;
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public string Enqueue<TJob, TData>(TData data, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        var state = CreateEnqueuedState(queue);
        return _backgroundJobClient.Create<TJob>(
            job => job.ExecuteAsync(data, CancellationToken.None),
            state);
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        return _backgroundJobClient.Enqueue(methodCall);
    }

    public string Schedule<TJob, TData>(TData data, TimeSpan delay, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        return _backgroundJobClient.Schedule<TJob>(
            job => job.ExecuteAsync(data, CancellationToken.None),
            delay);
    }

    public string Schedule<TJob, TData>(TData data, DateTimeOffset enqueueAt, string? queue = null)
        where TJob : IBackgroundJob<TData>
        where TData : class
    {
        return _backgroundJobClient.Schedule<TJob>(
            job => job.ExecuteAsync(data, CancellationToken.None),
            enqueueAt);
    }

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        return _backgroundJobClient.Schedule(methodCall, delay);
    }

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
    {
        return _backgroundJobClient.Schedule(methodCall, enqueueAt);
    }

    public void AddOrUpdateRecurring<TJob>() where TJob : IRecurringJob
    {
        var job = _serviceProvider.GetRequiredService<TJob>();
        _recurringJobManager.AddOrUpdate<TJob>(
            job.JobId,
            j => j.ExecuteAsync(CancellationToken.None),
            job.CronExpression,
            new RecurringJobOptions
            {
                TimeZone = job.TimeZone,
                QueueName = job.Queue ?? "default"
            });
    }

    public void AddOrUpdateRecurring(
        string jobId,
        Expression<Action> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string? queue = null)
    {
        _recurringJobManager.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone ?? TimeZoneInfo.Utc,
                QueueName = queue ?? "default"
            });
    }

    public void AddOrUpdateRecurring(
        string jobId,
        Expression<Func<Task>> methodCall,
        string cronExpression,
        TimeZoneInfo? timeZone = null,
        string? queue = null)
    {
        _recurringJobManager.AddOrUpdate(
            jobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = timeZone ?? TimeZoneInfo.Utc,
                QueueName = queue ?? "default"
            });
    }

    public void RemoveRecurring(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
    }

    public void TriggerRecurring(string jobId)
    {
        _recurringJobManager.Trigger(jobId);
    }

    public string ContinueWith(string parentJobId, Expression<Action> methodCall)
    {
        return _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
    }

    public string ContinueWith(string parentJobId, Expression<Func<Task>> methodCall)
    {
        return _backgroundJobClient.ContinueJobWith(parentJobId, methodCall);
    }

    public bool Delete(string jobId)
    {
        return _backgroundJobClient.Delete(jobId);
    }

    public bool Requeue(string jobId)
    {
        return _backgroundJobClient.Requeue(jobId);
    }

    public JobInfo? GetJob(string jobId)
    {
        using var connection = _jobStorage.GetConnection();
        var jobData = connection.GetJobData(jobId);

        if (jobData == null)
            return null;

        return new JobInfo
        {
            Id = jobId,
            Name = jobData.Job?.Type?.Name ?? "Unknown",
            Status = MapState(jobData.State),
            CreatedAt = jobData.CreatedAt,
            JobData = jobData.Job?.ToString()
        };
    }

    public JobQueryResult GetJobs(JobFilter filter)
    {
        var api = _jobStorage.GetMonitoringApi();
        var jobs = new List<JobInfo>();
        var skip = (filter.PageNumber - 1) * filter.PageSize;

        // Get jobs based on status filter
        if (filter.Status == null || filter.Status == JobStatus.Enqueued)
        {
            var queues = api.Queues();
            foreach (var queue in queues)
            {
                if (filter.Queue != null && queue.Name != filter.Queue)
                    continue;

                var enqueuedJobs = api.EnqueuedJobs(queue.Name, skip, filter.PageSize);
                jobs.AddRange(enqueuedJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Enqueued, queue.Name)));
            }
        }

        if (filter.Status == null || filter.Status == JobStatus.Scheduled)
        {
            var scheduledJobs = api.ScheduledJobs(skip, filter.PageSize);
            jobs.AddRange(scheduledJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Scheduled)));
        }

        if (filter.Status == null || filter.Status == JobStatus.Processing)
        {
            var processingJobs = api.ProcessingJobs(skip, filter.PageSize);
            jobs.AddRange(processingJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Processing)));
        }

        if (filter.Status == null || filter.Status == JobStatus.Succeeded)
        {
            var succeededJobs = api.SucceededJobs(skip, filter.PageSize);
            jobs.AddRange(succeededJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Succeeded)));
        }

        if (filter.Status == null || filter.Status == JobStatus.Failed)
        {
            var failedJobs = api.FailedJobs(skip, filter.PageSize);
            jobs.AddRange(failedJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Failed)));
        }

        if (filter.Status == null || filter.Status == JobStatus.Deleted)
        {
            var deletedJobs = api.DeletedJobs(skip, filter.PageSize);
            jobs.AddRange(deletedJobs.Select(j => MapToJobInfo(j.Key, j.Value, JobStatus.Deleted)));
        }

        // Apply name filter
        if (!string.IsNullOrEmpty(filter.NameContains))
        {
            jobs = jobs.Where(j => j.Name.Contains(filter.NameContains, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply date filters
        if (filter.FromDate.HasValue)
        {
            jobs = jobs.Where(j => j.CreatedAt >= filter.FromDate.Value).ToList();
        }

        if (filter.ToDate.HasValue)
        {
            jobs = jobs.Where(j => j.CreatedAt <= filter.ToDate.Value).ToList();
        }

        var stats = api.GetStatistics();

        return new JobQueryResult
        {
            Jobs = jobs.Take(filter.PageSize).ToList(),
            TotalCount = (int)(stats.Enqueued + stats.Scheduled + stats.Processing + stats.Succeeded + stats.Failed + stats.Deleted),
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public JobStatistics GetStatistics()
    {
        var api = _jobStorage.GetMonitoringApi();
        var stats = api.GetStatistics();
        var queues = api.Queues();

        return new JobStatistics
        {
            EnqueuedCount = stats.Enqueued,
            ScheduledCount = stats.Scheduled,
            ProcessingCount = stats.Processing,
            SucceededCount = stats.Succeeded,
            FailedCount = stats.Failed,
            DeletedCount = stats.Deleted,
            RecurringCount = stats.Recurring,
            ServerCount = (int)stats.Servers,
            QueueCounts = queues.ToDictionary(q => q.Name, q => q.Length)
        };
    }

    private static IState CreateEnqueuedState(string? queue)
    {
        return string.IsNullOrEmpty(queue)
            ? new EnqueuedState()
            : new EnqueuedState(queue);
    }

    private static JobStatus MapState(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "enqueued" => JobStatus.Enqueued,
            "scheduled" => JobStatus.Scheduled,
            "processing" => JobStatus.Processing,
            "succeeded" => JobStatus.Succeeded,
            "failed" => JobStatus.Failed,
            "deleted" => JobStatus.Deleted,
            "awaiting" => JobStatus.Awaiting,
            _ => JobStatus.Enqueued
        };
    }

    private static JobInfo MapToJobInfo(string id, EnqueuedJobDto dto, JobStatus status, string? queue = null)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Queue = queue,
            Status = status,
            CreatedAt = dto.EnqueuedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static JobInfo MapToJobInfo(string id, ScheduledJobDto dto, JobStatus status)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Status = status,
            CreatedAt = dto.EnqueueAt,
            ScheduledAt = dto.ScheduledAt
        };
    }

    private static JobInfo MapToJobInfo(string id, ProcessingJobDto dto, JobStatus status)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Status = status,
            StartedAt = dto.StartedAt
        };
    }

    private static JobInfo MapToJobInfo(string id, SucceededJobDto dto, JobStatus status)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Status = status,
            CompletedAt = dto.SucceededAt,
            DurationMs = dto.TotalDuration.HasValue ? (long)dto.TotalDuration.Value : null
        };
    }

    private static JobInfo MapToJobInfo(string id, FailedJobDto dto, JobStatus status)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Status = status,
            CompletedAt = dto.FailedAt,
            ErrorMessage = dto.ExceptionMessage
        };
    }

    private static JobInfo MapToJobInfo(string id, DeletedJobDto dto, JobStatus status)
    {
        return new JobInfo
        {
            Id = id,
            Name = dto.Job?.Type?.Name ?? "Unknown",
            Status = status,
            CompletedAt = dto.DeletedAt
        };
    }
}
