namespace Framework.Domain.Common.Exceptions;

/// <summary>
/// Base exception for domain-level errors
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "DOMAIN_ERROR")
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, string code, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object? EntityId { get; }

    public EntityNotFoundException(string entityName, object? entityId = null)
        : base($"Entity '{entityName}' with id '{entityId}' was not found.", "ENTITY_NOT_FOUND")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public static EntityNotFoundException For<TEntity>(object? id = null)
        => new(typeof(TEntity).Name, id);
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message)
        : base(message, "BUSINESS_RULE_VIOLATION")
    {
        RuleName = ruleName;
    }
}

/// <summary>
/// Exception thrown when a validation fails
/// </summary>
public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", "VALIDATION_ERROR")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage, "VALIDATION_ERROR")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, [errorMessage] }
        };
    }
}

/// <summary>
/// Exception thrown when there's a conflict (e.g., duplicate entry)
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message, "CONFLICT")
    {
    }
}

/// <summary>
/// Exception thrown when access is forbidden
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message, "FORBIDDEN")
    {
    }
}

/// <summary>
/// Exception thrown when authentication is required
/// </summary>
public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Authentication is required.")
        : base(message, "UNAUTHORIZED")
    {
    }
}
