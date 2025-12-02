using Framework.Domain.Common.Entities;

namespace Framework.Domain.Tests.Entities;

public class TestAuditableEntity : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

public class TestFullAuditableEntity : FullAuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

public class AuditableEntityTests
{
    [Fact]
    public void AuditableEntity_SetCreated_ShouldSetTimestampAndUser()
    {
        // Arrange
        var entity = new TestAuditableEntity();
        var timestamp = DateTimeOffset.UtcNow;
        var userId = "user-123";

        // Act
        entity.SetCreated(timestamp, userId);

        // Assert
        Assert.Equal(timestamp, entity.CreatedAt);
        Assert.Equal(userId, entity.CreatedBy);
    }

    [Fact]
    public void AuditableEntity_SetModified_ShouldSetTimestampAndUser()
    {
        // Arrange
        var entity = new TestAuditableEntity();
        var timestamp = DateTimeOffset.UtcNow;
        var userId = "user-456";

        // Act
        entity.SetModified(timestamp, userId);

        // Assert
        Assert.Equal(timestamp, entity.LastModifiedAt);
        Assert.Equal(userId, entity.LastModifiedBy);
    }

    [Fact]
    public void FullAuditableEntity_SoftDelete_ShouldMarkAsDeleted()
    {
        // Arrange
        var entity = new TestFullAuditableEntity();
        var timestamp = DateTimeOffset.UtcNow;
        var userId = "user-789";

        // Act
        entity.SoftDelete(timestamp, userId);

        // Assert
        Assert.True(entity.IsDeleted);
        Assert.Equal(timestamp, entity.DeletedAt);
        Assert.Equal(userId, entity.DeletedBy);
    }

    [Fact]
    public void FullAuditableEntity_Restore_ShouldClearDeletedFlags()
    {
        // Arrange
        var entity = new TestFullAuditableEntity();
        entity.SoftDelete(DateTimeOffset.UtcNow, "user");

        // Act
        entity.Restore();

        // Assert
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
    }
}
