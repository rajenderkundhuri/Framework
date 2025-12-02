using Framework.Application.Common.Models;
using MediatR;

namespace Framework.Application.Common.Commands;

/// <summary>
/// Handler for commands without response
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler for commands with typed response
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
