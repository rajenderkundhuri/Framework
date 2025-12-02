namespace Framework.Application.Email;

/// <summary>
/// Interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    /// <param name="message">Email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="to">Recipient email address</param>
    /// <param name="model">Data model for the template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the send operation</returns>
    Task<EmailResult> SendTemplateAsync<TModel>(
        string templateName,
        string to,
        TModel model,
        CancellationToken cancellationToken = default) where TModel : class;

    /// <summary>
    /// Sends multiple emails in batch
    /// </summary>
    /// <param name="messages">Email messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results for each message</returns>
    Task<IEnumerable<EmailResult>> SendBatchAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an email message
/// </summary>
public class EmailMessage
{
    /// <summary>
    /// Recipient email addresses (To)
    /// </summary>
    public List<EmailAddress> To { get; set; } = new();

    /// <summary>
    /// CC recipients
    /// </summary>
    public List<EmailAddress> Cc { get; set; } = new();

    /// <summary>
    /// BCC recipients
    /// </summary>
    public List<EmailAddress> Bcc { get; set; } = new();

    /// <summary>
    /// Sender email address
    /// </summary>
    public EmailAddress? From { get; set; }

    /// <summary>
    /// Reply-to email address
    /// </summary>
    public EmailAddress? ReplyTo { get; set; }

    /// <summary>
    /// Email subject
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Plain text body
    /// </summary>
    public string? TextBody { get; set; }

    /// <summary>
    /// HTML body
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// File attachments
    /// </summary>
    public List<EmailAttachment> Attachments { get; set; } = new();

    /// <summary>
    /// Custom headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Priority of the email
    /// </summary>
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    /// <summary>
    /// Tags for tracking/categorization
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Creates a simple email message
    /// </summary>
    public static EmailMessage Create(string to, string subject, string body, bool isHtml = true)
    {
        var message = new EmailMessage
        {
            Subject = subject
        };

        message.To.Add(new EmailAddress(to));

        if (isHtml)
            message.HtmlBody = body;
        else
            message.TextBody = body;

        return message;
    }
}

/// <summary>
/// Represents an email address
/// </summary>
public class EmailAddress
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }

    public EmailAddress() { }

    public EmailAddress(string email, string? name = null)
    {
        Email = email;
        Name = name;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? Email : $"{Name} <{Email}>";
    }
}

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; }
    public string? ContentId { get; set; }

    public static EmailAttachment FromBytes(string fileName, byte[] content, string? contentType = null)
    {
        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType ?? "application/octet-stream"
        };
    }

    public static async Task<EmailAttachment> FromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        return new EmailAttachment
        {
            FileName = fileName,
            Content = content,
            ContentType = contentType
        };
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

/// <summary>
/// Result of an email send operation
/// </summary>
public class EmailResult
{
    public bool IsSuccess { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public EmailAddress? Recipient { get; set; }

    public static EmailResult Success(string? messageId = null, EmailAddress? recipient = null)
    {
        return new EmailResult
        {
            IsSuccess = true,
            MessageId = messageId,
            Recipient = recipient
        };
    }

    public static EmailResult Failure(string errorMessage, string? errorCode = null, EmailAddress? recipient = null)
    {
        return new EmailResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Recipient = recipient
        };
    }
}
