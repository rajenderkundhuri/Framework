using Framework.Application.Outbox;
using Framework.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Outbox;

public class InMemoryOutboxServiceTests
{
    private readonly ILogger<InMemoryOutboxService> _logger;
    private readonly IOptions<OutboxSettings> _options;

    public InMemoryOutboxServiceTests()
    {
        _logger = Substitute.For<ILogger<InMemoryOutboxService>>();
        _options = Options.Create(new OutboxSettings
        {
            BatchSize = 10,
            MaxRetries = 3
        });
    }

    private InMemoryOutboxService CreateService() => new(_logger, _options);

    [Fact]
    public async Task AddAsync_ShouldStoreMessage()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 42 });

        // Act
        await service.AddAsync(message);

        // Assert
        service.Count.ShouldBe(1);
        service.GetAll().ShouldContain(m => m.Id == message.Id);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldStoreMultipleMessages()
    {
        // Arrange
        var service = CreateService();
        var messages = new[]
        {
            OutboxMessage.Create(new TestPayload { Value = 1 }),
            OutboxMessage.Create(new TestPayload { Value = 2 }),
            OutboxMessage.Create(new TestPayload { Value = 3 })
        };

        // Act
        await service.AddRangeAsync(messages);

        // Assert
        service.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOnlyPending()
    {
        // Arrange
        var service = CreateService();
        var pending1 = OutboxMessage.Create(new TestPayload { Value = 1 });
        var pending2 = OutboxMessage.Create(new TestPayload { Value = 2 });

        await service.AddAsync(pending1);
        await service.AddAsync(pending2);
        await service.MarkAsProcessedAsync(pending1.Id);

        // Act - need to get pending first, then add more to see only pending
        service.Clear();
        var newPending = OutboxMessage.Create(new TestPayload { Value = 3 });
        var newProcessed = OutboxMessage.Create(new TestPayload { Value = 4 });
        await service.AddAsync(newPending);
        await service.AddAsync(newProcessed);
        await service.MarkAsProcessedAsync(newProcessed.Id);

        var pendingMessages = await service.GetPendingAsync(10);

        // Assert - GetPendingAsync marks messages as Processing
        pendingMessages.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldRespectBatchSize()
    {
        // Arrange
        var service = CreateService();
        for (int i = 0; i < 15; i++)
        {
            await service.AddAsync(OutboxMessage.Create(new TestPayload { Value = i }));
        }

        // Act
        var pending = await service.GetPendingAsync(5);

        // Assert
        pending.Count().ShouldBe(5);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldMarkAsProcessing()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Act
        var pending = await service.GetPendingAsync(10);

        // Assert
        pending.First().Status.ShouldBe(OutboxMessageStatus.Processing);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_ShouldUpdateStatus()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Act
        await service.MarkAsProcessedAsync(message.Id);
        var storedMessage = service.GetAll().First(m => m.Id == message.Id);

        // Assert
        storedMessage.Status.ShouldBe(OutboxMessageStatus.Processed);
        storedMessage.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldUpdateStatusAndError()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Act
        await service.MarkAsFailedAsync(message.Id, "Something went wrong");
        var storedMessage = service.GetAll().First(m => m.Id == message.Id);

        // Assert
        storedMessage.Status.ShouldBe(OutboxMessageStatus.Failed);
        storedMessage.Error.ShouldBe("Something went wrong");
        storedMessage.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldIncrementRetryCount()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Act
        await service.MarkAsFailedAsync(message.Id, "Error 1");
        await service.MarkAsFailedAsync(message.Id, "Error 2");
        var storedMessage = service.GetAll().First(m => m.Id == message.Id);

        // Assert
        storedMessage.RetryCount.ShouldBe(2);
    }

    [Fact]
    public async Task RetryFailedAsync_ShouldResetStatusForEligibleMessages()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);
        await service.MarkAsFailedAsync(message.Id, "Error");

        // Act
        var retriedCount = await service.RetryFailedAsync(3);
        var storedMessage = service.GetAll().First(m => m.Id == message.Id);

        // Assert
        retriedCount.ShouldBe(1);
        storedMessage.Status.ShouldBe(OutboxMessageStatus.Pending);
    }

    [Fact]
    public async Task RetryFailedAsync_ShouldNotRetryIfMaxRetriesExceeded()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Fail 3 times
        await service.MarkAsFailedAsync(message.Id, "Error 1");
        await service.MarkAsFailedAsync(message.Id, "Error 2");
        await service.MarkAsFailedAsync(message.Id, "Error 3");

        // Act
        var retriedCount = await service.RetryFailedAsync(3);
        var storedMessage = service.GetAll().First(m => m.Id == message.Id);

        // Assert
        retriedCount.ShouldBe(0);
        storedMessage.Status.ShouldBe(OutboxMessageStatus.Failed);
    }

    [Fact]
    public async Task CleanupAsync_ShouldRemoveOldProcessed()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);
        await service.MarkAsProcessedAsync(message.Id);

        // Act
        var deletedCount = await service.CleanupAsync(TimeSpan.Zero);

        // Assert
        deletedCount.ShouldBe(1);
        service.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CleanupAsync_ShouldNotRemovePending()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);

        // Act
        var deletedCount = await service.CleanupAsync(TimeSpan.Zero);

        // Assert
        deletedCount.ShouldBe(0);
        service.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CleanupAsync_ShouldRespectOlderThanParameter()
    {
        // Arrange
        var service = CreateService();
        var message = OutboxMessage.Create(new TestPayload { Value = 1 });
        await service.AddAsync(message);
        await service.MarkAsProcessedAsync(message.Id);

        // Act - message was just processed, so 1 hour threshold shouldn't remove it
        var deletedCount = await service.CleanupAsync(TimeSpan.FromHours(1));

        // Assert
        deletedCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnOldestFirst()
    {
        // Arrange
        var service = CreateService();
        var first = OutboxMessage.Create(new TestPayload { Value = 1 });
        first.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        var second = OutboxMessage.Create(new TestPayload { Value = 2 });
        second.CreatedAt = DateTime.UtcNow;

        await service.AddAsync(second);
        await service.AddAsync(first);

        // Act
        var pending = (await service.GetPendingAsync(10)).ToList();

        // Assert
        pending[0].CreatedAt.ShouldBeLessThanOrEqualTo(pending[1].CreatedAt);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        var service = CreateService();
        await service.AddAsync(OutboxMessage.Create(new TestPayload { Value = 1 }));
        await service.AddAsync(OutboxMessage.Create(new TestPayload { Value = 2 }));

        // Act
        service.Clear();

        // Assert
        service.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var service = CreateService();
        var tasks = new List<Task>();

        // Act - concurrent adds
        for (int i = 0; i < 100; i++)
        {
            var message = OutboxMessage.Create(new TestPayload { Value = i });
            tasks.Add(service.AddAsync(message));
        }
        await Task.WhenAll(tasks);

        // Assert
        service.Count.ShouldBe(100);
    }

    [Fact]
    public void OutboxMessage_Create_ShouldSerializePayload()
    {
        // Arrange
        var payload = new TestPayload { Value = 42 };

        // Act
        var message = OutboxMessage.Create(payload, "corr-123", Guid.NewGuid());

        // Assert
        message.Type.ShouldContain("TestPayload");
        message.Payload.ShouldContain("42");
        message.CorrelationId.ShouldBe("corr-123");
        message.TenantId.ShouldNotBeNull();
    }

    [Fact]
    public void OutboxMessage_GetPayload_ShouldDeserialize()
    {
        // Arrange
        var original = new TestPayload { Value = 42 };
        var message = OutboxMessage.Create(original);

        // Act
        var deserialized = message.GetPayload<TestPayload>();

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized!.Value.ShouldBe(42);
    }

    private class TestPayload
    {
        public int Value { get; set; }
    }
}
