using Framework.Application.Email;
using Framework.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Email;

public class FileLogEmailServiceTests : IDisposable
{
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<FileLogEmailService> _logger;
    private readonly FileLogEmailService _service;
    private readonly string _outputPath;

    public FileLogEmailServiceTests()
    {
        var settings = new EmailSettings
        {
            IsEnabled = true,
            DefaultFromEmail = "noreply@test.com",
            DefaultFromName = "Test App"
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<FileLogEmailService>>();
        _service = new FileLogEmailService(_options, _logger);
        _outputPath = _service.GetOutputPath();
    }

    public void Dispose()
    {
        // Clean up logged emails
        _service.ClearLoggedEmails();
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Assert
        _service.ShouldNotBeNull();
    }

    [Fact]
    public void GetOutputPath_ShouldReturnValidPath()
    {
        // Act
        var path = _service.GetOutputPath();

        // Assert
        path.ShouldNotBeNullOrEmpty();
        path.ShouldContain("Framework");
        path.ShouldContain("Emails");
    }

    [Fact]
    public async Task SendAsync_ShouldCreateEmailFile()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Test Subject", "<p>Test Body</p>");

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNullOrEmpty();

        var files = _service.GetLoggedEmails().ToList();
        files.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SendAsync_ShouldCreateMetadataFile()
    {
        // Arrange
        var message = EmailMessage.Create("test@example.com", "Test Subject", "<p>Test Body</p>");

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var metadata = await _service.GetLoggedEmailMetadataAsync();
        metadata.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldNotCreateFile()
    {
        // Arrange
        var settings = new EmailSettings { IsEnabled = false };
        var options = Options.Create(settings);
        var service = new FileLogEmailService(options, _logger);

        var message = EmailMessage.Create("test@example.com", "Test", "Body");
        var initialCount = service.GetLoggedEmails().Count();

        // Act
        var result = await service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        service.GetLoggedEmails().Count().ShouldBe(initialCount);
    }

    [Fact]
    public async Task SendAsync_ShouldLogMultipleRecipients()
    {
        // Arrange
        var message = new EmailMessage
        {
            Subject = "Multi-Recipient Test"
        };
        message.To.Add(new EmailAddress("user1@example.com", "User 1"));
        message.To.Add(new EmailAddress("user2@example.com", "User 2"));
        message.Cc.Add(new EmailAddress("cc@example.com"));
        message.Bcc.Add(new EmailAddress("bcc@example.com"));
        message.HtmlBody = "<p>Hello</p>";

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.To.Count.ShouldBe(2);
        metadata.Cc.Count.ShouldBe(1);
        metadata.Bcc.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_ShouldLogAttachmentInfo()
    {
        // Arrange
        var message = new EmailMessage
        {
            Subject = "Attachment Test",
            HtmlBody = "<p>See attached</p>"
        };
        message.To.Add(new EmailAddress("test@example.com"));
        message.Attachments.Add(EmailAttachment.FromBytes("test.txt", new byte[] { 1, 2, 3 }, "text/plain"));
        message.Attachments.Add(EmailAttachment.FromBytes("data.pdf", new byte[] { 4, 5, 6 }, "application/pdf"));

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.AttachmentCount.ShouldBe(2);
    }

    [Fact]
    public async Task SendAsync_ShouldLogPriority()
    {
        // Arrange
        var message = new EmailMessage
        {
            Subject = "High Priority",
            HtmlBody = "<p>Important</p>",
            Priority = EmailPriority.High
        };
        message.To.Add(new EmailAddress("test@example.com"));

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.Priority.ShouldBe("High");
    }

    [Fact]
    public async Task SendAsync_ShouldLogCustomHeaders()
    {
        // Arrange
        var message = new EmailMessage
        {
            Subject = "Headers Test",
            HtmlBody = "<p>Test</p>"
        };
        message.To.Add(new EmailAddress("test@example.com"));
        message.Headers["X-Campaign-Id"] = "camp-123";
        message.Headers["X-Custom"] = "value";

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.Headers.Count.ShouldBe(2);
        metadata.Headers["X-Campaign-Id"].ShouldBe("camp-123");
    }

    [Fact]
    public async Task SendAsync_ShouldLogTags()
    {
        // Arrange
        var message = new EmailMessage
        {
            Subject = "Tags Test",
            HtmlBody = "<p>Test</p>"
        };
        message.To.Add(new EmailAddress("test@example.com"));
        message.Tags.Add("marketing");
        message.Tags.Add("newsletter");

        // Act
        var result = await _service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.Tags.ShouldContain("marketing");
        metadata.Tags.ShouldContain("newsletter");
    }

    [Fact]
    public async Task SendBatchAsync_ShouldLogAllEmails()
    {
        // Arrange
        var messages = new List<EmailMessage>
        {
            EmailMessage.Create("user1@example.com", "Email 1", "<p>Body 1</p>"),
            EmailMessage.Create("user2@example.com", "Email 2", "<p>Body 2</p>"),
            EmailMessage.Create("user3@example.com", "Email 3", "<p>Body 3</p>")
        };

        var initialCount = _service.GetLoggedEmails().Count();

        // Act
        var results = await _service.SendBatchAsync(messages);

        // Assert
        results.Count().ShouldBe(3);
        results.All(r => r.IsSuccess).ShouldBeTrue();
        _service.GetLoggedEmails().Count().ShouldBe(initialCount + 3);
    }

    [Fact]
    public async Task SendBatchAsync_ShouldRespectCancellation()
    {
        // Arrange
        var messages = new List<EmailMessage>
        {
            EmailMessage.Create("user1@example.com", "Email 1", "<p>Body 1</p>"),
            EmailMessage.Create("user2@example.com", "Email 2", "<p>Body 2</p>"),
            EmailMessage.Create("user3@example.com", "Email 3", "<p>Body 3</p>")
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var results = await _service.SendBatchAsync(messages, cts.Token);

        // Assert - should have fewer results due to cancellation
        results.Count().ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task SendTemplateAsync_WithoutTemplateService_ShouldReturnFailure()
    {
        // Arrange
        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act
        var result = await _service.SendTemplateAsync("welcome", "test@example.com", model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Template service not configured");
    }

    [Fact]
    public async Task ClearLoggedEmails_ShouldRemoveAllFiles()
    {
        // Arrange - ensure some emails exist
        var message = EmailMessage.Create("test@example.com", "Test", "<p>Body</p>");
        await _service.SendAsync(message);
        _service.GetLoggedEmails().Count().ShouldBeGreaterThan(0);

        // Act
        _service.ClearLoggedEmails();

        // Assert
        _service.GetLoggedEmails().Count().ShouldBe(0);
    }

    [Fact]
    public void GetLoggedEmails_WhenNoEmails_ShouldReturnEmpty()
    {
        // Arrange
        _service.ClearLoggedEmails();

        // Act
        var emails = _service.GetLoggedEmails();

        // Assert
        emails.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLoggedEmailMetadata_WhenNoEmails_ShouldReturnEmpty()
    {
        // Arrange
        _service.ClearLoggedEmails();

        // Act
        var metadata = await _service.GetLoggedEmailMetadataAsync();

        // Assert
        metadata.ShouldBeEmpty();
    }
}

public class FileLogEmailServiceWithTemplateTests : IDisposable
{
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<FileLogEmailService> _logger;
    private readonly IEmailTemplateService _templateService;
    private readonly FileLogEmailService _service;

    public FileLogEmailServiceWithTemplateTests()
    {
        var settings = new EmailSettings
        {
            IsEnabled = true,
            DefaultFromEmail = "noreply@test.com",
            DefaultFromName = "Test App"
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<FileLogEmailService>>();
        _templateService = Substitute.For<IEmailTemplateService>();
        _service = new FileLogEmailService(_options, _logger, _templateService);
    }

    public void Dispose()
    {
        _service.ClearLoggedEmails();
    }

    [Fact]
    public async Task SendTemplateAsync_WithTemplateService_ShouldRenderAndLog()
    {
        // Arrange
        _service.ClearLoggedEmails(); // Clear any existing emails for test isolation

        var model = new WelcomeEmailModel { UserName = "Test User" };
        var rendered = new RenderedEmail
        {
            Subject = "Welcome, Test User!",
            HtmlBody = "<p>Welcome</p>",
            TextBody = "Welcome",
            TemplateName = "welcome"
        };

        _templateService.RenderAsync("welcome", model, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(rendered));

        // Act
        var result = await _service.SendTemplateAsync("welcome", "test@example.com", model);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldNotBeNull();

        var metadata = (await _service.GetLoggedEmailMetadataAsync())
            .First(m => m.MessageId == result.MessageId);
        metadata.Subject.ShouldBe("Welcome, Test User!");
        metadata.Tags.ShouldContain("template:welcome");
    }

    [Fact]
    public async Task SendTemplateAsync_WhenRenderFails_ShouldReturnFailure()
    {
        // Arrange
        var model = new WelcomeEmailModel { UserName = "Test" };
        _templateService.RenderAsync(Arg.Any<string>(), Arg.Any<WelcomeEmailModel>(), Arg.Any<CancellationToken>())
            .Returns<RenderedEmail>(x => throw new InvalidOperationException("Template parse error"));

        // Act
        var result = await _service.SendTemplateAsync("broken", "test@example.com", model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Template parse error");
    }
}
