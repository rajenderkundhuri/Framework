using Framework.Application.Outbox;
using Shouldly;

namespace Framework.Application.Tests.Outbox;

public class OutboxMessageTests
{
    [Fact]
    public void NewMessage_ShouldHaveDefaults()
    {
        // Act
        var message = new OutboxMessage();

        // Assert
        message.Id.ShouldNotBe(Guid.Empty);
        message.Type.ShouldBe(string.Empty);
        message.Payload.ShouldBe(string.Empty);
        message.Status.ShouldBe(OutboxMessageStatus.Pending);
        message.RetryCount.ShouldBe(0);
        message.ProcessedAt.ShouldBeNull();
        message.Error.ShouldBeNull();
        message.CorrelationId.ShouldBeNull();
        message.TenantId.ShouldBeNull();
        message.Metadata.ShouldNotBeNull();
        message.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldCreateMessageFromPayload()
    {
        // Arrange
        var payload = new TestPayload { Name = "Test", Value = 42 };

        // Act
        var message = OutboxMessage.Create(payload, "correlation-123", Guid.NewGuid());

        // Assert
        message.Type.ShouldContain("TestPayload");
        message.Payload.ShouldNotBeNullOrEmpty();
        message.CorrelationId.ShouldBe("correlation-123");
        message.TenantId.ShouldNotBeNull();
    }

    [Fact]
    public void GetPayload_ShouldDeserializePayload()
    {
        // Arrange
        var original = new TestPayload { Name = "Test", Value = 42 };
        var message = OutboxMessage.Create(original);

        // Act
        var payload = message.GetPayload<TestPayload>();

        // Assert
        payload.ShouldNotBeNull();
        payload!.Name.ShouldBe("Test");
        payload.Value.ShouldBe(42);
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var message = new OutboxMessage
        {
            Id = id,
            Type = "TestType",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ProcessedAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Processed,
            RetryCount = 2,
            Error = "Previous error",
            CorrelationId = "corr-123",
            TenantId = tenantId,
            Metadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Assert
        message.Id.ShouldBe(id);
        message.Type.ShouldBe("TestType");
        message.Status.ShouldBe(OutboxMessageStatus.Processed);
        message.RetryCount.ShouldBe(2);
        message.TenantId.ShouldBe(tenantId);
        message.Metadata["key"].ShouldBe("value");
    }

    private class TestPayload
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

public class OutboxMessageStatusTests
{
    [Fact]
    public void Status_ShouldHaveExpectedValues()
    {
        ((int)OutboxMessageStatus.Pending).ShouldBe(0);
        ((int)OutboxMessageStatus.Processing).ShouldBe(1);
        ((int)OutboxMessageStatus.Processed).ShouldBe(2);
        ((int)OutboxMessageStatus.Failed).ShouldBe(3);
    }
}

public class OutboxSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeOutbox()
    {
        OutboxSettings.SectionName.ShouldBe("Outbox");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new OutboxSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.ProcessingIntervalSeconds.ShouldBe(5);
        settings.BatchSize.ShouldBe(100);
        settings.MaxRetries.ShouldBe(3);
        settings.RetryDelaySeconds.ShouldBe(60);
        settings.CleanupIntervalHours.ShouldBe(24);
        settings.RetentionDays.ShouldBe(7);
        settings.UseTransaction.ShouldBeTrue();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new OutboxSettings
        {
            Enabled = false,
            ProcessingIntervalSeconds = 10,
            BatchSize = 50,
            MaxRetries = 5,
            RetryDelaySeconds = 120,
            CleanupIntervalHours = 12,
            RetentionDays = 14,
            UseTransaction = false
        };

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.ProcessingIntervalSeconds.ShouldBe(10);
        settings.BatchSize.ShouldBe(50);
        settings.MaxRetries.ShouldBe(5);
        settings.RetryDelaySeconds.ShouldBe(120);
        settings.CleanupIntervalHours.ShouldBe(12);
        settings.RetentionDays.ShouldBe(14);
        settings.UseTransaction.ShouldBeFalse();
    }
}
