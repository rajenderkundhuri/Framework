using Framework.Application.Common.Commands;
using Framework.Domain.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Framework.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps commands in a transaction
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply transactions to commands
        if (!IsCommand())
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        try
        {
            _logger.LogDebug("Beginning transaction for {RequestName}", requestName);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var response = await next();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug("Transaction committed for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);

            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).GetInterfaces()
            .Any(i => i == typeof(ICommand) ||
                     (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
    }
}
