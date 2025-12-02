using Framework.Application.Common.Models;

namespace Framework.Application.Tests.Models;

public class PagedListTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act
        var pagedList = new PagedList<string>(items, 10, 1, 3);

        // Assert
        Assert.Equal(3, pagedList.Items.Count);
        Assert.Equal(10, pagedList.TotalCount);
        Assert.Equal(1, pagedList.PageNumber);
        Assert.Equal(3, pagedList.PageSize);
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "a", "b" };

        // Act
        var pagedList = new PagedList<string>(items, 25, 1, 10);

        // Assert
        Assert.Equal(3, pagedList.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalseOnFirstPage()
    {
        // Arrange
        var items = new List<string> { "a" };

        // Act
        var pagedList = new PagedList<string>(items, 50, 1, 10);

        // Assert
        Assert.False(pagedList.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrueOnSubsequentPages()
    {
        // Arrange
        var items = new List<string> { "a" };

        // Act
        var pagedList = new PagedList<string>(items, 50, 2, 10);

        // Assert
        Assert.True(pagedList.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_ShouldBeTrueWhenMorePagesExist()
    {
        // Arrange
        var items = new List<string> { "a" };

        // Act
        var pagedList = new PagedList<string>(items, 50, 1, 10);

        // Assert
        Assert.True(pagedList.HasNextPage);
    }

    [Fact]
    public void HasNextPage_ShouldBeFalseOnLastPage()
    {
        // Arrange
        var items = new List<string> { "a" };

        // Act
        var pagedList = new PagedList<string>(items, 50, 5, 10);

        // Assert
        Assert.False(pagedList.HasNextPage);
    }

    [Fact]
    public void Empty_ShouldCreateEmptyPagedList()
    {
        // Act
        var pagedList = PagedList<string>.Empty(1, 10);

        // Assert
        Assert.Empty(pagedList.Items);
        Assert.Equal(0, pagedList.TotalCount);
        Assert.Equal(1, pagedList.PageNumber);
        Assert.Equal(10, pagedList.PageSize);
    }

    [Fact]
    public void Create_ShouldPaginateCorrectly()
    {
        // Arrange
        var source = Enumerable.Range(1, 25).ToList();

        // Act
        var pagedList = PagedList<int>.Create(source, 2, 10);

        // Assert
        Assert.Equal(10, pagedList.Items.Count);
        Assert.Equal(11, pagedList.Items[0]);
        Assert.Equal(20, pagedList.Items[9]);
        Assert.Equal(25, pagedList.TotalCount);
    }

    [Fact]
    public void Map_ShouldTransformItems()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };
        var pagedList = new PagedList<int>(items, 100, 1, 10);

        // Act
        var mapped = pagedList.Map(x => x.ToString());

        // Assert
        Assert.Equal(3, mapped.Items.Count);
        Assert.Equal("1", mapped.Items[0]);
        Assert.Equal("2", mapped.Items[1]);
        Assert.Equal("3", mapped.Items[2]);
        Assert.Equal(100, mapped.TotalCount);
        Assert.Equal(1, mapped.PageNumber);
        Assert.Equal(10, mapped.PageSize);
    }
}

public class PaginationRequestTests
{
    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var request = new PaginationRequest();

        // Assert
        Assert.Equal(1, request.PageNumber);
        Assert.Equal(10, request.PageSize);
    }

    [Fact]
    public void Skip_ShouldCalculateCorrectly()
    {
        // Arrange
        var request = new PaginationRequest { PageNumber = 3, PageSize = 10 };

        // Act & Assert
        Assert.Equal(20, request.Skip);
    }

    [Fact]
    public void Take_ShouldEqualPageSize()
    {
        // Arrange
        var request = new PaginationRequest { PageSize = 25 };

        // Act & Assert
        Assert.Equal(25, request.Take);
    }
}
