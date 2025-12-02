using Framework.Domain.Identity;
using Shouldly;

namespace Framework.Domain.Tests.Identity;

public class ExternalLoginTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var provider = "Google";
        var providerKey = "google-user-id-123";
        var displayName = "John Doe";

        // Act
        var login = new ExternalLogin(id, userId, provider, providerKey, displayName);

        // Assert
        login.Id.ShouldBe(id);
        login.UserId.ShouldBe(userId);
        login.Provider.ShouldBe(provider);
        login.ProviderKey.ShouldBe(providerKey);
        login.ProviderDisplayName.ShouldBe(displayName);
        login.LinkedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Constructor_WithoutDisplayName_ShouldSetNullDisplayName()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var provider = "AzureAD";
        var providerKey = "azure-ad-user-id";

        // Act
        var login = new ExternalLogin(id, userId, provider, providerKey);

        // Assert
        login.ProviderDisplayName.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldSetLinkedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var login = new ExternalLogin(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Provider",
            "key");

        var after = DateTime.UtcNow;

        // Assert
        login.LinkedAt.ShouldBeGreaterThanOrEqualTo(before);
        login.LinkedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void ExternalLogins_WithSameProviderAndKey_ShouldBeDistinct()
    {
        // Arrange
        var provider = "Google";
        var providerKey = "google-key-123";

        var login1 = new ExternalLogin(Guid.NewGuid(), Guid.NewGuid(), provider, providerKey);
        var login2 = new ExternalLogin(Guid.NewGuid(), Guid.NewGuid(), provider, providerKey);

        // Assert
        login1.Id.ShouldNotBe(login2.Id);
        login1.Provider.ShouldBe(login2.Provider);
        login1.ProviderKey.ShouldBe(login2.ProviderKey);
    }

    [Fact]
    public void ExternalLogin_DifferentProviders_ShouldHaveDifferentProviderNames()
    {
        // Arrange & Act
        var googleLogin = new ExternalLogin(Guid.NewGuid(), Guid.NewGuid(), "Google", "google-key");
        var azureLogin = new ExternalLogin(Guid.NewGuid(), Guid.NewGuid(), "AzureAD", "azure-key");
        var oktaLogin = new ExternalLogin(Guid.NewGuid(), Guid.NewGuid(), "Okta", "okta-key");

        // Assert
        googleLogin.Provider.ShouldNotBe(azureLogin.Provider);
        azureLogin.Provider.ShouldNotBe(oktaLogin.Provider);
        googleLogin.Provider.ShouldNotBe(oktaLogin.Provider);
    }
}
