using Framework.Application.Common.Models;
using MediatR;

namespace Framework.Application.Common.Queries;

/// <summary>
/// Handler for queries with typed response
/// </summary>
/// <typeparam name="TQuery">Query type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
