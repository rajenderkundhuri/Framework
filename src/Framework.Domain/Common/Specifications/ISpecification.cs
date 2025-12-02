using System.Linq.Expressions;

namespace Framework.Domain.Common.Specifications;

/// <summary>
/// Specification pattern interface for encapsulating query logic
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// The criteria expression for filtering
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Include expressions for eager loading
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based include expressions for ThenInclude support
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression (ascending)
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression (descending)
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Number of items to take
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Number of items to skip
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Whether to track entities
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Whether to split query for includes
    /// </summary>
    bool AsSplitQuery { get; }
}
