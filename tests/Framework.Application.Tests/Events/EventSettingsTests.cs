using Framework.Application.Events;
using Shouldly;

namespace Framework.Application.Tests.Events;

public class EventSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeEvents()
    {
        // Assert
        EventSettings.SectionName.ShouldBe("Events");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new EventSettings();

        // Assert
        settings.ThrowOnHandlerException.ShouldBeFalse();
        settings.ParallelDispatch.ShouldBeFalse();
        settings.MaxParallelism.ShouldBe(4);
        settings.LogEvents.ShouldBeTrue();
        settings.EventRetention.ShouldBe(TimeSpan.FromDays(7));
        settings.UseOutbox.ShouldBeFalse();
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new EventSettings
        {
            ThrowOnHandlerException = true,
            ParallelDispatch = true,
            MaxParallelism = 8,
            LogEvents = false,
            EventRetention = TimeSpan.FromDays(30),
            UseOutbox = true
        };

        // Assert
        settings.ThrowOnHandlerException.ShouldBeTrue();
        settings.ParallelDispatch.ShouldBeTrue();
        settings.MaxParallelism.ShouldBe(8);
        settings.LogEvents.ShouldBeFalse();
        settings.EventRetention.ShouldBe(TimeSpan.FromDays(30));
        settings.UseOutbox.ShouldBeTrue();
    }
}
