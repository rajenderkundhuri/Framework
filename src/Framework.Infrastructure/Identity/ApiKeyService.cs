using System.Security.Cryptography;
using System.Text;
using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Domain.Identity;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure.Identity;

/// <summary>
/// Implementation of the API key service
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _context;
    private const string ApiKeyPrefix = "fw_";
    private const int KeyLength = 32;

    public ApiKeyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ApiKeyResponse>> GetUserApiKeysAsync(
        Guid userId,
        ApiKeyListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ApiKeys.AsQueryable();

        // Filter by userId only if not Guid.Empty (Guid.Empty means get all keys)
        if (userId != Guid.Empty)
        {
            query = query.Where(k => k.UserId == userId);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(k => k.IsActive == request.IsActive.Value);
        }

        // Apply sorting
        query = request.SortBy.ToLowerInvariant() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(k => k.Name) : query.OrderBy(k => k.Name),
            "lastused" => request.SortDescending ? query.OrderByDescending(k => k.LastUsedAt) : query.OrderBy(k => k.LastUsedAt),
            _ => request.SortDescending ? query.OrderByDescending(k => k.CreatedAt) : query.OrderBy(k => k.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(k => MapToResponse(k))
            .ToListAsync(cancellationToken);

        return new PagedList<ApiKeyResponse>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<ApiKeyResponse?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        return apiKey != null ? MapToResponse(apiKey) : null;
    }

    public async Task<Result<CreateApiKeyResponse>> CreateAsync(
        Guid userId,
        CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return Result<CreateApiKeyResponse>.Failure("User not found");
        }

        // Generate the API key
        var rawKey = GenerateApiKey();
        var fullKey = ApiKeyPrefix + rawKey;
        var keyHash = HashKey(fullKey);
        var keyPrefix = ApiKeyPrefix + rawKey[..8];

        var apiKey = new ApiKey(
            Guid.NewGuid(),
            userId,
            request.Name,
            keyHash,
            keyPrefix);

        if (request.ExpiresAt.HasValue)
        {
            apiKey.SetExpiration(request.ExpiresAt.Value);
        }

        if (request.Scopes != null && request.Scopes.Any())
        {
            apiKey.SetScopes(string.Join(",", request.Scopes));
        }

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateApiKeyResponse>.Success(new CreateApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            ApiKey = fullKey,
            KeyPrefix = apiKey.KeyPrefix,
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt
        });
    }

    public async Task<Result> UpdateAsync(
        Guid apiKeyId,
        UpdateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            return Result.Failure("API key not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            apiKey.UpdateName(request.Name);
        }

        if (request.ExpiresAt.HasValue)
        {
            apiKey.SetExpiration(request.ExpiresAt.Value);
        }

        if (request.Scopes != null)
        {
            apiKey.SetScopes(request.Scopes.Any() ? string.Join(",", request.Scopes) : null);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RevokeAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            return Result.Failure("API key not found");
        }

        apiKey.Revoke();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey == null)
        {
            return Result.Failure("API key not found");
        }

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ApiKeyValidationResult>> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(apiKey) || !apiKey.StartsWith(ApiKeyPrefix))
        {
            return Result<ApiKeyValidationResult>.Failure("Invalid API key format");
        }

        var keyHash = HashKey(apiKey);

        var storedKey = await _context.ApiKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (storedKey == null)
        {
            return Result<ApiKeyValidationResult>.Failure("Invalid API key");
        }

        if (!storedKey.IsValid)
        {
            return Result<ApiKeyValidationResult>.Failure("API key is expired or revoked");
        }

        var scopes = string.IsNullOrEmpty(storedKey.Scopes)
            ? new List<string>()
            : storedKey.Scopes.Split(',').ToList();

        return Result<ApiKeyValidationResult>.Success(new ApiKeyValidationResult
        {
            ApiKeyId = storedKey.Id,
            UserId = storedKey.UserId,
            UserEmail = storedKey.User?.Email ?? string.Empty,
            Scopes = scopes
        });
    }

    public async Task RecordUsageAsync(Guid apiKeyId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, cancellationToken);

        if (apiKey != null)
        {
            apiKey.RecordUsage(ipAddress);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateApiKey()
    {
        var bytes = new byte[KeyLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            [..KeyLength];
    }

    private static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hashBytes);
    }

    private static ApiKeyResponse MapToResponse(ApiKey apiKey)
    {
        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            UserId = apiKey.UserId,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            IsActive = apiKey.IsActive,
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            LastUsedIp = apiKey.LastUsedIp,
            Scopes = string.IsNullOrEmpty(apiKey.Scopes)
                ? new List<string>()
                : apiKey.Scopes.Split(',').ToList()
        };
    }
}
