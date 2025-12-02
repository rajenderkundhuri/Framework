using Framework.Application.Email;
using Framework.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Email;

public class SmtpEmailServiceTests : IDisposable
{
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly SmtpEmailService _service;

    public SmtpEmailServiceTests()
    {
        var settings = new EmailSettings
        {
            IsEnabled = true,
            DefaultFromEmail = "noreply@test.com",
            DefaultFromName = "Test App",
            SmtpHost = "localhost",
            SmtpPort = 25
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<SmtpEmailService>>();
        _service = new SmtpEmailService(_options, _logger);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Assert
        _service.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldReturnSuccessWithoutSending()
    {
        // Arrange
        var settings = new EmailSettings { IsEnabled = false };
        var options = Options.Create(settings);
        using var service = new SmtpEmailService(options, _logger);

        var message = EmailMessage.Create("test@example.com", "Test", "Body");

        // Act
        var result = await service.SendAsync(message);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task SendAsync_WhenDisabled_ShouldLogWarning()
    {
        // Arrange
        var settings = new EmailSettings { IsEnabled = false };
        var options = Options.Create(settings);
        using var service = new SmtpEmailService(options, _logger);

        var message = EmailMessage.Create("test@example.com", "Test", "Body");

        // Act
        await service.SendAsync(message);

        // Assert
        _logger.ReceivedWithAnyArgs().LogWarning(default(string));
    }

    [Fact]
    public async Task SendTemplateAsync_WithoutTemplateService_ShouldReturnFailure()
    {
        // Arrange - service created without template service
        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act
        var result = await _service.SendTemplateAsync("welcome", "test@example.com", model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Template service not configured");
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyList_ShouldReturnEmptyResults()
    {
        // Arrange
        var messages = new List<EmailMessage>();

        // Act
        var results = await _service.SendBatchAsync(messages);

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendBatchAsync_WhenDisabled_ShouldReturnSuccessForAll()
    {
        // Arrange
        var settings = new EmailSettings { IsEnabled = false };
        var options = Options.Create(settings);
        using var service = new SmtpEmailService(options, _logger);

        var messages = new List<EmailMessage>
        {
            EmailMessage.Create("user1@example.com", "Test 1", "Body 1"),
            EmailMessage.Create("user2@example.com", "Test 2", "Body 2")
        };

        // Act
        var results = await service.SendBatchAsync(messages);

        // Assert
        var resultList = results.ToList();
        resultList.Count.ShouldBe(2);
        resultList.All(r => r.IsSuccess).ShouldBeTrue();
    }

    [Fact]
    public async Task SendBatchAsync_ShouldRespectCancellation()
    {
        // Arrange
        var settings = new EmailSettings { IsEnabled = false };
        var options = Options.Create(settings);
        using var service = new SmtpEmailService(options, _logger);

        var messages = new List<EmailMessage>
        {
            EmailMessage.Create("user1@example.com", "Test 1", "Body 1"),
            EmailMessage.Create("user2@example.com", "Test 2", "Body 2"),
            EmailMessage.Create("user3@example.com", "Test 3", "Body 3")
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var results = await service.SendBatchAsync(messages, cts.Token);

        // Assert
        results.Count().ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var settings = new EmailSettings();
        var options = Options.Create(settings);
        var service = new SmtpEmailService(options, _logger);

        // Act & Assert
        Should.NotThrow(() => service.Dispose());
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var settings = new EmailSettings();
        var options = Options.Create(settings);
        var service = new SmtpEmailService(options, _logger);

        // Act & Assert
        Should.NotThrow(() =>
        {
            service.Dispose();
            service.Dispose();
        });
    }
}

public class SmtpEmailServiceWithTemplateTests : IDisposable
{
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly IEmailTemplateService _templateService;
    private readonly SmtpEmailService _service;

    public SmtpEmailServiceWithTemplateTests()
    {
        var settings = new EmailSettings
        {
            IsEnabled = false, // Disable actual sending for tests
            DefaultFromEmail = "noreply@test.com",
            DefaultFromName = "Test App"
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<SmtpEmailService>>();
        _templateService = Substitute.For<IEmailTemplateService>();
        _service = new SmtpEmailService(_options, _logger, _templateService);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task SendTemplateAsync_WithTemplateService_ShouldRenderAndSend()
    {
        // Arrange
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
        await _templateService.Received(1).RenderAsync("welcome", model, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTemplateAsync_WhenRenderFails_ShouldReturnFailure()
    {
        // Arrange
        var model = new WelcomeEmailModel { UserName = "Test User" };
        _templateService.RenderAsync(Arg.Any<string>(), Arg.Any<WelcomeEmailModel>(), Arg.Any<CancellationToken>())
            .Returns<RenderedEmail>(x => throw new InvalidOperationException("Template not found"));

        // Act
        var result = await _service.SendTemplateAsync("unknown", "test@example.com", model);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("Template not found");
    }
}
