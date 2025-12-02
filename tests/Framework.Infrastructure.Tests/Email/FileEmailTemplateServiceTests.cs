using Framework.Application.Email;
using Framework.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Framework.Infrastructure.Tests.Email;

public class FileEmailTemplateServiceTests : IDisposable
{
    private readonly string _templatePath;
    private readonly IOptions<EmailSettings> _options;
    private readonly ILogger<FileEmailTemplateService> _logger;
    private readonly FileEmailTemplateService _service;

    public FileEmailTemplateServiceTests()
    {
        _templatePath = Path.Combine(Path.GetTempPath(), $"templates_{Guid.NewGuid()}");
        Directory.CreateDirectory(_templatePath);

        var settings = new EmailSettings
        {
            TemplatesPath = _templatePath
        };

        _options = Options.Create(settings);
        _logger = Substitute.For<ILogger<FileEmailTemplateService>>();
        _service = new FileEmailTemplateService(_options, _logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_templatePath))
        {
            Directory.Delete(_templatePath, true);
        }
    }

    [Fact]
    public void Constructor_ShouldCreateService()
    {
        // Assert
        _service.ShouldNotBeNull();
    }

    [Fact]
    public void TemplateExists_WhenTemplateDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var exists = _service.TemplateExists("nonexistent");

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public void TemplateExists_WhenDirectoryTemplateExists_ShouldReturnTrue()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "welcome");
        Directory.CreateDirectory(templateDir);

        // Act
        var exists = _service.TemplateExists("welcome");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public void TemplateExists_WhenSingleFileTemplateExists_ShouldReturnTrue()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_templatePath, "simple.html"), "<p>Hello</p>");

        // Act
        var exists = _service.TemplateExists("simple");

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public void GetTemplateNames_WhenNoTemplates_ShouldReturnEmpty()
    {
        // Act
        var names = _service.GetTemplateNames();

        // Assert
        names.ShouldBeEmpty();
    }

    [Fact]
    public void GetTemplateNames_ShouldReturnDirectoryTemplates()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_templatePath, "welcome"));
        Directory.CreateDirectory(Path.Combine(_templatePath, "password-reset"));

        // Act
        var names = _service.GetTemplateNames().ToList();

        // Assert
        names.Count.ShouldBe(2);
        names.ShouldContain("welcome");
        names.ShouldContain("password-reset");
    }

    [Fact]
    public void GetTemplateNames_ShouldReturnFileTemplates()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_templatePath, "notification.html"), "<p>Test</p>");
        File.WriteAllText(Path.Combine(_templatePath, "alert.html"), "<p>Alert</p>");

        // Act
        var names = _service.GetTemplateNames().ToList();

        // Assert
        names.Count.ShouldBe(2);
        names.ShouldContain("notification");
        names.ShouldContain("alert");
    }

    [Fact]
    public void GetTemplateNames_ShouldReturnBothDirectoryAndFileTemplates()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_templatePath, "welcome"));
        File.WriteAllText(Path.Combine(_templatePath, "simple.html"), "<p>Test</p>");

        // Act
        var names = _service.GetTemplateNames().ToList();

        // Assert
        names.Count.ShouldBe(2);
        names.ShouldContain("welcome");
        names.ShouldContain("simple");
    }

    [Fact]
    public async Task RenderAsync_WithDirectoryTemplate_ShouldRenderCorrectly()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "welcome");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Welcome, {{UserName}}!");
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<h1>Hello {{UserName}}</h1>");
        File.WriteAllText(Path.Combine(templateDir, "body.txt"), "Hello {{UserName}}");

        var model = new WelcomeEmailModel { UserName = "John" };

        // Act
        var rendered = await _service.RenderAsync("welcome", model);

        // Assert
        rendered.Subject.ShouldBe("Welcome, John!");
        rendered.HtmlBody.ShouldBe("<h1>Hello John</h1>");
        rendered.TextBody.ShouldBe("Hello John");
        rendered.TemplateName.ShouldBe("welcome");
    }

    [Fact]
    public async Task RenderAsync_WithSingleFileTemplate_ShouldRenderCorrectly()
    {
        // Arrange
        var templateContent = @"---
subject: Hello {{UserName}}
---
<p>Welcome to {{ApplicationName}}</p>";

        File.WriteAllText(Path.Combine(_templatePath, "simple.html"), templateContent);

        var model = new WelcomeEmailModel
        {
            UserName = "Jane",
            ApplicationName = "Test App"
        };

        // Act
        var rendered = await _service.RenderAsync("simple", model);

        // Assert
        rendered.Subject.ShouldBe("Hello Jane");
        rendered.HtmlBody.ShouldBe("<p>Welcome to Test App</p>");
    }

    [Fact]
    public async Task RenderAsync_WithNonExistentTemplate_ShouldThrow()
    {
        // Arrange
        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _service.RenderAsync("nonexistent", model));
    }

    [Fact]
    public async Task RenderAsync_ShouldReplaceMultiplePlaceholders()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "multi");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "{{UserName}} - {{ApplicationName}}");
        File.WriteAllText(Path.Combine(templateDir, "body.html"),
            "<p>User: {{UserName}}</p><p>App: {{ApplicationName}}</p><p>Support: {{SupportEmail}}</p>");

        var model = new WelcomeEmailModel
        {
            UserName = "Alice",
            ApplicationName = "My App",
            SupportEmail = "help@myapp.com"
        };

        // Act
        var rendered = await _service.RenderAsync("multi", model);

        // Assert
        rendered.Subject.ShouldBe("Alice - My App");
        rendered.HtmlBody.ShouldContain("User: Alice");
        rendered.HtmlBody.ShouldContain("App: My App");
        rendered.HtmlBody.ShouldContain("Support: help@myapp.com");
    }

    [Fact]
    public async Task RenderAsync_ShouldHandlePlaceholdersWithSpaces()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "spaced");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "{{ UserName }}");
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<p>{{ ApplicationName }}</p>");

        var model = new WelcomeEmailModel
        {
            UserName = "Bob",
            ApplicationName = "Space App"
        };

        // Act
        var rendered = await _service.RenderAsync("spaced", model);

        // Assert
        rendered.Subject.ShouldBe("Bob");
        rendered.HtmlBody.ShouldBe("<p>Space App</p>");
    }

    [Fact]
    public async Task RenderAsync_ShouldHandleConditionalBlocks()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "conditional");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Welcome");
        File.WriteAllText(Path.Combine(templateDir, "body.html"),
            "<p>Hello {{UserName}}</p>{{#if ActivationLink}}<a href=\"{{ActivationLink}}\">Activate</a>{{/if}}");

        // Act - with activation link
        var modelWithLink = new WelcomeEmailModel
        {
            UserName = "Test",
            ActivationLink = "https://example.com/activate"
        };
        var renderedWithLink = await _service.RenderAsync("conditional", modelWithLink);

        // Clear cache to test without link
        _service.ClearCache();

        // Act - without activation link
        var modelWithoutLink = new WelcomeEmailModel
        {
            UserName = "Test2",
            ActivationLink = null
        };
        var renderedWithoutLink = await _service.RenderAsync("conditional", modelWithoutLink);

        // Assert
        renderedWithLink.HtmlBody.ShouldContain("<a href=\"https://example.com/activate\">Activate</a>");
        renderedWithoutLink.HtmlBody.ShouldNotContain("Activate");
    }

    [Fact]
    public async Task RenderAsync_ShouldCacheTemplates()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "cached");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Original Subject");
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<p>Original</p>");

        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act - first render
        var firstRender = await _service.RenderAsync("cached", model);

        // Modify template file
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Modified Subject");

        // Second render should use cached version
        var secondRender = await _service.RenderAsync("cached", model);

        // Assert - both should have original subject
        firstRender.Subject.ShouldBe("Original Subject");
        secondRender.Subject.ShouldBe("Original Subject");
    }

    [Fact]
    public async Task RenderAsync_AfterClearCache_ShouldReloadTemplate()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "reloaded");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Original");
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<p>Body</p>");

        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act - first render
        var firstRender = await _service.RenderAsync("reloaded", model);

        // Modify and clear cache
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Modified");
        _service.ClearCache();

        var secondRender = await _service.RenderAsync("reloaded", model);

        // Assert
        firstRender.Subject.ShouldBe("Original");
        secondRender.Subject.ShouldBe("Modified");
    }

    [Fact]
    public void ClearCache_ShouldNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => _service.ClearCache());
    }

    [Fact]
    public async Task RenderAsync_WithPasswordResetModel_ShouldRenderCorrectly()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "password-reset");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Reset your password");
        File.WriteAllText(Path.Combine(templateDir, "body.html"),
            "<p>Hi {{UserName}}, click <a href=\"{{ResetLink}}\">here</a> to reset. Expires in {{ExpirationHours}} hours.</p>");

        var model = new PasswordResetEmailModel
        {
            UserName = "John",
            ResetLink = "https://example.com/reset/abc123",
            ExpirationHours = 48
        };

        // Act
        var rendered = await _service.RenderAsync("password-reset", model);

        // Assert
        rendered.HtmlBody.ShouldContain("Hi John");
        rendered.HtmlBody.ShouldContain("href=\"https://example.com/reset/abc123\"");
        rendered.HtmlBody.ShouldContain("48 hours");
    }

    [Fact]
    public async Task RenderAsync_WithNotificationModel_ShouldRenderCorrectly()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "notification");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "{{Title}}");
        File.WriteAllText(Path.Combine(templateDir, "body.html"),
            "<p>{{Message}}</p>{{#if ActionUrl}}<a href=\"{{ActionUrl}}\">{{ActionText}}</a>{{/if}}");

        var model = new NotificationEmailModel
        {
            Title = "New Message",
            Message = "You have a new message!",
            ActionText = "View",
            ActionUrl = "https://example.com/messages/123"
        };

        // Act
        var rendered = await _service.RenderAsync("notification", model);

        // Assert
        rendered.Subject.ShouldBe("New Message");
        rendered.HtmlBody.ShouldContain("You have a new message!");
        rendered.HtmlBody.ShouldContain("View");
        rendered.HtmlBody.ShouldContain("https://example.com/messages/123");
    }

    [Fact]
    public async Task RenderAsync_WithoutSubjectFile_ShouldUseTemplateName()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "no-subject");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<p>Body only</p>");

        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act
        var rendered = await _service.RenderAsync("no-subject", model);

        // Assert
        rendered.Subject.ShouldBe("no-subject");
    }

    [Fact]
    public async Task RenderAsync_WithHtmlOnlyTemplate_ShouldHaveNullTextBody()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "html-only");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Subject");
        File.WriteAllText(Path.Combine(templateDir, "body.html"), "<p>HTML Body</p>");

        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act
        var rendered = await _service.RenderAsync("html-only", model);

        // Assert
        rendered.HtmlBody.ShouldBe("<p>HTML Body</p>");
        rendered.TextBody.ShouldBeNull();
    }

    [Fact]
    public async Task RenderAsync_WithTextOnlyTemplate_ShouldHaveNullHtmlBody()
    {
        // Arrange
        var templateDir = Path.Combine(_templatePath, "text-only");
        Directory.CreateDirectory(templateDir);
        File.WriteAllText(Path.Combine(templateDir, "subject.txt"), "Subject");
        File.WriteAllText(Path.Combine(templateDir, "body.txt"), "Plain text body");

        var model = new WelcomeEmailModel { UserName = "Test" };

        // Act
        var rendered = await _service.RenderAsync("text-only", model);

        // Assert
        rendered.HtmlBody.ShouldBeNull();
        rendered.TextBody.ShouldBe("Plain text body");
    }

    [Fact]
    public void GetTemplateNames_WithInvalidPath_ShouldReturnEmpty()
    {
        // Arrange
        var settings = new EmailSettings { TemplatesPath = "/nonexistent/path" };
        var options = Options.Create(settings);
        var service = new FileEmailTemplateService(options, _logger);

        // Act
        var names = service.GetTemplateNames();

        // Assert
        names.ShouldBeEmpty();
    }
}
