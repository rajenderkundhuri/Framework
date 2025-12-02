using Framework.Application.Email;
using Shouldly;

namespace Framework.Application.Tests.Email;

public class RenderedEmailTests
{
    [Fact]
    public void NewRenderedEmail_ShouldHaveDefaults()
    {
        // Act
        var email = new RenderedEmail();

        // Assert
        email.Subject.ShouldBe(string.Empty);
        email.HtmlBody.ShouldBeNull();
        email.TextBody.ShouldBeNull();
        email.TemplateName.ShouldBe(string.Empty);
    }

    [Fact]
    public void RenderedEmail_ShouldAllowSettingAllProperties()
    {
        // Act
        var email = new RenderedEmail
        {
            Subject = "Welcome!",
            HtmlBody = "<h1>Welcome</h1>",
            TextBody = "Welcome",
            TemplateName = "welcome"
        };

        // Assert
        email.Subject.ShouldBe("Welcome!");
        email.HtmlBody.ShouldBe("<h1>Welcome</h1>");
        email.TextBody.ShouldBe("Welcome");
        email.TemplateName.ShouldBe("welcome");
    }
}

public class WelcomeEmailModelTests
{
    [Fact]
    public void WelcomeEmailModel_ShouldInheritFromBase()
    {
        // Act
        var model = new WelcomeEmailModel();

        // Assert
        model.ShouldBeAssignableTo<EmailTemplateModel>();
    }

    [Fact]
    public void WelcomeEmailModel_ShouldHaveDefaults()
    {
        // Act
        var model = new WelcomeEmailModel();

        // Assert
        model.UserName.ShouldBe(string.Empty);
        model.Email.ShouldBe(string.Empty);
        model.ActivationLink.ShouldBeNull();
        model.ApplicationName.ShouldBe("Application");
        model.ApplicationUrl.ShouldBe(string.Empty);
        model.SupportEmail.ShouldBe(string.Empty);
    }

    [Fact]
    public void WelcomeEmailModel_ShouldAllowSettingProperties()
    {
        // Act
        var model = new WelcomeEmailModel
        {
            UserName = "John Doe",
            Email = "john@example.com",
            ActivationLink = "https://example.com/activate/123",
            ApplicationName = "My App",
            ApplicationUrl = "https://myapp.com",
            SupportEmail = "support@myapp.com"
        };

        // Assert
        model.UserName.ShouldBe("John Doe");
        model.Email.ShouldBe("john@example.com");
        model.ActivationLink.ShouldBe("https://example.com/activate/123");
        model.ApplicationName.ShouldBe("My App");
        model.ApplicationUrl.ShouldBe("https://myapp.com");
        model.SupportEmail.ShouldBe("support@myapp.com");
    }

    [Fact]
    public void CurrentYear_ShouldReturnCurrentYear()
    {
        // Arrange
        var model = new WelcomeEmailModel();

        // Assert
        model.CurrentYear.ShouldBe(DateTime.UtcNow.Year);
    }
}

public class PasswordResetEmailModelTests
{
    [Fact]
    public void PasswordResetEmailModel_ShouldInheritFromBase()
    {
        // Act
        var model = new PasswordResetEmailModel();

        // Assert
        model.ShouldBeAssignableTo<EmailTemplateModel>();
    }

    [Fact]
    public void PasswordResetEmailModel_ShouldHaveDefaults()
    {
        // Act
        var model = new PasswordResetEmailModel();

        // Assert
        model.UserName.ShouldBe(string.Empty);
        model.ResetLink.ShouldBe(string.Empty);
        model.ExpirationHours.ShouldBe(24);
    }

    [Fact]
    public void PasswordResetEmailModel_ShouldAllowSettingProperties()
    {
        // Act
        var model = new PasswordResetEmailModel
        {
            UserName = "Jane Doe",
            ResetLink = "https://example.com/reset/abc123",
            ExpirationHours = 48,
            ApplicationName = "Secure App"
        };

        // Assert
        model.UserName.ShouldBe("Jane Doe");
        model.ResetLink.ShouldBe("https://example.com/reset/abc123");
        model.ExpirationHours.ShouldBe(48);
        model.ApplicationName.ShouldBe("Secure App");
    }
}

public class NotificationEmailModelTests
{
    [Fact]
    public void NotificationEmailModel_ShouldInheritFromBase()
    {
        // Act
        var model = new NotificationEmailModel();

        // Assert
        model.ShouldBeAssignableTo<EmailTemplateModel>();
    }

    [Fact]
    public void NotificationEmailModel_ShouldHaveDefaults()
    {
        // Act
        var model = new NotificationEmailModel();

        // Assert
        model.UserName.ShouldBe(string.Empty);
        model.Title.ShouldBe(string.Empty);
        model.Message.ShouldBe(string.Empty);
        model.ActionText.ShouldBeNull();
        model.ActionUrl.ShouldBeNull();
    }

    [Fact]
    public void NotificationEmailModel_ShouldAllowSettingProperties()
    {
        // Act
        var model = new NotificationEmailModel
        {
            UserName = "Bob Smith",
            Title = "New Comment",
            Message = "Someone commented on your post",
            ActionText = "View Comment",
            ActionUrl = "https://example.com/posts/123#comment-456"
        };

        // Assert
        model.UserName.ShouldBe("Bob Smith");
        model.Title.ShouldBe("New Comment");
        model.Message.ShouldBe("Someone commented on your post");
        model.ActionText.ShouldBe("View Comment");
        model.ActionUrl.ShouldBe("https://example.com/posts/123#comment-456");
    }

    [Fact]
    public void NotificationEmailModel_WithoutAction_ShouldHaveNullActionFields()
    {
        // Act
        var model = new NotificationEmailModel
        {
            UserName = "Test User",
            Title = "Info Only",
            Message = "This is just informational"
        };

        // Assert
        model.ActionText.ShouldBeNull();
        model.ActionUrl.ShouldBeNull();
    }
}

public class EmailTemplateModelBaseTests
{
    private class TestEmailModel : EmailTemplateModel
    {
        public string CustomField { get; set; } = string.Empty;
    }

    [Fact]
    public void EmailTemplateModel_ShouldHaveDefaultApplicationName()
    {
        // Act
        var model = new TestEmailModel();

        // Assert
        model.ApplicationName.ShouldBe("Application");
    }

    [Fact]
    public void CurrentYear_ShouldAlwaysReturnCurrentYear()
    {
        // Arrange
        var model = new TestEmailModel();
        var expectedYear = DateTime.UtcNow.Year;

        // Act & Assert - multiple calls should return same year
        model.CurrentYear.ShouldBe(expectedYear);
        model.CurrentYear.ShouldBe(expectedYear);
    }

    [Fact]
    public void CustomModel_CanExtendBase()
    {
        // Act
        var model = new TestEmailModel
        {
            CustomField = "Custom Value",
            ApplicationName = "Test App",
            ApplicationUrl = "https://test.com",
            SupportEmail = "help@test.com"
        };

        // Assert
        model.CustomField.ShouldBe("Custom Value");
        model.ApplicationName.ShouldBe("Test App");
        model.ApplicationUrl.ShouldBe("https://test.com");
        model.SupportEmail.ShouldBe("help@test.com");
    }
}
