using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// API key management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ApiKeysController : ApiControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Get current user's API keys
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedList<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApiKeys([FromQuery] ApiKeyListRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _apiKeyService.GetUserApiKeysAsync(userId.Value, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all API keys (admin)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ApiKeyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllApiKeys([FromQuery] ApiKeyListRequest request, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ApiKeysView))
            return Forbid();

        if (userId.HasValue)
        {
            var result = await _apiKeyService.GetUserApiKeysAsync(userId.Value, request, cancellationToken);
            return Ok(result);
        }

        // Return all API keys
        var allKeys = await _apiKeyService.GetUserApiKeysAsync(Guid.Empty, request, cancellationToken);
        return Ok(allKeys);
    }

    /// <summary>
    /// Get API key by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApiKey(Guid id, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyService.GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return NotFound();

        // Check if user owns this key or has permission
        var userId = GetCurrentUserId();
        if (apiKey.UserId != userId && !HasPermission(Permissions.ApiKeysView))
            return Forbid();

        return Ok(apiKey);
    }

    /// <summary>
    /// Create a new API key for current user
    /// </summary>
    [HttpPost("my")]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMyApiKey([FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _apiKeyService.CreateAsync(userId.Value, request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetApiKey), new { id = result.Value!.Id }, result.Value);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new API key for a user (admin)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateApiKey([FromQuery] Guid userId, [FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.ApiKeysCreate))
            return Forbid();

        var result = await _apiKeyService.CreateAsync(userId, request, cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetApiKey), new { id = result.Value!.Id }, result.Value);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Update an API key
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] UpdateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyService.GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return NotFound();

        // Check if user owns this key or has permission
        var userId = GetCurrentUserId();
        if (apiKey.UserId != userId && !HasPermission(Permissions.ApiKeysEdit))
            return Forbid();

        var result = await _apiKeyService.UpdateAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Revoke an API key
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyService.GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return NotFound();

        // Check if user owns this key or has permission
        var userId = GetCurrentUserId();
        if (apiKey.UserId != userId && !HasPermission(Permissions.ApiKeysDelete))
            return Forbid();

        var result = await _apiKeyService.RevokeAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete an API key
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteApiKey(Guid id, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyService.GetByIdAsync(id, cancellationToken);
        if (apiKey == null)
            return NotFound();

        // Check if user owns this key or has permission
        var userId = GetCurrentUserId();
        if (apiKey.UserId != userId && !HasPermission(Permissions.ApiKeysDelete))
            return Forbid();

        var result = await _apiKeyService.DeleteAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Validate an API key (for authentication middleware)
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiKeyValidationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateApiKey([FromBody] ValidateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var result = await _apiKeyService.ValidateAsync(request.ApiKey, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Request to validate an API key
/// </summary>
public record ValidateApiKeyRequest
{
    public string ApiKey { get; init; } = string.Empty;
}
