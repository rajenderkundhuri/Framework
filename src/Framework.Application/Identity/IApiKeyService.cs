using Framework.Application.Common.Models;

namespace Framework.Application.Identity;

/// <summary>
/// Service for API key management
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Gets API keys for a user
    /// </summary>
    Task<PagedList<ApiKeyResponse>> GetUserApiKeysAsync(
        Guid userId,
        ApiKeyListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API key by ID
    /// </summary>
    Task<ApiKeyResponse?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key
    /// </summary>
    Task<Result<CreateApiKeyResponse>> CreateAsync(
        Guid userId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an API key
    /// </summary>
    Task<Result> UpdateAsync(
        Guid apiKeyId,
        UpdateApiKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task<Result> RevokeAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an API key
    /// </summary>
    Task<Result> DeleteAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an API key and returns associated user info
    /// </summary>
    Task<Result<ApiKeyValidationResult>> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records API key usage
    /// </summary>
    Task RecordUsageAsync(Guid apiKeyId, string? ipAddress = null, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// API key list request
/// </summary>
public class ApiKeyListRequest
{
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Create API key request
/// </summary>
public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public List<string>? Scopes { get; set; }
}

/// <summary>
/// Update API key request
/// </summary>
public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public List<string>? Scopes { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// API key response
/// </summary>
public class ApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public List<string> Scopes { get; set; } = new();
}

/// <summary>
/// Create API key response
/// </summary>
public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full API key - only returned once during creation
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

/// <summary>
/// API key validation result
/// </summary>
public class ApiKeyValidationResult
{
    public Guid ApiKeyId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new();
}

#endregion
