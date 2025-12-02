namespace Framework.Application.Email;

/// <summary>
/// Configuration settings for email services
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Whether email sending is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Default sender email address
    /// </summary>
    public string DefaultFromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender name
    /// </summary>
    public string DefaultFromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP host
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// SMTP port
    /// </summary>
    public int SmtpPort { get; set; } = 25;

    /// <summary>
    /// Whether to use SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// SMTP username (if authentication required)
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password (if authentication required)
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Timeout in seconds for SMTP operations
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Path to email templates
    /// </summary>
    public string TemplatesPath { get; set; } = "Templates/Email";

    /// <summary>
    /// Whether to log email content
    /// </summary>
    public bool LogEmailContent { get; set; } = false;

    /// <summary>
    /// Email provider type
    /// </summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

    /// <summary>
    /// SendGrid API key (when using SendGrid provider)
    /// </summary>
    public string? SendGridApiKey { get; set; }

    /// <summary>
    /// AWS SES region (when using AWS SES provider)
    /// </summary>
    public string? AwsSesRegion { get; set; }

    /// <summary>
    /// Override all emails to this address (for testing)
    /// </summary>
    public string? RedirectAllEmailsTo { get; set; }

    /// <summary>
    /// Maximum batch size for bulk sends
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Delay between batch sends in milliseconds
    /// </summary>
    public int BatchDelayMs { get; set; } = 100;
}

/// <summary>
/// Email provider types
/// </summary>
public enum EmailProvider
{
    /// <summary>
    /// Standard SMTP
    /// </summary>
    Smtp = 0,

    /// <summary>
    /// SendGrid
    /// </summary>
    SendGrid = 1,

    /// <summary>
    /// Amazon SES
    /// </summary>
    AwsSes = 2,

    /// <summary>
    /// Mailgun
    /// </summary>
    Mailgun = 3,

    /// <summary>
    /// Log only (no actual sending)
    /// </summary>
    FileLog = 99
}
