using Framework.Domain.Common.Entities;

namespace Framework.Domain.Email;

/// <summary>
/// Email template entity for customizable email notifications
/// </summary>
public class EmailTemplate : AuditableEntity
{
    private EmailTemplate() : base() { }

    public EmailTemplate(Guid id, string name, string subject, string body, EmailTemplateType type)
        : base(id)
    {
        Name = name;
        Subject = subject;
        Body = body;
        Type = type;
        IsActive = true;
    }

    /// <summary>
    /// Template name/identifier (e.g., "WelcomeEmail", "PasswordReset")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Email subject template (supports placeholders like {{UserName}})
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Email body template (HTML, supports placeholders)
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Template type
    /// </summary>
    public EmailTemplateType Type { get; private set; }

    /// <summary>
    /// Whether the template is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Optional description of the template
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Language code (e.g., "en", "es") - null for default
    /// </summary>
    public string? LanguageCode { get; private set; }

    /// <summary>
    /// Update the template content
    /// </summary>
    public void Update(string subject, string body, string? description = null)
    {
        Subject = subject;
        Body = body;
        Description = description;
    }

    /// <summary>
    /// Set the language for this template
    /// </summary>
    public void SetLanguage(string? languageCode)
    {
        LanguageCode = languageCode;
    }

    /// <summary>
    /// Activate the template
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Deactivate the template
    /// </summary>
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Types of email templates
/// </summary>
public enum EmailTemplateType
{
    Welcome = 0,
    PasswordReset = 1,
    EmailConfirmation = 2,
    TwoFactorCode = 3,
    AccountLocked = 4,
    PasswordChanged = 5,
    InviteUser = 6,
    Custom = 99
}
