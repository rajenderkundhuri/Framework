using Framework.Application.Email;
using Shouldly;

namespace Framework.Application.Tests.Email;

public class EmailMessageTests
{
    [Fact]
    public void Create_WithBasicParameters_ShouldCreateHtmlMessage()
    {
        // Act
        var message = EmailMessage.Create("test@example.com", "Test Subject", "<p>Hello</p>");

        // Assert
        message.To.Count.ShouldBe(1);
        message.To[0].Email.ShouldBe("test@example.com");
        message.Subject.ShouldBe("Test Subject");
        message.HtmlBody.ShouldBe("<p>Hello</p>");
        message.TextBody.ShouldBeNull();
    }

    [Fact]
    public void Create_WithIsHtmlFalse_ShouldCreateTextMessage()
    {
        // Act
        var message = EmailMessage.Create("test@example.com", "Test Subject", "Hello", isHtml: false);

        // Assert
        message.TextBody.ShouldBe("Hello");
        message.HtmlBody.ShouldBeNull();
    }

    [Fact]
    public void NewEmailMessage_ShouldHaveEmptyCollections()
    {
        // Act
        var message = new EmailMessage();

        // Assert
        message.To.ShouldNotBeNull();
        message.To.ShouldBeEmpty();
        message.Cc.ShouldNotBeNull();
        message.Cc.ShouldBeEmpty();
        message.Bcc.ShouldNotBeNull();
        message.Bcc.ShouldBeEmpty();
        message.Attachments.ShouldNotBeNull();
        message.Attachments.ShouldBeEmpty();
        message.Headers.ShouldNotBeNull();
        message.Headers.ShouldBeEmpty();
        message.Tags.ShouldNotBeNull();
        message.Tags.ShouldBeEmpty();
    }

    [Fact]
    public void NewEmailMessage_ShouldHaveDefaultPriority()
    {
        // Act
        var message = new EmailMessage();

        // Assert
        message.Priority.ShouldBe(EmailPriority.Normal);
    }

    [Fact]
    public void EmailMessage_ShouldAllowMultipleRecipients()
    {
        // Arrange
        var message = new EmailMessage { Subject = "Test" };

        // Act
        message.To.Add(new EmailAddress("user1@example.com", "User 1"));
        message.To.Add(new EmailAddress("user2@example.com", "User 2"));
        message.Cc.Add(new EmailAddress("cc@example.com"));
        message.Bcc.Add(new EmailAddress("bcc@example.com"));

        // Assert
        message.To.Count.ShouldBe(2);
        message.Cc.Count.ShouldBe(1);
        message.Bcc.Count.ShouldBe(1);
    }

    [Fact]
    public void EmailMessage_ShouldAllowCustomHeaders()
    {
        // Arrange
        var message = new EmailMessage { Subject = "Test" };

        // Act
        message.Headers["X-Custom-Header"] = "CustomValue";
        message.Headers["X-Campaign-Id"] = "12345";

        // Assert
        message.Headers.Count.ShouldBe(2);
        message.Headers["X-Custom-Header"].ShouldBe("CustomValue");
    }

    [Fact]
    public void EmailMessage_ShouldAllowTags()
    {
        // Arrange
        var message = new EmailMessage { Subject = "Test" };

        // Act
        message.Tags.Add("marketing");
        message.Tags.Add("newsletter");

        // Assert
        message.Tags.Count.ShouldBe(2);
        message.Tags.ShouldContain("marketing");
        message.Tags.ShouldContain("newsletter");
    }

    [Fact]
    public void EmailMessage_ShouldSetReplyTo()
    {
        // Arrange
        var message = new EmailMessage { Subject = "Test" };

        // Act
        message.ReplyTo = new EmailAddress("reply@example.com", "Reply Here");

        // Assert
        message.ReplyTo.ShouldNotBeNull();
        message.ReplyTo.Email.ShouldBe("reply@example.com");
        message.ReplyTo.Name.ShouldBe("Reply Here");
    }
}

public class EmailAddressTests
{
    [Fact]
    public void Constructor_WithEmailOnly_ShouldSetEmail()
    {
        // Act
        var address = new EmailAddress("test@example.com");

        // Assert
        address.Email.ShouldBe("test@example.com");
        address.Name.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithEmailAndName_ShouldSetBoth()
    {
        // Act
        var address = new EmailAddress("test@example.com", "Test User");

        // Assert
        address.Email.ShouldBe("test@example.com");
        address.Name.ShouldBe("Test User");
    }

    [Fact]
    public void DefaultConstructor_ShouldHaveEmptyEmail()
    {
        // Act
        var address = new EmailAddress();

        // Assert
        address.Email.ShouldBe(string.Empty);
        address.Name.ShouldBeNull();
    }

    [Fact]
    public void ToString_WithEmailOnly_ShouldReturnEmail()
    {
        // Arrange
        var address = new EmailAddress("test@example.com");

        // Act
        var result = address.ToString();

        // Assert
        result.ShouldBe("test@example.com");
    }

    [Fact]
    public void ToString_WithEmailAndName_ShouldReturnFormattedString()
    {
        // Arrange
        var address = new EmailAddress("test@example.com", "Test User");

        // Act
        var result = address.ToString();

        // Assert
        result.ShouldBe("Test User <test@example.com>");
    }

    [Fact]
    public void ToString_WithEmptyName_ShouldReturnEmailOnly()
    {
        // Arrange
        var address = new EmailAddress("test@example.com", "");

        // Act
        var result = address.ToString();

        // Assert
        result.ShouldBe("test@example.com");
    }
}

public class EmailAttachmentTests
{
    [Fact]
    public void FromBytes_ShouldCreateAttachment()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var attachment = EmailAttachment.FromBytes("test.bin", content);

        // Assert
        attachment.FileName.ShouldBe("test.bin");
        attachment.Content.ShouldBe(content);
        attachment.ContentType.ShouldBe("application/octet-stream");
    }

    [Fact]
    public void FromBytes_WithContentType_ShouldSetContentType()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };

        // Act
        var attachment = EmailAttachment.FromBytes("test.pdf", content, "application/pdf");

        // Assert
        attachment.ContentType.ShouldBe("application/pdf");
    }

    [Fact]
    public void NewAttachment_ShouldHaveDefaults()
    {
        // Act
        var attachment = new EmailAttachment();

        // Assert
        attachment.FileName.ShouldBe(string.Empty);
        attachment.Content.ShouldBeEmpty();
        attachment.ContentType.ShouldBe("application/octet-stream");
        attachment.IsInline.ShouldBeFalse();
        attachment.ContentId.ShouldBeNull();
    }

    [Fact]
    public void InlineAttachment_ShouldSetContentId()
    {
        // Act
        var attachment = new EmailAttachment
        {
            FileName = "logo.png",
            Content = new byte[] { 1, 2, 3 },
            ContentType = "image/png",
            IsInline = true,
            ContentId = "logo-cid"
        };

        // Assert
        attachment.IsInline.ShouldBeTrue();
        attachment.ContentId.ShouldBe("logo-cid");
    }

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".doc", "application/msword")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".xls", "application/vnd.ms-excel")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".png", "image/png")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".txt", "text/plain")]
    [InlineData(".html", "text/html")]
    [InlineData(".csv", "text/csv")]
    [InlineData(".zip", "application/zip")]
    [InlineData(".unknown", "application/octet-stream")]
    public async Task FromFileAsync_ShouldDetectContentType(string extension, string expectedType)
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, $"test{extension}");
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3 });

        try
        {
            // Act
            var attachment = await EmailAttachment.FromFileAsync(filePath);

            // Assert
            attachment.ContentType.ShouldBe(expectedType);
            attachment.FileName.ShouldBe($"test{extension}");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

public class EmailResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = EmailResult.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        result.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void Success_WithMessageId_ShouldSetMessageId()
    {
        // Act
        var result = EmailResult.Success(messageId: "msg-123");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.MessageId.ShouldBe("msg-123");
    }

    [Fact]
    public void Success_WithRecipient_ShouldSetRecipient()
    {
        // Arrange
        var recipient = new EmailAddress("test@example.com");

        // Act
        var result = EmailResult.Success(recipient: recipient);

        // Assert
        result.Recipient.ShouldBe(recipient);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Act
        var result = EmailResult.Failure("Connection refused");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Connection refused");
    }

    [Fact]
    public void Failure_WithErrorCode_ShouldSetErrorCode()
    {
        // Act
        var result = EmailResult.Failure("Invalid recipient", errorCode: "INVALID_EMAIL");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid recipient");
        result.ErrorCode.ShouldBe("INVALID_EMAIL");
    }

    [Fact]
    public void Failure_WithRecipient_ShouldSetRecipient()
    {
        // Arrange
        var recipient = new EmailAddress("invalid@example.com");

        // Act
        var result = EmailResult.Failure("Invalid", recipient: recipient);

        // Assert
        result.Recipient.ShouldBe(recipient);
    }
}

public class EmailPriorityTests
{
    [Fact]
    public void EmailPriority_ShouldHaveExpectedValues()
    {
        // Assert
        ((int)EmailPriority.Low).ShouldBe(0);
        ((int)EmailPriority.Normal).ShouldBe(1);
        ((int)EmailPriority.High).ShouldBe(2);
    }
}
