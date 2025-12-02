using Framework.Application.BackgroundJobs;
using Shouldly;

namespace Framework.Application.Tests.BackgroundJobs;

public class JobContextTests
{
    [Fact]
    public void JobContext_DefaultValues_ShouldBeNull()
    {
        // Act
        var context = new JobContext();

        // Assert
        context.TenantId.ShouldBeNull();
        context.UserId.ShouldBeNull();
        context.UserName.ShouldBeNull();
        context.CorrelationId.ShouldBeNull();
        context.Culture.ShouldBeNull();
        context.AdditionalData.ShouldNotBeNull();
        context.AdditionalData.ShouldBeEmpty();
    }

    [Fact]
    public void CreateFromCurrent_ShouldSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = "user-123";
        var userName = "John Doe";
        var correlationId = "corr-456";

        // Act
        var context = JobContext.CreateFromCurrent(tenantId, userId, userName, correlationId);

        // Assert
        context.TenantId.ShouldBe(tenantId);
        context.UserId.ShouldBe(userId);
        context.UserName.ShouldBe(userName);
        context.CorrelationId.ShouldBe(correlationId);
        context.Culture.ShouldNotBeNull();
    }

    [Fact]
    public void CreateFromCurrent_WithoutCorrelationId_ShouldGenerateOne()
    {
        // Act
        var context = JobContext.CreateFromCurrent(null, null, null);

        // Assert
        context.CorrelationId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CreateFromCurrent_ShouldCaptureCulture()
    {
        // Act
        var context = JobContext.CreateFromCurrent(null, null, null);

        // Assert
        context.Culture.ShouldBe(Thread.CurrentThread.CurrentCulture.Name);
    }

    [Fact]
    public void AdditionalData_ShouldBeModifiable()
    {
        // Arrange
        var context = new JobContext();

        // Act
        context.AdditionalData["key1"] = "value1";
        context.AdditionalData["key2"] = "value2";

        // Assert
        context.AdditionalData.Count.ShouldBe(2);
        context.AdditionalData["key1"].ShouldBe("value1");
        context.AdditionalData["key2"].ShouldBe("value2");
    }
}

public class JobContextAccessorTests
{
    [Fact]
    public void CurrentContext_InitiallyNull()
    {
        // Arrange
        var accessor = new JobContextAccessor();

        // Assert
        accessor.CurrentContext.ShouldBeNull();
    }

    [Fact]
    public void CurrentContext_CanBeSetAndRetrieved()
    {
        // Arrange
        var accessor = new JobContextAccessor();
        var context = new JobContext
        {
            TenantId = Guid.NewGuid(),
            UserId = "user-1"
        };

        // Act
        accessor.CurrentContext = context;

        // Assert
        accessor.CurrentContext.ShouldNotBeNull();
        accessor.CurrentContext.TenantId.ShouldBe(context.TenantId);
        accessor.CurrentContext.UserId.ShouldBe(context.UserId);
    }

    [Fact]
    public void CurrentContext_CanBeCleared()
    {
        // Arrange
        var accessor = new JobContextAccessor();
        accessor.CurrentContext = new JobContext { UserId = "user-1" };

        // Act
        accessor.CurrentContext = null;

        // Assert
        accessor.CurrentContext.ShouldBeNull();
    }

    [Fact]
    public async Task CurrentContext_IsIsolatedAcrossAsyncFlows()
    {
        // Arrange
        var accessor = new JobContextAccessor();
        var context1 = new JobContext { UserId = "user-1" };

        // Act & Assert
        accessor.CurrentContext = context1;
        accessor.CurrentContext?.UserId.ShouldBe("user-1");

        var task = Task.Run(() =>
        {
            // This should see context1 due to AsyncLocal flow
            return accessor.CurrentContext?.UserId;
        });

        var result = await task;
        result.ShouldBe("user-1");
    }
}

public class JobDataBaseTests
{
    private class TestJobData : JobDataBase
    {
        public string? Payload { get; set; }
    }

    [Fact]
    public void JobDataBase_ShouldHaveDefaultContext()
    {
        // Act
        var data = new TestJobData();

        // Assert
        data.Context.ShouldNotBeNull();
    }

    [Fact]
    public void JobDataBase_ContextCanBeSet()
    {
        // Arrange
        var data = new TestJobData();
        var context = new JobContext
        {
            TenantId = Guid.NewGuid(),
            UserId = "user-1"
        };

        // Act
        data.Context = context;

        // Assert
        data.Context.TenantId.ShouldBe(context.TenantId);
        data.Context.UserId.ShouldBe("user-1");
    }
}
