using System.Linq.Expressions;

namespace Framework.Domain.Specifications;

/// <summary>
/// Interface for the specification pattern
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Criteria expression
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Includes for eager loading
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String includes for nested eager loading
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by descending expression
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Secondary order by expressions
    /// </summary>
    List<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenByExpressions { get; }

    /// <summary>
    /// Number of records to take
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Number of records to skip
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Whether to disable tracking
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Whether to split queries
    /// </summary>
    bool AsSplitQuery { get; }
}

/// <summary>
/// Base specification implementation
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public virtual Expression<Func<T, bool>> Criteria => _ => true;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public List<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenByExpressions { get; } = new();
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; } = true;
    public bool AsSplitQuery { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    protected void ApplyThenBy(Expression<Func<T, object>> thenByExpression)
    {
        ThenByExpressions.Add((thenByExpression, false));
    }

    protected void ApplyThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
    {
        ThenByExpressions.Add((thenByDescendingExpression, true));
    }

    protected void ApplyNoTracking()
    {
        AsNoTracking = true;
    }

    protected void ApplyTracking()
    {
        AsNoTracking = false;
    }

    protected void ApplySplitQuery()
    {
        AsSplitQuery = true;
    }
}

/// <summary>
/// Specification with explicit criteria
/// </summary>
public class ExpressionSpecification<T> : Specification<T>
{
    private readonly Expression<Func<T, bool>> _criteria;

    public ExpressionSpecification(Expression<Func<T, bool>> criteria)
    {
        _criteria = criteria;
    }

    public override Expression<Func<T, bool>> Criteria => _criteria;
}

/// <summary>
/// Extension methods for specifications
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Combines two specifications with AND
    /// </summary>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        var leftExpr = left.Criteria;
        var rightExpr = right.Criteria;

        var parameter = Expression.Parameter(typeof(T));
        var combined = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter));

        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
        return new ExpressionSpecification<T>(lambda);
    }

    /// <summary>
    /// Combines two specifications with OR
    /// </summary>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        var leftExpr = left.Criteria;
        var rightExpr = right.Criteria;

        var parameter = Expression.Parameter(typeof(T));
        var combined = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter));

        var lambda = Expression.Lambda<Func<T, bool>>(combined, parameter);
        return new ExpressionSpecification<T>(lambda);
    }

    /// <summary>
    /// Negates a specification
    /// </summary>
    public static ISpecification<T> Not<T>(this ISpecification<T> specification)
    {
        var expr = specification.Criteria;
        var parameter = Expression.Parameter(typeof(T));
        var negated = Expression.Not(Expression.Invoke(expr, parameter));
        var lambda = Expression.Lambda<Func<T, bool>>(negated, parameter);
        return new ExpressionSpecification<T>(lambda);
    }
}
