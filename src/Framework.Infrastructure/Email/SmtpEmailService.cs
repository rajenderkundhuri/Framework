using System.Net;
using System.Net.Mail;
using Framework.Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.Email;

/// <summary>
/// SMTP-based email service implementation
/// </summary>
public class SmtpEmailService : IEmailService, IDisposable
{
    private readonly EmailSettings _settings;
    private readonly IEmailTemplateService? _templateService;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpClient _smtpClient;
    private bool _disposed;

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailService> logger,
        IEmailTemplateService? templateService = null)
    {
        _settings = settings.Value;
        _logger = logger;
        _templateService = templateService;

        _smtpClient = CreateSmtpClient();
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
            var mailMessage = CreateMailMessage(message);
            await _smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipients}",
                string.Join(", ", message.To.Select(t => t.Email)));

            return EmailResult.Success(messageId: Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}",
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
        var messageList = messages.ToList();

        for (var i = 0; i < messageList.Count; i += _settings.MaxBatchSize)
        {
            var batch = messageList.Skip(i).Take(_settings.MaxBatchSize);

            foreach (var message in batch)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = await SendAsync(message, cancellationToken);
                results.Add(result);

                if (_settings.BatchDelayMs > 0)
                {
                    await Task.Delay(_settings.BatchDelayMs, cancellationToken);
                }
            }
        }

        return results;
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.UseSsl,
            Timeout = _settings.TimeoutSeconds * 1000
        };

        if (!string.IsNullOrEmpty(_settings.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(
                _settings.SmtpUsername,
                _settings.SmtpPassword);
        }

        return client;
    }

    private MailMessage CreateMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage();

        // Set From
        var from = message.From ?? new EmailAddress(_settings.DefaultFromEmail, _settings.DefaultFromName);
        mailMessage.From = new MailAddress(from.Email, from.Name);

        // Add recipients (with possible redirect for testing)
        if (!string.IsNullOrEmpty(_settings.RedirectAllEmailsTo))
        {
            mailMessage.To.Add(new MailAddress(_settings.RedirectAllEmailsTo));
            mailMessage.Subject = $"[Redirected: {string.Join(", ", message.To.Select(t => t.Email))}] {message.Subject}";
        }
        else
        {
            foreach (var to in message.To)
            {
                mailMessage.To.Add(new MailAddress(to.Email, to.Name));
            }

            foreach (var cc in message.Cc)
            {
                mailMessage.CC.Add(new MailAddress(cc.Email, cc.Name));
            }

            foreach (var bcc in message.Bcc)
            {
                mailMessage.Bcc.Add(new MailAddress(bcc.Email, bcc.Name));
            }

            mailMessage.Subject = message.Subject;
        }

        // Set Reply-To
        if (message.ReplyTo != null)
        {
            mailMessage.ReplyToList.Add(new MailAddress(message.ReplyTo.Email, message.ReplyTo.Name));
        }

        // Set body
        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            mailMessage.Body = message.HtmlBody;
            mailMessage.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(message.TextBody))
            {
                var alternateView = AlternateView.CreateAlternateViewFromString(
                    message.TextBody,
                    null,
                    "text/plain");
                mailMessage.AlternateViews.Add(alternateView);
            }
        }
        else if (!string.IsNullOrEmpty(message.TextBody))
        {
            mailMessage.Body = message.TextBody;
            mailMessage.IsBodyHtml = false;
        }

        // Set priority
        mailMessage.Priority = message.Priority switch
        {
            EmailPriority.Low => MailPriority.Low,
            EmailPriority.High => MailPriority.High,
            _ => MailPriority.Normal
        };

        // Add attachments
        foreach (var attachment in message.Attachments)
        {
            var stream = new MemoryStream(attachment.Content);
            var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);

            if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
            {
                mailAttachment.ContentDisposition!.Inline = true;
                mailAttachment.ContentId = attachment.ContentId;
            }

            mailMessage.Attachments.Add(mailAttachment);
        }

        // Add custom headers
        foreach (var header in message.Headers)
        {
            mailMessage.Headers.Add(header.Key, header.Value);
        }

        return mailMessage;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _smtpClient.Dispose();
        _disposed = true;
    }
}
