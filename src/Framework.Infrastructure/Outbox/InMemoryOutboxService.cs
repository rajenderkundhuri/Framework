using System.Collections.Concurrent;
using Framework.Application.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Outbox;

/// <summary>
/// In-memory outbox service for development/testing
/// </summary>
public class InMemoryOutboxService : IOutboxService
{
    private readonly ConcurrentDictionary<Guid, OutboxMessage> _messages = new();
    private readonly ILogger<InMemoryOutboxService> _logger;
    private readonly OutboxSettings _settings;

    public InMemoryOutboxService(
        ILogger<InMemoryOutboxService> logger,
        IOptions<OutboxSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        _logger.LogDebug("Added outbox message {MessageId} of type {Type}", message.Id, message.Type);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            _messages[message.Id] = message;
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var pending = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();

        // Mark as processing
        foreach (var message in pending)
        {
            message.Status = OutboxMessageStatus.Processing;
        }

        return Task.FromResult<IEnumerable<OutboxMessage>>(pending);
    }

    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAt = DateTime.UtcNow;
            _logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
        }
        return Task.CompletedTask;
    }

    public Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(messageId, out var message))
        {
            message.Status = OutboxMessageStatus.Failed;
            message.Error = error;
            message.RetryCount++;
            _logger.LogWarning("Marked outbox message {MessageId} as failed: {Error}", messageId, error);
        }
        return Task.CompletedTask;
    }

    public Task<int> RetryFailedAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        var failed = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Failed && m.RetryCount < maxRetries)
            .ToList();

        foreach (var message in failed)
        {
            message.Status = OutboxMessageStatus.Pending;
        }

        return Task.FromResult(failed.Count);
    }

    public Task<int> CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var toRemove = _messages.Values
            .Where(m => m.Status == OutboxMessageStatus.Processed && m.ProcessedAt < cutoff)
            .ToList();

        var count = 0;
        foreach (var message in toRemove)
        {
            if (_messages.TryRemove(message.Id, out _))
            {
                count++;
            }
        }

        _logger.LogInformation("Cleaned up {Count} outbox messages older than {OlderThan}", count, olderThan);
        return Task.FromResult(count);
    }

    /// <summary>
    /// Gets message count (for testing)
    /// </summary>
    public int Count => _messages.Count;

    /// <summary>
    /// Gets all messages (for testing)
    /// </summary>
    public IEnumerable<OutboxMessage> GetAll() => _messages.Values;

    /// <summary>
    /// Clears all messages (for testing)
    /// </summary>
    public void Clear() => _messages.Clear();
}
