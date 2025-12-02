using Framework.Application.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Infrastructure.Email;

/// <summary>
/// Extension methods for configuring email services
/// </summary>
public static class EmailExtensions
{
    /// <summary>
    /// Adds email services to the service collection
    /// </summary>
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(EmailSettings.SectionName)
            .Get<EmailSettings>() ?? new EmailSettings();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        switch (settings.Provider)
        {
            case EmailProvider.Smtp:
                services.AddScoped<IEmailService, SmtpEmailService>();
                break;

            case EmailProvider.SendGrid:
                // Note: SendGrid package required
                throw new NotSupportedException(
                    "SendGrid email provider requires SendGrid package. " +
                    "Use SMTP or FileLog for development.");

            case EmailProvider.AwsSes:
                // Note: AWSSDK.SimpleEmail package required
                throw new NotSupportedException(
                    "AWS SES email provider requires AWSSDK.SimpleEmail package. " +
                    "Use SMTP or FileLog for development.");

            case EmailProvider.Mailgun:
                // Note: Mailgun package required
                throw new NotSupportedException(
                    "Mailgun email provider requires RestSharp package. " +
                    "Use SMTP or FileLog for development.");

            case EmailProvider.FileLog:
                services.AddScoped<IEmailService, FileLogEmailService>();
                break;

            default:
                services.AddScoped<IEmailService, SmtpEmailService>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds email services with template support
    /// </summary>
    public static IServiceCollection AddEmailWithTemplates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEmail(configuration);
        services.AddScoped<IEmailTemplateService, FileEmailTemplateService>();
        return services;
    }

    /// <summary>
    /// Adds file-based email logging for development/testing
    /// </summary>
    public static IServiceCollection AddFileLogEmail(
        this IServiceCollection services,
        string? outputPath = null)
    {
        services.Configure<EmailSettings>(options =>
        {
            options.Provider = EmailProvider.FileLog;
            if (!string.IsNullOrEmpty(outputPath))
            {
                options.TemplatesPath = outputPath;
            }
        });
        services.AddScoped<IEmailService, FileLogEmailService>();
        return services;
    }
}
