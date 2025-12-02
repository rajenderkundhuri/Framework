using Framework.Application.Caching;
using Shouldly;

namespace Framework.Application.Tests.Caching;

public class CacheOptionsTests
{
    [Fact]
    public void CacheOptions_DefaultValues_ShouldBeNull()
    {
        // Act
        var options = new CacheOptions();

        // Assert
        options.AbsoluteExpiration.ShouldBeNull();
        options.AbsoluteExpirationRelativeToNow.ShouldBeNull();
        options.SlidingExpiration.ShouldBeNull();
        options.Priority.ShouldBe(CachePriority.Normal);
        options.Tags.ShouldNotBeNull();
        options.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void Default_ShouldCreateOptionsWithDuration()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var options = CacheOptions.Default(duration);

        // Assert
        options.AbsoluteExpirationRelativeToNow.ShouldBe(duration);
        options.SlidingExpiration.ShouldBeNull();
    }

    [Fact]
    public void Default_WithoutDuration_ShouldUse5Minutes()
    {
        // Act
        var options = CacheOptions.Default();

        // Assert
        options.AbsoluteExpirationRelativeToNow.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Sliding_ShouldCreateSlidingExpiration()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(15);

        // Act
        var options = CacheOptions.Sliding(duration);

        // Assert
        options.SlidingExpiration.ShouldBe(duration);
        options.AbsoluteExpirationRelativeToNow.ShouldBeNull();
    }

    [Fact]
    public void NeverExpire_ShouldCreateEmptyOptions()
    {
        // Act
        var options = CacheOptions.NeverExpire();

        // Assert
        options.AbsoluteExpiration.ShouldBeNull();
        options.AbsoluteExpirationRelativeToNow.ShouldBeNull();
        options.SlidingExpiration.ShouldBeNull();
    }

    [Fact]
    public void Tags_ShouldBeModifiable()
    {
        // Arrange
        var options = new CacheOptions();

        // Act
        options.Tags.Add("tag1");
        options.Tags.Add("tag2");

        // Assert
        options.Tags.Count.ShouldBe(2);
        options.Tags.ShouldContain("tag1");
        options.Tags.ShouldContain("tag2");
    }
}

public class CachePriorityTests
{
    [Theory]
    [InlineData(CachePriority.Low, 0)]
    [InlineData(CachePriority.Normal, 1)]
    [InlineData(CachePriority.High, 2)]
    [InlineData(CachePriority.NeverRemove, 3)]
    public void CachePriority_ShouldHaveCorrectValues(CachePriority priority, int expected)
    {
        ((int)priority).ShouldBe(expected);
    }
}
