namespace Framework.Application.Email;

/// <summary>
/// Interface for email template rendering
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders an email template with the provided model
    /// </summary>
    /// <typeparam name="TModel">Type of the model</typeparam>
    /// <param name="templateName">Name of the template</param>
    /// <param name="model">Data model for the template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rendered email content</returns>
    Task<RenderedEmail> RenderAsync<TModel>(
        string templateName,
        TModel model,
        CancellationToken cancellationToken = default) where TModel : class;

    /// <summary>
    /// Checks if a template exists
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <returns>True if template exists</returns>
    bool TemplateExists(string templateName);

    /// <summary>
    /// Gets available template names
    /// </summary>
    /// <returns>List of template names</returns>
    IEnumerable<string> GetTemplateNames();
}

/// <summary>
/// Rendered email content
/// </summary>
public class RenderedEmail
{
    /// <summary>
    /// Rendered subject line
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Rendered HTML body
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// Rendered plain text body
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// Template name used
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;
}

/// <summary>
/// Base class for email template models
/// </summary>
public abstract class EmailTemplateModel
{
    /// <summary>
    /// Application name
    /// </summary>
    public string ApplicationName { get; set; } = "Application";

    /// <summary>
    /// Application URL
    /// </summary>
    public string ApplicationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Support email
    /// </summary>
    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>
    /// Current year for copyright
    /// </summary>
    public int CurrentYear => DateTime.UtcNow.Year;
}

/// <summary>
/// Model for welcome emails
/// </summary>
public class WelcomeEmailModel : EmailTemplateModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ActivationLink { get; set; }
}

/// <summary>
/// Model for password reset emails
/// </summary>
public class PasswordResetEmailModel : EmailTemplateModel
{
    public string UserName { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
}

/// <summary>
/// Model for notification emails
/// </summary>
public class NotificationEmailModel : EmailTemplateModel
{
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
