using Framework.Application.Common.Models;
using Framework.Domain.Email;

namespace Framework.Application.Email;

/// <summary>
/// Service for managing email templates (CRUD operations)
/// </summary>
public interface IEmailTemplateManagementService
{
    /// <summary>
    /// Gets all email templates with pagination
    /// </summary>
    Task<PagedList<EmailTemplateResponse>> GetTemplatesAsync(
        EmailTemplateListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an email template by ID
    /// </summary>
    Task<EmailTemplateResponse?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an email template by name
    /// </summary>
    Task<EmailTemplateResponse?> GetByNameAsync(
        string name,
        string? languageCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new email template
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateEmailTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an email template
    /// </summary>
    Task<Result> UpdateAsync(Guid templateId, UpdateEmailTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an email template
    /// </summary>
    Task<Result> DeleteAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates an email template
    /// </summary>
    Task<Result> ActivateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an email template
    /// </summary>
    Task<Result> DeactivateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets email log entries
    /// </summary>
    Task<PagedList<EmailLogResponse>> GetEmailLogsAsync(
        EmailLogListRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets email log by ID
    /// </summary>
    Task<EmailLogResponse?> GetEmailLogByIdAsync(Guid logId, CancellationToken cancellationToken = default);
}

#region Request DTOs

/// <summary>
/// Email template list request
/// </summary>
public class EmailTemplateListRequest
{
    public string? SearchTerm { get; set; }
    public EmailTemplateType? Type { get; set; }
    public bool? IsActive { get; set; }
    public string? LanguageCode { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Create email template request
/// </summary>
public class CreateEmailTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailTemplateType Type { get; set; }
    public string? Description { get; set; }
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Update email template request
/// </summary>
public class UpdateEmailTemplateRequest
{
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Description { get; set; }
    public string? LanguageCode { get; set; }
}

/// <summary>
/// Email log list request
/// </summary>
public class EmailLogListRequest
{
    public string? SearchTerm { get; set; }
    public EmailStatus? Status { get; set; }
    public Guid? TemplateId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool SortDescending { get; set; } = true;
}

#endregion

#region Response DTOs

/// <summary>
/// Email template response
/// </summary>
public class EmailTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailTemplateType Type { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public string? LanguageCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}

/// <summary>
/// Email log response
/// </summary>
public class EmailLogResponse
{
    public Guid Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public EmailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset SentAt { get; set; }
    public int RetryCount { get; set; }
}

#endregion
