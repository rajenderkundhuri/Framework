using System.Text;
using System.Text.Json;
using Framework.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Email;

/// <summary>
/// File-based email service for development and testing
/// Logs emails to files instead of sending them
/// </summary>
public class FileLogEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IEmailTemplateService? _templateService;
    private readonly ILogger<FileLogEmailService> _logger;
    private readonly string _outputPath;

    public FileLogEmailService(
        IOptions<EmailSettings> settings,
        ILogger<FileLogEmailService> logger,
        IEmailTemplateService? templateService = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _templateService = templateService;
        _outputPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Framework",
            "Emails");
    }

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_settings.IsEnabled)
        {
            _logger.LogWarning("Email sending is disabled. Skipping email to {Recipients}",
                string.Join(", ", message.To.Select(t => t.Email)));
            return EmailResult.Success();
        }

        try
        {
            EnsureOutputDirectoryExists();

            var messageId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;
            var fileName = $"{timestamp:yyyyMMdd_HHmmss}_{messageId[..8]}.eml";
            var filePath = Path.Combine(_outputPath, fileName);

            var emailContent = BuildEmailContent(message, messageId, timestamp);
            await File.WriteAllTextAsync(filePath, emailContent, cancellationToken);

            // Also save metadata as JSON for easy parsing
            var metadataPath = Path.Combine(_outputPath, $"{timestamp:yyyyMMdd_HHmmss}_{messageId[..8]}.json");
            var metadata = new EmailLogMetadata
            {
                MessageId = messageId,
                Timestamp = timestamp,
                From = message.From?.ToString() ?? $"{_settings.DefaultFromName} <{_settings.DefaultFromEmail}>",
                To = message.To.Select(t => t.ToString()).ToList(),
                Cc = message.Cc.Select(c => c.ToString()).ToList(),
                Bcc = message.Bcc.Select(b => b.ToString()).ToList(),
                Subject = message.Subject,
                HasHtmlBody = !string.IsNullOrEmpty(message.HtmlBody),
                HasTextBody = !string.IsNullOrEmpty(message.TextBody),
                AttachmentCount = message.Attachments.Count,
                Priority = message.Priority.ToString(),
                Headers = message.Headers,
                Tags = message.Tags
            };

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, jsonOptions), cancellationToken);

            _logger.LogInformation(
                "Email logged to file: {FilePath} | To: {Recipients} | Subject: {Subject}",
                filePath,
                string.Join(", ", message.To.Select(t => t.Email)),
                message.Subject);

            return EmailResult.Success(messageId: messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email to file for {Recipients}",
                string.Join(", ", message.To.Select(t => t.Email)));

            return EmailResult.Failure(ex.Message);
        }
    }

    public async Task<EmailResult> SendTemplateAsync<TModel>(
        string templateName,
        string to,
        TModel model,
        CancellationToken cancellationToken = default) where TModel : class
    {
        if (_templateService == null)
        {
            return EmailResult.Failure("Template service not configured");
        }

        try
        {
            var rendered = await _templateService.RenderAsync(templateName, model, cancellationToken);

            var message = new EmailMessage
            {
                Subject = rendered.Subject,
                HtmlBody = rendered.HtmlBody,
                TextBody = rendered.TextBody
            };
            message.To.Add(new EmailAddress(to));
            message.Tags.Add($"template:{templateName}");

            return await SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {TemplateName}", templateName);
            return EmailResult.Failure($"Failed to render template: {ex.Message}");
        }
    }

    public async Task<IEnumerable<EmailResult>> SendBatchAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EmailResult>();

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await SendAsync(message, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Gets the output path where emails are logged
    /// </summary>
    public string GetOutputPath() => _outputPath;

    /// <summary>
    /// Gets all logged emails
    /// </summary>
    public IEnumerable<string> GetLoggedEmails()
    {
        if (!Directory.Exists(_outputPath))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(_outputPath, "*.eml")
            .OrderByDescending(f => f);
    }

    /// <summary>
    /// Gets logged email metadata
    /// </summary>
    public async Task<IEnumerable<EmailLogMetadata>> GetLoggedEmailMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_outputPath))
            return Enumerable.Empty<EmailLogMetadata>();

        var results = new List<EmailLogMetadata>();
        var jsonFiles = Directory.GetFiles(_outputPath, "*.json")
            .OrderByDescending(f => f);

        foreach (var file in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var metadata = JsonSerializer.Deserialize<EmailLogMetadata>(json);
                if (metadata != null)
                    results.Add(metadata);
            }
            catch
            {
                // Skip invalid files
            }
        }

        return results;
    }

    /// <summary>
    /// Clears all logged emails
    /// </summary>
    public void ClearLoggedEmails()
    {
        if (!Directory.Exists(_outputPath))
            return;

        foreach (var file in Directory.GetFiles(_outputPath))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    private void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
        }
    }

    private string BuildEmailContent(EmailMessage message, string messageId, DateTime timestamp)
    {
        var sb = new StringBuilder();

        // Headers
        var from = message.From ?? new EmailAddress(_settings.DefaultFromEmail, _settings.DefaultFromName);
        sb.AppendLine($"Message-ID: <{messageId}>");
        sb.AppendLine($"Date: {timestamp:R}");
        sb.AppendLine($"From: {from}");
        sb.AppendLine($"To: {string.Join(", ", message.To.Select(t => t.ToString()))}");

        if (message.Cc.Any())
            sb.AppendLine($"Cc: {string.Join(", ", message.Cc.Select(c => c.ToString()))}");

        if (message.Bcc.Any())
            sb.AppendLine($"Bcc: {string.Join(", ", message.Bcc.Select(b => b.ToString()))}");

        if (message.ReplyTo != null)
            sb.AppendLine($"Reply-To: {message.ReplyTo}");

        sb.AppendLine($"Subject: {message.Subject}");

        var priority = message.Priority switch
        {
            EmailPriority.High => "1 (Highest)",
            EmailPriority.Low => "5 (Lowest)",
            _ => "3 (Normal)"
        };
        sb.AppendLine($"X-Priority: {priority}");

        foreach (var header in message.Headers)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }

        sb.AppendLine($"MIME-Version: 1.0");

        // Body
        if (!string.IsNullOrEmpty(message.HtmlBody) && !string.IsNullOrEmpty(message.TextBody))
        {
            var boundary = $"----=_Part_{messageId[..8]}";
            sb.AppendLine($"Content-Type: multipart/alternative; boundary=\"{boundary}\"");
            sb.AppendLine();
            sb.AppendLine($"--{boundary}");
            sb.AppendLine("Content-Type: text/plain; charset=utf-8");
            sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
            sb.AppendLine();
            sb.AppendLine(message.TextBody);
            sb.AppendLine();
            sb.AppendLine($"--{boundary}");
            sb.AppendLine("Content-Type: text/html; charset=utf-8");
            sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
            sb.AppendLine();
            sb.AppendLine(message.HtmlBody);
            sb.AppendLine();
            sb.AppendLine($"--{boundary}--");
        }
        else if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            sb.AppendLine("Content-Type: text/html; charset=utf-8");
            sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
            sb.AppendLine();
            sb.AppendLine(message.HtmlBody);
        }
        else if (!string.IsNullOrEmpty(message.TextBody))
        {
            sb.AppendLine("Content-Type: text/plain; charset=utf-8");
            sb.AppendLine("Content-Transfer-Encoding: quoted-printable");
            sb.AppendLine();
            sb.AppendLine(message.TextBody);
        }

        // Attachment info (not actual content for file log)
        if (message.Attachments.Any())
        {
            sb.AppendLine();
            sb.AppendLine("--- ATTACHMENTS ---");
            foreach (var attachment in message.Attachments)
            {
                sb.AppendLine($"  - {attachment.FileName} ({attachment.ContentType}, {attachment.Content.Length} bytes)");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Metadata for logged emails
/// </summary>
public class EmailLogMetadata
{
    public string MessageId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string From { get; set; } = string.Empty;
    public List<string> To { get; set; } = new();
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public bool HasHtmlBody { get; set; }
    public bool HasTextBody { get; set; }
    public int AttachmentCount { get; set; }
    public string Priority { get; set; } = "Normal";
    public Dictionary<string, string> Headers { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
