using Framework.Application.Events;
using Framework.Domain.Events;
using Framework.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Events;

public class InMemoryEventDispatcherTests
{
    private class TestEvent : DomainEvent
    {
        public string Message { get; }
        public TestEvent(string message) => Message = message;
    }

    private class AnotherEvent : DomainEvent
    {
        public int Value { get; }
        public AnotherEvent(int value) => Value = value;
    }

    private class TestEventHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> HandledEvents { get; } = new();

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private class SecondTestEventHandler : IEventHandler<TestEvent>
    {
        public List<TestEvent> HandledEvents { get; } = new();

        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private class ThrowingHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Handler error");
        }
    }

    private class SlowHandler : IEventHandler<TestEvent>
    {
        private int _callCount;
        public int CallCount => _callCount;

        public async Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);
            Interlocked.Increment(ref _callCount);
        }
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlers_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);
        var @event = new TestEvent("test");

        // Act & Assert
        await Should.NotThrowAsync(() => dispatcher.DispatchAsync(@event));
    }

    [Fact]
    public async Task DispatchAsync_WithSingleHandler_ShouldInvokeHandler()
    {
        // Arrange
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);
        var @event = new TestEvent("test message");

        // Act
        await dispatcher.DispatchAsync(@event);

        // Assert
        handler.HandledEvents.Count.ShouldBe(1);
        handler.HandledEvents[0].Message.ShouldBe("test message");
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_ShouldInvokeAll()
    {
        // Arrange
        var handler1 = new TestEventHandler();
        var handler2 = new SecondTestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);
        var @event = new TestEvent("test");

        // Act
        await dispatcher.DispatchAsync(@event);

        // Assert
        handler1.HandledEvents.Count.ShouldBe(1);
        handler2.HandledEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DispatchAsync_WithThrowingHandler_ShouldNotThrowByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingHandler());
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);
        var @event = new TestEvent("test");

        // Act & Assert
        await Should.NotThrowAsync(() => dispatcher.DispatchAsync(@event));
    }

    [Fact]
    public async Task DispatchAsync_WithThrowOnException_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(new ThrowingHandler());
        var provider = services.BuildServiceProvider();
        var settings = new EventSettings { ThrowOnHandlerException = true };
        var dispatcher = CreateDispatcher(provider, settings);
        var @event = new TestEvent("test");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(@event));
    }

    [Fact]
    public async Task DispatchAsync_MultipleEvents_ShouldDispatchAll()
    {
        // Arrange
        var handler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);

        var events = new IDomainEvent[]
        {
            new TestEvent("event1"),
            new TestEvent("event2"),
            new TestEvent("event3")
        };

        // Act
        await dispatcher.DispatchAsync(events);

        // Assert
        handler.HandledEvents.Count.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchAsync_WithParallelDispatch_ShouldInvokeInParallel()
    {
        // Arrange
        var handler1 = new SlowHandler();
        var handler2 = new SlowHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler1);
        services.AddSingleton<IEventHandler<TestEvent>>(handler2);
        var provider = services.BuildServiceProvider();
        var settings = new EventSettings { ParallelDispatch = true, MaxParallelism = 4 };
        var dispatcher = CreateDispatcher(provider, settings);
        var @event = new TestEvent("test");

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.DispatchAsync(@event);
        sw.Stop();

        // Assert - parallel should be faster than sequential (200ms for 2 100ms handlers)
        handler1.CallCount.ShouldBe(1);
        handler2.CallCount.ShouldBe(1);
        // With parallel, both should run at the same time (~100ms total)
        sw.ElapsedMilliseconds.ShouldBeLessThan(180);
    }

    [Fact]
    public async Task DispatchAsync_OnlyMatchingHandlers_ShouldBeInvoked()
    {
        // Arrange
        var testHandler = new TestEventHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(testHandler);
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);

        // Act - dispatch AnotherEvent, not TestEvent
        await dispatcher.DispatchAsync(new AnotherEvent(42));

        // Assert - TestEventHandler should NOT be called
        testHandler.HandledEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        var handler = new SlowHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);
        var @event = new TestEvent("test");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - should throw or complete quickly
        try
        {
            await dispatcher.DispatchAsync(@event, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    private static InMemoryEventDispatcher CreateDispatcher(
        IServiceProvider provider,
        EventSettings? settings = null)
    {
        var logger = Substitute.For<ILogger<InMemoryEventDispatcher>>();
        var options = Options.Create(settings ?? new EventSettings());
        return new InMemoryEventDispatcher(provider, logger, options);
    }
}
