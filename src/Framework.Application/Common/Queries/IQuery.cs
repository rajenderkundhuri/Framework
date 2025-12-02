using Framework.Application.Common.Models;
using MediatR;

namespace Framework.Application.Common.Queries;

/// <summary>
/// Marker interface for queries (read operations)
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Base record for queries
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public abstract record Query<TResponse> : IQuery<TResponse>;

/// <summary>
/// Query with pagination support
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public abstract record PagedQuery<TResponse> : IQuery<PagedList<TResponse>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? OrderBy { get; init; }
    public bool IsDescending { get; init; }
    public string? SearchTerm { get; init; }
}
