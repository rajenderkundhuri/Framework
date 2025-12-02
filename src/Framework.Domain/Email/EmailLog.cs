using Framework.Domain.Common.Entities;

namespace Framework.Domain.Email;

/// <summary>
/// Log of sent emails for tracking and debugging
/// </summary>
public class EmailLog : Entity<Guid>
{
    private EmailLog() : base() { }

    public EmailLog(Guid id, string to, string subject, string body, Guid? templateId = null)
        : base(id)
    {
        To = to;
        Subject = subject;
        Body = body;
        TemplateId = templateId;
        SentAt = DateTimeOffset.UtcNow;
        Status = EmailStatus.Pending;
    }

    /// <summary>
    /// Recipient email address
    /// </summary>
    public string To { get; private set; } = string.Empty;

    /// <summary>
    /// CC recipients (comma-separated)
    /// </summary>
    public string? Cc { get; private set; }

    /// <summary>
    /// BCC recipients (comma-separated)
    /// </summary>
    public string? Bcc { get; private set; }

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    /// <summary>
    /// Email body (HTML)
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Template ID if generated from template
    /// </summary>
    public Guid? TemplateId { get; private set; }

    /// <summary>
    /// Status of the email
    /// </summary>
    public EmailStatus Status { get; private set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// When the email was sent/attempted
    /// </summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Mark as sent successfully
    /// </summary>
    public void MarkAsSent()
    {
        Status = EmailStatus.Sent;
        ErrorMessage = null;
    }

    /// <summary>
    /// Mark as failed with error
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
    }

    /// <summary>
    /// Set CC recipients
    /// </summary>
    public void SetCc(string? cc) => Cc = cc;

    /// <summary>
    /// Set BCC recipients
    /// </summary>
    public void SetBcc(string? bcc) => Bcc = bcc;
}

/// <summary>
/// Email sending status
/// </summary>
public enum EmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Retry = 3
}
