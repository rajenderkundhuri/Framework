using Framework.Application.Common.Models;
using MediatR;

namespace Framework.Application.Common.Commands;

/// <summary>
/// Marker interface for commands (write operations)
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Command that returns a typed result
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Base record for commands
/// </summary>
public abstract record Command : ICommand;

/// <summary>
/// Base record for commands with a typed response
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public abstract record Command<TResponse> : ICommand<TResponse>;
