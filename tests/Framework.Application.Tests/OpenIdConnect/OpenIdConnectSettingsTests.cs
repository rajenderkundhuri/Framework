using Framework.Application.OpenIdConnect;
using Shouldly;

namespace Framework.Application.Tests.OpenIdConnect;

public class OpenIdConnectSettingsTests
{
    [Fact]
    public void SectionName_ShouldBeOpenIdConnect()
    {
        // Assert
        OpenIdConnectSettings.SectionName.ShouldBe("OpenIdConnect");
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new OpenIdConnectSettings();

        // Assert
        settings.Enabled.ShouldBeTrue();
        settings.Issuer.ShouldBe(string.Empty);
        settings.AccessTokenLifetimeMinutes.ShouldBe(60);
        settings.RefreshTokenLifetimeDays.ShouldBe(14);
        settings.IdentityTokenLifetimeMinutes.ShouldBe(60);
        settings.AuthorizationCodeLifetimeMinutes.ShouldBe(5);
        settings.EnableAuthorizationCodeFlow.ShouldBeTrue();
        settings.EnableClientCredentialsFlow.ShouldBeTrue();
        settings.EnableRefreshTokenFlow.ShouldBeTrue();
        settings.EnablePasswordFlow.ShouldBeFalse();
        settings.RequirePkce.ShouldBeTrue();
        settings.UseDevelopmentCertificates.ShouldBeFalse();
        settings.SigningKey.ShouldBeNull();
        settings.EncryptionKey.ShouldBeNull();
        settings.Scopes.ShouldNotBeNull();
        settings.Clients.ShouldNotBeNull();
        settings.ExternalProviders.ShouldNotBeNull();
    }

    [Fact]
    public void DefaultScopes_ShouldContainStandardScopes()
    {
        // Arrange & Act
        var settings = new OpenIdConnectSettings();

        // Assert
        settings.Scopes.ShouldContain("openid");
        settings.Scopes.ShouldContain("profile");
        settings.Scopes.ShouldContain("email");
        settings.Scopes.ShouldContain("roles");
    }

    [Fact]
    public void ExternalProvidersSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new ExternalProvidersSettings();

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.RequireExistingUser.ShouldBeTrue();
        settings.AutoCreateUser.ShouldBeFalse();
        settings.OidcProviders.ShouldNotBeNull();
        settings.OidcProviders.ShouldBeEmpty();
    }

    [Fact]
    public void AzureAdSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new AzureAdSettings();

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.Instance.ShouldBe("https://login.microsoftonline.com/");
        settings.TenantId.ShouldBe(string.Empty);
        settings.ClientId.ShouldBe(string.Empty);
        settings.ClientSecret.ShouldBe(string.Empty);
        settings.CallbackPath.ShouldBe("/signin-azuread");
    }

    [Fact]
    public void GoogleSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new GoogleSettings();

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.ClientId.ShouldBe(string.Empty);
        settings.ClientSecret.ShouldBe(string.Empty);
        settings.CallbackPath.ShouldBe("/signin-google");
    }

    [Fact]
    public void OidcClientSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new OidcClientSettings();

        // Assert
        settings.ClientId.ShouldBe(string.Empty);
        settings.ClientSecret.ShouldBeNull();
        settings.DisplayName.ShouldBe(string.Empty);
        settings.ClientType.ShouldBe(OidcClientType.Confidential);
        settings.RequireConsent.ShouldBeFalse();
        settings.RequirePkce.ShouldBeTrue();
        settings.RedirectUris.ShouldNotBeNull();
        settings.PostLogoutRedirectUris.ShouldNotBeNull();
        settings.AllowedGrantTypes.ShouldNotBeNull();
        settings.AllowedScopes.ShouldNotBeNull();
    }

    [Fact]
    public void GenericOidcProviderSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new GenericOidcProviderSettings();

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.Name.ShouldBe(string.Empty);
        settings.DisplayName.ShouldBe(string.Empty);
        settings.Authority.ShouldBe(string.Empty);
        settings.ClientId.ShouldBe(string.Empty);
        settings.ClientSecret.ShouldBe(string.Empty);
        settings.CallbackPath.ShouldBe(string.Empty);
        settings.GetClaimsFromUserInfoEndpoint.ShouldBeTrue();
        settings.Scopes.ShouldNotBeNull();
        settings.Scopes.ShouldContain("openid");
        settings.Scopes.ShouldContain("profile");
        settings.Scopes.ShouldContain("email");
    }

    [Fact]
    public void OidcClientType_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)OidcClientType.Confidential).ShouldBe(0);
        ((int)OidcClientType.Public).ShouldBe(1);
    }

    [Fact]
    public void Settings_ShouldBeModifiable()
    {
        // Arrange
        var settings = new OpenIdConnectSettings
        {
            Enabled = false,
            Issuer = "https://example.com",
            AccessTokenLifetimeMinutes = 30,
            RefreshTokenLifetimeDays = 7,
            EnablePasswordFlow = true,
            RequirePkce = false
        };

        // Assert
        settings.Enabled.ShouldBeFalse();
        settings.Issuer.ShouldBe("https://example.com");
        settings.AccessTokenLifetimeMinutes.ShouldBe(30);
        settings.RefreshTokenLifetimeDays.ShouldBe(7);
        settings.EnablePasswordFlow.ShouldBeTrue();
        settings.RequirePkce.ShouldBeFalse();
    }

    [Fact]
    public void ClientSettings_ShouldSupportMultipleRedirectUris()
    {
        // Arrange
        var settings = new OidcClientSettings
        {
            ClientId = "test-client",
            RedirectUris = new List<string>
            {
                "https://app1.example.com/callback",
                "https://app2.example.com/callback",
                "http://localhost:3000/callback"
            }
        };

        // Assert
        settings.RedirectUris.Count.ShouldBe(3);
    }

    [Fact]
    public void ClientSettings_ShouldSupportMultipleGrantTypes()
    {
        // Arrange
        var settings = new OidcClientSettings
        {
            ClientId = "test-client",
            AllowedGrantTypes = new List<string>
            {
                "authorization_code",
                "refresh_token",
                "client_credentials"
            }
        };

        // Assert
        settings.AllowedGrantTypes.Count.ShouldBe(3);
        settings.AllowedGrantTypes.ShouldContain("authorization_code");
        settings.AllowedGrantTypes.ShouldContain("refresh_token");
        settings.AllowedGrantTypes.ShouldContain("client_credentials");
    }
}
