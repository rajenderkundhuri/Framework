using Framework.Application.Common.Models;
using Framework.Application.Identity;
using Framework.Application.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Localization resource management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ResourcesController : ApiControllerBase
{
    private readonly IResourceManagementService _resourceManagementService;

    public ResourcesController(IResourceManagementService resourceManagementService)
    {
        _resourceManagementService = resourceManagementService;
    }

    /// <summary>
    /// Get paginated list of resources
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ResourceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResources([FromQuery] ResourceListRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetResourcesAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get resource by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResource(Guid id, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetResourceByIdAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get resource by key and culture
    /// </summary>
    [HttpGet("by-key")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResourceByKey([FromQuery] string key, [FromQuery] string cultureName, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetResourceAsync(key, cultureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new resource
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateResource([FromBody] CreateResourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.CreateResourceAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetResource), new { id = result.Value });
    }

    /// <summary>
    /// Update a resource
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateResource(Guid id, [FromBody] UpdateResourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.UpdateResourceAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a resource
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteResource(Guid id, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.DeleteResourceAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Import resources
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(typeof(BulkResourceResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportResources([FromBody] List<ImportResourceRequest> resources, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.ImportResourcesAsync(resources, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Export resources
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(IEnumerable<ExportResourceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportResources([FromQuery] ExportResourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.ExportResourcesAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get resource groups
    /// </summary>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceGroups(CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetResourceGroupsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get resources by group
    /// </summary>
    [HttpGet("groups/{group}")]
    [ProducesResponseType(typeof(IEnumerable<ResourceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourcesByGroup(string group, [FromQuery] string? cultureName, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetResourcesByGroupAsync(group, cultureName, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get localization statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(LocalizationStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetStatisticsAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get missing resources for a culture
    /// </summary>
    [HttpGet("missing/{cultureName}")]
    [ProducesResponseType(typeof(IEnumerable<MissingResourceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMissingResources(string cultureName, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetMissingResourcesAsync(cultureName, cancellationToken);
        return HandleResult(result);
    }
}

/// <summary>
/// Language management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class LanguagesController : ApiControllerBase
{
    private readonly IResourceManagementService _resourceManagementService;

    public LanguagesController(IResourceManagementService resourceManagementService)
    {
        _resourceManagementService = resourceManagementService;
    }

    /// <summary>
    /// Get all supported languages
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<LanguageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.GetLanguagesAsync(cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Add a new language
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLanguage([FromBody] AddLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.AddLanguageAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update a language
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.UpdateLanguageAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Set default language
    /// </summary>
    [HttpPost("{id:guid}/set-default")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultLanguage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.SetDefaultLanguageAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enable a language
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableLanguage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.EnableLanguageAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Disable a language
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DisableLanguage(Guid id, CancellationToken cancellationToken)
    {
        var result = await _resourceManagementService.DisableLanguageAsync(id, cancellationToken);
        return HandleResult(result);
    }
}
