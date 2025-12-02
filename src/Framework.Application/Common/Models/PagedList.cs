using System.Text.Json.Serialization;

namespace Framework.Application.Common.Models;

/// <summary>
/// Represents a paginated list of items
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Parameterless constructor for JSON deserialization
    /// </summary>
    public PagedList() { }

    [JsonConstructor]
    public PagedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public static PagedList<T> Empty(int pageNumber = 1, int pageSize = 10)
        => new([], 0, pageNumber, pageSize);

    public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var items = source.ToList();
        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<T>(pagedItems, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Maps items to a different type
    /// </summary>
    public PagedList<TDestination> Map<TDestination>(Func<T, TDestination> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return new PagedList<TDestination>(mappedItems, TotalCount, PageNumber, PageSize);
    }
}

/// <summary>
/// Pagination request parameters
/// </summary>
public record PaginationRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}
