using Framework.Application.Email;
using Shouldly;

namespace Framework.Application.Tests.Email;

public class EmailSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeEmail()
    {
        // Assert
        EmailSettings.SectionName.ShouldBe("Email");
    }

    [Fact]
    public void DefaultValues_ShouldBeSet()
    {
        // Act
        var settings = new EmailSettings();

        // Assert
        settings.IsEnabled.ShouldBeTrue();
        settings.DefaultFromEmail.ShouldBe(string.Empty);
        settings.DefaultFromName.ShouldBe(string.Empty);
        settings.SmtpHost.ShouldBe("localhost");
        settings.SmtpPort.ShouldBe(25);
        settings.UseSsl.ShouldBeFalse();
        settings.SmtpUsername.ShouldBeNull();
        settings.SmtpPassword.ShouldBeNull();
        settings.TimeoutSeconds.ShouldBe(30);
        settings.TemplatesPath.ShouldBe("Templates/Email");
        settings.LogEmailContent.ShouldBeFalse();
        settings.Provider.ShouldBe(EmailProvider.Smtp);
        settings.SendGridApiKey.ShouldBeNull();
        settings.AwsSesRegion.ShouldBeNull();
        settings.RedirectAllEmailsTo.ShouldBeNull();
        settings.MaxBatchSize.ShouldBe(100);
        settings.BatchDelayMs.ShouldBe(100);
    }

    [Fact]
    public void AllProperties_ShouldBeSettable()
    {
        // Act
        var settings = new EmailSettings
        {
            IsEnabled = false,
            DefaultFromEmail = "noreply@example.com",
            DefaultFromName = "No Reply",
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            UseSsl = true,
            SmtpUsername = "user@example.com",
            SmtpPassword = "password123",
            TimeoutSeconds = 60,
            TemplatesPath = "/custom/templates",
            LogEmailContent = true,
            Provider = EmailProvider.SendGrid,
            SendGridApiKey = "SG.xxxxx",
            AwsSesRegion = "us-east-1",
            RedirectAllEmailsTo = "dev@example.com",
            MaxBatchSize = 50,
            BatchDelayMs = 200
        };

        // Assert
        settings.IsEnabled.ShouldBeFalse();
        settings.DefaultFromEmail.ShouldBe("noreply@example.com");
        settings.DefaultFromName.ShouldBe("No Reply");
        settings.SmtpHost.ShouldBe("smtp.example.com");
        settings.SmtpPort.ShouldBe(587);
        settings.UseSsl.ShouldBeTrue();
        settings.SmtpUsername.ShouldBe("user@example.com");
        settings.SmtpPassword.ShouldBe("password123");
        settings.TimeoutSeconds.ShouldBe(60);
        settings.TemplatesPath.ShouldBe("/custom/templates");
        settings.LogEmailContent.ShouldBeTrue();
        settings.Provider.ShouldBe(EmailProvider.SendGrid);
        settings.SendGridApiKey.ShouldBe("SG.xxxxx");
        settings.AwsSesRegion.ShouldBe("us-east-1");
        settings.RedirectAllEmailsTo.ShouldBe("dev@example.com");
        settings.MaxBatchSize.ShouldBe(50);
        settings.BatchDelayMs.ShouldBe(200);
    }
}

public class EmailProviderTests
{
    [Fact]
    public void EmailProvider_ShouldHaveExpectedValues()
    {
        // Assert
        ((int)EmailProvider.Smtp).ShouldBe(0);
        ((int)EmailProvider.SendGrid).ShouldBe(1);
        ((int)EmailProvider.AwsSes).ShouldBe(2);
        ((int)EmailProvider.Mailgun).ShouldBe(3);
        ((int)EmailProvider.FileLog).ShouldBe(99);
    }

    [Fact]
    public void EmailProvider_ShouldHaveAllProviders()
    {
        // Act
        var providers = Enum.GetValues<EmailProvider>();

        // Assert
        providers.Length.ShouldBe(5);
        providers.ShouldContain(EmailProvider.Smtp);
        providers.ShouldContain(EmailProvider.SendGrid);
        providers.ShouldContain(EmailProvider.AwsSes);
        providers.ShouldContain(EmailProvider.Mailgun);
        providers.ShouldContain(EmailProvider.FileLog);
    }
}
