using System.Linq.Expressions;

namespace Framework.Domain.Common.Specifications;

/// <summary>
/// Base implementation of the specification pattern
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; } = true;
    public bool AsSplitQuery { get; private set; }

    protected Specification()
    {
    }

    protected Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Sets the filter criteria
    /// </summary>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for eager loading
    /// </summary>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include for ThenInclude support
    /// </summary>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Sets ascending order
    /// </summary>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Sets descending order
    /// </summary>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies paging
    /// </summary>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Enables no-tracking for read-only queries
    /// </summary>
    protected void ApplyNoTracking(bool noTracking = true)
    {
        AsNoTracking = noTracking;
    }

    /// <summary>
    /// Enables split query for complex includes
    /// </summary>
    protected void ApplySplitQuery(bool splitQuery = true)
    {
        AsSplitQuery = splitQuery;
    }
}
