using System.Linq.Expressions;
using Framework.Domain.Specifications;
using Shouldly;

namespace Framework.Domain.Tests.Specifications;

public class SpecificationTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
    }

    private class ActiveEntitiesSpec : Specification<TestEntity>
    {
        public override Expression<Func<TestEntity, bool>> Criteria =>
            e => e.IsActive;
    }

    private class NameContainsSpec : Specification<TestEntity>
    {
        private readonly string _searchTerm;

        public NameContainsSpec(string searchTerm)
        {
            _searchTerm = searchTerm;
        }

        public override Expression<Func<TestEntity, bool>> Criteria =>
            e => e.Name.Contains(_searchTerm);
    }

    private class PriceRangeSpec : Specification<TestEntity>
    {
        private readonly decimal _min;
        private readonly decimal _max;

        public PriceRangeSpec(decimal min, decimal max)
        {
            _min = min;
            _max = max;
        }

        public override Expression<Func<TestEntity, bool>> Criteria =>
            e => e.Price >= _min && e.Price <= _max;
    }

    private class OrderedSpec : Specification<TestEntity>
    {
        public OrderedSpec()
        {
            ApplyOrderBy(e => e.Name);
        }
    }

    private class PagedSpec : Specification<TestEntity>
    {
        public PagedSpec(int page, int pageSize)
        {
            ApplyPaging((page - 1) * pageSize, pageSize);
        }
    }

    [Fact]
    public void Specification_ShouldFilterEntities()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Active Item", IsActive = true },
            new() { Id = 2, Name = "Inactive Item", IsActive = false },
            new() { Id = 3, Name = "Another Active", IsActive = true }
        };

        var spec = new ActiveEntitiesSpec();

        // Act
        var result = entities.AsQueryable().Where(spec.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(e => e.IsActive);
    }

    [Fact]
    public void Specification_WithParameter_ShouldWork()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, Name = "Apple" },
            new() { Id = 2, Name = "Banana" },
            new() { Id = 3, Name = "Apricot" }
        };

        var spec = new NameContainsSpec("App");

        // Act
        var result = entities.AsQueryable().Where(spec.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Apple");
    }

    [Fact]
    public void Specification_WithRangeFilter_ShouldWork()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, Price = 5.00m },
            new() { Id = 2, Price = 15.00m },
            new() { Id = 3, Price = 25.00m },
            new() { Id = 4, Price = 35.00m }
        };

        var spec = new PriceRangeSpec(10m, 30m);

        // Act
        var result = entities.AsQueryable().Where(spec.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(e => e.Price >= 10m && e.Price <= 30m);
    }

    [Fact]
    public void Specification_WithOrdering_ShouldSetOrderBy()
    {
        // Arrange
        var spec = new OrderedSpec();

        // Assert
        spec.OrderBy.ShouldNotBeNull();
    }

    [Fact]
    public void Specification_WithPaging_ShouldSetPagingProperties()
    {
        // Arrange
        var spec = new PagedSpec(2, 10);

        // Assert
        spec.IsPagingEnabled.ShouldBeTrue();
        spec.Skip.ShouldBe(10);
        spec.Take.ShouldBe(10);
    }

    [Fact]
    public void Specification_DefaultValues_ShouldBeSet()
    {
        // Arrange
        var spec = new ActiveEntitiesSpec();

        // Assert
        spec.Includes.ShouldBeEmpty();
        spec.IncludeStrings.ShouldBeEmpty();
        spec.OrderBy.ShouldBeNull();
        spec.OrderByDescending.ShouldBeNull();
        spec.Take.ShouldBeNull();
        spec.Skip.ShouldBeNull();
        spec.IsPagingEnabled.ShouldBeFalse();
        spec.AsNoTracking.ShouldBeTrue();
        spec.AsSplitQuery.ShouldBeFalse();
    }
}

public class ExpressionSpecificationTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    public void ExpressionSpecification_ShouldUseCriteria()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> criteria = e => e.IsActive;
        var spec = new ExpressionSpecification<TestEntity>(criteria);

        // Assert
        spec.Criteria.ShouldBe(criteria);
    }

    [Fact]
    public void ExpressionSpecification_ShouldFilterCorrectly()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false }
        };

        var spec = new ExpressionSpecification<TestEntity>(e => e.Id > 1);

        // Act
        var result = entities.AsQueryable().Where(spec.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }
}

public class SpecificationExtensionsTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    [Fact]
    public void And_ShouldCombineSpecifications()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, IsActive = true, Category = "A" },
            new() { Id = 2, IsActive = true, Category = "B" },
            new() { Id = 3, IsActive = false, Category = "A" }
        };

        var activeSpec = new ExpressionSpecification<TestEntity>(e => e.IsActive);
        var categorySpec = new ExpressionSpecification<TestEntity>(e => e.Category == "A");

        // Act
        var combined = activeSpec.And(categorySpec);
        var result = entities.AsQueryable().Where(combined.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(1);
    }

    [Fact]
    public void Or_ShouldCombineSpecifications()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, IsActive = true, Category = "A" },
            new() { Id = 2, IsActive = false, Category = "B" },
            new() { Id = 3, IsActive = false, Category = "A" }
        };

        var activeSpec = new ExpressionSpecification<TestEntity>(e => e.IsActive);
        var categorySpec = new ExpressionSpecification<TestEntity>(e => e.Category == "B");

        // Act
        var combined = activeSpec.Or(categorySpec);
        var result = entities.AsQueryable().Where(combined.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(2); // Id 1 (active) and Id 2 (category B)
    }

    [Fact]
    public void Not_ShouldNegateSpecification()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = false },
            new() { Id = 3, IsActive = true }
        };

        var activeSpec = new ExpressionSpecification<TestEntity>(e => e.IsActive);

        // Act
        var negated = activeSpec.Not();
        var result = entities.AsQueryable().Where(negated.Criteria).ToList();

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(2);
    }
}
