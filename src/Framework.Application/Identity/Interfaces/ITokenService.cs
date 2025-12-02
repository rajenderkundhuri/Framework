using Framework.Application.Common.Models;
using Framework.Application.Identity.Models;

namespace Framework.Application.Identity.Interfaces;

/// <summary>
/// JWT token service interface
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate access and refresh tokens for a user
    /// </summary>
    Task<Result<TokenResponse>> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh an expired access token using a valid refresh token
    /// </summary>
    Task<Result<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a user's refresh token
    /// </summary>
    Task<Result> RevokeTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an access token
    /// </summary>
    Task<Result<Guid>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
