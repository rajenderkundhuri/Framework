using Framework.Application.Common.Models;
using Framework.Application.Email;
using Framework.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Framework.Api.Controllers;

/// <summary>
/// Email template management endpoints
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class EmailTemplatesController : ApiControllerBase
{
    private readonly IEmailTemplateManagementService _emailTemplateService;

    public EmailTemplatesController(IEmailTemplateManagementService emailTemplateService)
    {
        _emailTemplateService = emailTemplateService;
    }

    /// <summary>
    /// Get paginated list of email templates
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedList<EmailTemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates([FromQuery] EmailTemplateListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesView))
            return Forbid();

        var result = await _emailTemplateService.GetTemplatesAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesView))
            return Forbid();

        var template = await _emailTemplateService.GetByIdAsync(id, cancellationToken);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    /// <summary>
    /// Get email template by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(EmailTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateByName(string name, [FromQuery] string? languageCode, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesView))
            return Forbid();

        var template = await _emailTemplateService.GetByNameAsync(name, languageCode, cancellationToken);
        if (template == null)
            return NotFound();

        return Ok(template);
    }

    /// <summary>
    /// Create a new email template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesCreate))
            return Forbid();

        var result = await _emailTemplateService.CreateAsync(request, cancellationToken);
        return HandleCreatedResult(result, nameof(GetTemplate), new { id = result.Value });
    }

    /// <summary>
    /// Update an email template
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesEdit))
            return Forbid();

        var result = await _emailTemplateService.UpdateAsync(id, request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete an email template
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesDelete))
            return Forbid();

        var result = await _emailTemplateService.DeleteAsync(id, cancellationToken);
        return HandleDeleteResult(result);
    }

    /// <summary>
    /// Activate an email template
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateTemplate(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesEdit))
            return Forbid();

        var result = await _emailTemplateService.ActivateAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivate an email template
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTemplate(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailTemplatesEdit))
            return Forbid();

        var result = await _emailTemplateService.DeactivateAsync(id, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated list of email logs
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(PagedList<EmailLogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailLogs([FromQuery] EmailLogListRequest request, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailLogsView))
            return Forbid();

        var result = await _emailTemplateService.GetEmailLogsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get email log by ID
    /// </summary>
    [HttpGet("logs/{id:guid}")]
    [ProducesResponseType(typeof(EmailLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailLog(Guid id, CancellationToken cancellationToken)
    {
        if (!HasPermission(Permissions.EmailLogsView))
            return Forbid();

        var log = await _emailTemplateService.GetEmailLogByIdAsync(id, cancellationToken);
        if (log == null)
            return NotFound();

        return Ok(log);
    }
}
