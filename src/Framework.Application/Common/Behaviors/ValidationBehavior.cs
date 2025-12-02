using FluentValidation;
using Framework.Application.Common.Models;
using MediatR;

namespace Framework.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for validation using FluentValidation
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        if (failures.Count != 0)
        {
            return CreateValidationResult<TResponse>(failures);
        }

        return await next();
    }

    private static TResponse CreateValidationResult<T>(IDictionary<string, string[]> errors)
        where T : Result
    {
        if (typeof(T) == typeof(Result))
        {
            return (Result.ValidationFailure(errors) as TResponse)!;
        }

        var resultType = typeof(T).GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(resultType)
            .GetMethod(nameof(Result<object>.ValidationFailure), [typeof(IDictionary<string, string[]>)]);

        return (TResponse)failureMethod!.Invoke(null, [errors])!;
    }
}
