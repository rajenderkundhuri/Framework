using Framework.Domain.Common.Entities;

namespace Framework.Domain.Tests.Entities;

public class TestEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;

    public TestEntity() : base() { }
    public TestEntity(Guid id) : base(id) { }
}

public class EntityTests
{
    [Fact]
    public void Entity_WithSameId_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        // Act & Assert
        Assert.Equal(entity1, entity2);
        Assert.True(entity1 == entity2);
    }

    [Fact]
    public void Entity_WithDifferentId_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid());
        var entity2 = new TestEntity(Guid.NewGuid());

        // Act & Assert
        Assert.NotEqual(entity1, entity2);
        Assert.True(entity1 != entity2);
    }

    [Fact]
    public void Entity_WithDefaultId_ShouldBeTransient()
    {
        // Arrange
        var entity = new TestEntity();

        // Act & Assert
        Assert.True(entity.IsTransient());
    }

    [Fact]
    public void Entity_WithAssignedId_ShouldNotBeTransient()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid());

        // Act & Assert
        Assert.False(entity.IsTransient());
    }

    [Fact]
    public void TransientEntities_ShouldNotBeEqual()
    {
        // Arrange
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Act & Assert
        Assert.NotEqual(entity1, entity2);
    }
}
