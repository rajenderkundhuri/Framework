using Framework.Domain.Events;
using Shouldly;

namespace Framework.Domain.Tests.Events;

public class DomainEventTests
{
    private class TestEvent : DomainEvent
    {
        public string Message { get; }

        public TestEvent(string message)
        {
            Message = message;
        }
    }

    [Fact]
    public void DomainEvent_ShouldGenerateUniqueEventId()
    {
        // Act
        var event1 = new TestEvent("test1");
        var event2 = new TestEvent("test2");

        // Assert
        event1.EventId.ShouldNotBe(Guid.Empty);
        event2.EventId.ShouldNotBe(Guid.Empty);
        event1.EventId.ShouldNotBe(event2.EventId);
    }

    [Fact]
    public void DomainEvent_ShouldSetOccurredAt()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var @event = new TestEvent("test");

        // Assert
        @event.OccurredAt.ShouldBeGreaterThanOrEqualTo(before);
        @event.OccurredAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void DomainEvent_ShouldReturnTypeName()
    {
        // Act
        var @event = new TestEvent("test");

        // Assert
        @event.EventType.ShouldBe("TestEvent");
    }
}

public class EntityDomainEventTests
{
    private class UserCreatedEvent : EntityDomainEvent<Guid>
    {
        public string UserName { get; }

        public UserCreatedEvent(Guid userId, string userName) : base(userId)
        {
            UserName = userName;
        }
    }

    [Fact]
    public void EntityDomainEvent_ShouldStoreEntityId()
    {
        // Arrange
        var entityId = Guid.NewGuid();

        // Act
        var @event = new UserCreatedEvent(entityId, "john");

        // Assert
        @event.EntityId.ShouldBe(entityId);
        @event.UserName.ShouldBe("john");
    }

    [Fact]
    public void EntityDomainEvent_ShouldInheritFromDomainEvent()
    {
        // Act
        var @event = new UserCreatedEvent(Guid.NewGuid(), "test");

        // Assert
        @event.ShouldBeAssignableTo<DomainEvent>();
        @event.ShouldBeAssignableTo<IDomainEvent>();
        @event.EventId.ShouldNotBe(Guid.Empty);
    }
}

public class TenantDomainEventTests
{
    private class TenantTestEvent : TenantDomainEvent
    {
        public string Data { get; init; } = string.Empty;
    }

    [Fact]
    public void TenantDomainEvent_ShouldAllowSettingTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var @event = new TenantTestEvent
        {
            TenantId = tenantId,
            Data = "test"
        };

        // Assert
        @event.TenantId.ShouldBe(tenantId);
        @event.Data.ShouldBe("test");
    }

    [Fact]
    public void TenantDomainEvent_ShouldAllowNullTenantId()
    {
        // Act
        var @event = new TenantTestEvent { Data = "test" };

        // Assert
        @event.TenantId.ShouldBeNull();
    }
}

public class EntityWithDomainEventsTests
{
    private class TestEntity : EntityWithDomainEvents
    {
        public string Name { get; private set; } = string.Empty;

        public void SetName(string name)
        {
            Name = name;
            AddDomainEvent(new NameChangedEvent(name));
        }

        public void DoSomething()
        {
            AddDomainEvent(new SomethingHappenedEvent());
        }
    }

    private class NameChangedEvent : DomainEvent
    {
        public string NewName { get; }
        public NameChangedEvent(string newName) => NewName = newName;
    }

    private class SomethingHappenedEvent : DomainEvent { }

    [Fact]
    public void EntityWithDomainEvents_ShouldStartWithNoEvents()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void EntityWithDomainEvents_ShouldAddDomainEvents()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SetName("Test Name");

        // Assert
        entity.DomainEvents.Count.ShouldBe(1);
        entity.DomainEvents.First().ShouldBeOfType<NameChangedEvent>();
    }

    [Fact]
    public void EntityWithDomainEvents_ShouldAccumulateEvents()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.SetName("Name 1");
        entity.DoSomething();
        entity.SetName("Name 2");

        // Assert
        entity.DomainEvents.Count.ShouldBe(3);
    }

    [Fact]
    public void EntityWithDomainEvents_ShouldClearEvents()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SetName("Test");
        entity.DoSomething();
        entity.DomainEvents.Count.ShouldBe(2);

        // Act
        entity.ClearDomainEvents();

        // Assert
        entity.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void EntityWithDomainEvents_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntity();
        entity.SetName("Test");

        // Assert
        entity.DomainEvents.ShouldBeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}

public class HasDomainEventsInterfaceTests
{
    private class TestEntity : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

        public void AddEvent(IDomainEvent @event) => _events.Add(@event);
        public void ClearDomainEvents() => _events.Clear();
    }

    [Fact]
    public void IHasDomainEvents_ShouldBeImplementable()
    {
        // Arrange
        var entity = new TestEntity();
        var @event = new TestDomainEvent();

        // Act
        entity.AddEvent(@event);

        // Assert
        entity.DomainEvents.Count.ShouldBe(1);
    }

    private class TestDomainEvent : DomainEvent { }
}
