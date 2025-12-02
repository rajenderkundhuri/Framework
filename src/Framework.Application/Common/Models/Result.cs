namespace Framework.Application.Common.Models;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, string? error = null, string? errorCode = null, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(true);

    public static Result Failure(string error, string? errorCode = null)
        => new(false, error, errorCode);

    public static Result ValidationFailure(IDictionary<string, string[]> errors)
        => new(false, "Validation failed", "VALIDATION_ERROR", errors);

    public static Result NotFound(string message = "Resource not found")
        => new(false, message, "NOT_FOUND");

    public static Result Unauthorized(string message = "Unauthorized")
        => new(false, message, "UNAUTHORIZED");

    public static Result Forbidden(string message = "Forbidden")
        => new(false, message, "FORBIDDEN");

    public static Result Conflict(string message = "Conflict")
        => new(false, message, "CONFLICT");

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => Result<T>.Failure(error, errorCode);
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
/// <typeparam name="T">Value type</typeparam>
public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error = null, string? errorCode = null, IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, errorCode, validationErrors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);

    public new static Result<T> Failure(string error, string? errorCode = null)
        => new(false, default, error, errorCode);

    public new static Result<T> ValidationFailure(IDictionary<string, string[]> errors)
        => new(false, default, "Validation failed", "VALIDATION_ERROR", errors);

    public new static Result<T> NotFound(string message = "Resource not found")
        => new(false, default, message, "NOT_FOUND");

    public new static Result<T> Unauthorized(string message = "Unauthorized")
        => new(false, default, message, "UNAUTHORIZED");

    public new static Result<T> Forbidden(string message = "Forbidden")
        => new(false, default, message, "FORBIDDEN");

    public new static Result<T> Conflict(string message = "Conflict")
        => new(false, default, message, "CONFLICT");

    public static implicit operator Result<T>(T value) => Success(value);
}
