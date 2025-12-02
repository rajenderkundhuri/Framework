using Framework.Application.Caching;
using Shouldly;

namespace Framework.Application.Tests.Caching;

public class CacheKeyBuilderTests
{
    [Fact]
    public void Build_WithNoSegments_ShouldReturnEmpty()
    {
        // Act
        var key = new CacheKeyBuilder().Build();

        // Assert
        key.ShouldBe(string.Empty);
    }

    [Fact]
    public void Build_WithPrefix_ShouldIncludePrefix()
    {
        // Act
        var key = new CacheKeyBuilder("app").Build();

        // Assert
        key.ShouldBe("app");
    }

    [Fact]
    public void Add_SingleSegment_ShouldBuildCorrectly()
    {
        // Act
        var key = new CacheKeyBuilder()
            .Add("users")
            .Build();

        // Assert
        key.ShouldBe("users");
    }

    [Fact]
    public void Add_MultipleSegments_ShouldJoinWithSeparator()
    {
        // Act
        var key = new CacheKeyBuilder()
            .Add("users")
            .Add("123")
            .Build();

        // Assert
        key.ShouldBe("users:123");
    }

    [Fact]
    public void Add_WithPrefix_ShouldIncludeAll()
    {
        // Act
        var key = new CacheKeyBuilder("app")
            .Add("users")
            .Add("123")
            .Build();

        // Assert
        key.ShouldBe("app:users:123");
    }

    [Fact]
    public void Add_WithKeyValue_ShouldFormatCorrectly()
    {
        // Act
        var key = new CacheKeyBuilder()
            .Add("users")
            .Add("id", 123)
            .Build();

        // Assert
        key.ShouldBe("users:id=123");
    }

    [Fact]
    public void Add_WithNullValue_ShouldSkip()
    {
        // Act
        var key = new CacheKeyBuilder()
            .Add("users")
            .Add("id", null)
            .Build();

        // Assert
        key.ShouldBe("users");
    }

    [Fact]
    public void Add_EmptySegment_ShouldSkip()
    {
        // Act
        var key = new CacheKeyBuilder()
            .Add("users")
            .Add("")
            .Add("active")
            .Build();

        // Assert
        key.ShouldBe("users:active");
    }

    [Fact]
    public void WithTenant_WithTenantId_ShouldAddTenantSegment()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var key = new CacheKeyBuilder()
            .WithTenant(tenantId)
            .Add("users")
            .Build();

        // Assert
        key.ShouldBe($"tenant={tenantId}:users");
    }

    [Fact]
    public void WithTenant_WithoutTenantId_ShouldAddHostSegment()
    {
        // Act
        var key = new CacheKeyBuilder()
            .WithTenant(null)
            .Add("users")
            .Build();

        // Assert
        key.ShouldBe("tenant=host:users");
    }

    [Fact]
    public void WithUser_ShouldAddUserSegment()
    {
        // Act
        var key = new CacheKeyBuilder()
            .WithUser("user-123")
            .Add("preferences")
            .Build();

        // Assert
        key.ShouldBe("user=user-123:preferences");
    }

    [Fact]
    public void WithUser_WithEmptyUserId_ShouldSkip()
    {
        // Act
        var key = new CacheKeyBuilder()
            .WithUser("")
            .Add("preferences")
            .Build();

        // Assert
        key.ShouldBe("preferences");
    }

    [Fact]
    public void ForEntity_ShouldAddEntityType()
    {
        // Act
        var key = new CacheKeyBuilder()
            .ForEntity<TestEntity>()
            .Build();

        // Assert
        key.ShouldBe("testentity");
    }

    [Fact]
    public void ForEntity_WithId_ShouldAddEntityTypeAndId()
    {
        // Act
        var key = new CacheKeyBuilder()
            .ForEntity<TestEntity>(123)
            .Build();

        // Assert
        key.ShouldBe("testentity:123");
    }

    [Fact]
    public void Create_StaticMethod_ShouldCreateBuilder()
    {
        // Act
        var key = CacheKeyBuilder.Create("prefix")
            .Add("test")
            .Build();

        // Assert
        key.ShouldBe("prefix:test");
    }

    [Fact]
    public void ForEntity_StaticMethod_ShouldCreateEntityKey()
    {
        // Act
        var key = CacheKeyBuilder.ForEntity<TestEntity>(456);

        // Assert
        key.ShouldBe("testentity:456");
    }

    [Fact]
    public void ForEntity_StaticWithPrefix_ShouldIncludePrefix()
    {
        // Act
        var key = CacheKeyBuilder.ForEntity<TestEntity>(789, "app");

        // Assert
        key.ShouldBe("app:testentity:789");
    }

    [Fact]
    public void ForCollection_ShouldCreateCollectionKey()
    {
        // Act
        var key = CacheKeyBuilder.ForCollection<TestEntity>();

        // Assert
        key.ShouldBe("testentity:all");
    }

    [Fact]
    public void ForQuery_ShouldCreateQueryKey()
    {
        // Act
        var key = CacheKeyBuilder.ForQuery<TestEntity>("GetActive");

        // Assert
        key.ShouldBe("testentity:query:GetActive");
    }

    [Fact]
    public void ForQuery_WithParameters_ShouldIncludeParameters()
    {
        // Act
        var key = CacheKeyBuilder.ForQuery<TestEntity>("Search", null, "keyword", 10, 1);

        // Assert
        key.ShouldBe("testentity:query:Search:keyword:10:1");
    }

    [Fact]
    public void ToString_ShouldReturnBuild()
    {
        // Arrange
        var builder = new CacheKeyBuilder("app").Add("test");

        // Assert
        builder.ToString().ShouldBe(builder.Build());
    }

    private class TestEntity { }
}

public class CacheKeysTests
{
    [Fact]
    public void Tenant_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var key = CacheKeys.Tenant(tenantId, "users");

        // Assert
        key.ShouldBe($"tenant:{tenantId}:users");
    }

    [Fact]
    public void User_ShouldFormatCorrectly()
    {
        // Act
        var key = CacheKeys.User("user-123", "preferences");

        // Assert
        key.ShouldBe("user:user-123:preferences");
    }

    [Fact]
    public void Config_ShouldFormatCorrectly()
    {
        // Act
        var key = CacheKeys.Config("settings");

        // Assert
        key.ShouldBe("config:settings");
    }

    [Fact]
    public void UserPermissions_ShouldFormatCorrectly()
    {
        // Act
        var key = CacheKeys.UserPermissions("user-456");

        // Assert
        key.ShouldBe("permissions:user-456");
    }

    [Fact]
    public void UserRoles_ShouldFormatCorrectly()
    {
        // Act
        var key = CacheKeys.UserRoles("user-789");

        // Assert
        key.ShouldBe("roles:user-789");
    }

    [Fact]
    public void TenantPattern_ShouldReturnWildcardPattern()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var pattern = CacheKeys.TenantPattern(tenantId);

        // Assert
        pattern.ShouldBe($"tenant:{tenantId}:*");
    }

    [Fact]
    public void UserPattern_ShouldReturnWildcardPattern()
    {
        // Act
        var pattern = CacheKeys.UserPattern("user-123");

        // Assert
        pattern.ShouldBe("user:user-123:*");
    }
}
