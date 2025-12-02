namespace Framework.Application.OpenIdConnect;

/// <summary>
/// OpenID Connect configuration settings
/// </summary>
public class OpenIdConnectSettings
{
    public const string SectionName = "OpenIdConnect";

    /// <summary>
    /// Enable OpenID Connect server
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Issuer URL (your server's URL)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Access token lifetime in minutes
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in days
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>
    /// Identity token lifetime in minutes
    /// </summary>
    public int IdentityTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Authorization code lifetime in minutes
    /// </summary>
    public int AuthorizationCodeLifetimeMinutes { get; set; } = 5;

    /// <summary>
    /// Enable authorization code flow
    /// </summary>
    public bool EnableAuthorizationCodeFlow { get; set; } = true;

    /// <summary>
    /// Enable client credentials flow
    /// </summary>
    public bool EnableClientCredentialsFlow { get; set; } = true;

    /// <summary>
    /// Enable refresh token flow
    /// </summary>
    public bool EnableRefreshTokenFlow { get; set; } = true;

    /// <summary>
    /// Enable password flow (not recommended for production)
    /// </summary>
    public bool EnablePasswordFlow { get; set; } = false;

    /// <summary>
    /// Require PKCE for authorization code flow
    /// </summary>
    public bool RequirePkce { get; set; } = true;

    /// <summary>
    /// Encryption key for tokens (base64 encoded, 256-bit minimum)
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Signing key for tokens (base64 encoded, 256-bit minimum)
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Use development certificates (for dev only)
    /// </summary>
    public bool UseDevelopmentCertificates { get; set; } = false;

    /// <summary>
    /// Registered OIDC clients/applications
    /// </summary>
    public List<OidcClientSettings> Clients { get; set; } = new();

    /// <summary>
    /// External identity providers
    /// </summary>
    public ExternalProvidersSettings ExternalProviders { get; set; } = new();

    /// <summary>
    /// Custom scopes
    /// </summary>
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email", "roles" };
}

/// <summary>
/// OIDC client/application settings
/// </summary>
public class OidcClientSettings
{
    /// <summary>
    /// Client identifier
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (for confidential clients)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Client type
    /// </summary>
    public OidcClientType ClientType { get; set; } = OidcClientType.Confidential;

    /// <summary>
    /// Allowed redirect URIs
    /// </summary>
    public List<string> RedirectUris { get; set; } = new();

    /// <summary>
    /// Allowed post-logout redirect URIs
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; } = new();

    /// <summary>
    /// Allowed scopes
    /// </summary>
    public List<string> AllowedScopes { get; set; } = new() { "openid", "profile", "email" };

    /// <summary>
    /// Allowed grant types
    /// </summary>
    public List<string> AllowedGrantTypes { get; set; } = new() { "authorization_code", "refresh_token" };

    /// <summary>
    /// Require consent screen
    /// </summary>
    public bool RequireConsent { get; set; } = false;

    /// <summary>
    /// Require PKCE
    /// </summary>
    public bool RequirePkce { get; set; } = true;
}

/// <summary>
/// OIDC client types
/// </summary>
public enum OidcClientType
{
    /// <summary>
    /// Confidential client (can keep secrets)
    /// </summary>
    Confidential = 0,

    /// <summary>
    /// Public client (cannot keep secrets, e.g., SPA, mobile)
    /// </summary>
    Public = 1
}

/// <summary>
/// External identity provider settings
/// </summary>
public class ExternalProvidersSettings
{
    /// <summary>
    /// Enable external providers
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Require user to exist before linking
    /// </summary>
    public bool RequireExistingUser { get; set; } = true;

    /// <summary>
    /// Auto-create user if not exists (only if RequireExistingUser is false)
    /// </summary>
    public bool AutoCreateUser { get; set; } = false;

    /// <summary>
    /// Azure AD configuration
    /// </summary>
    public AzureAdSettings? AzureAd { get; set; }

    /// <summary>
    /// Google configuration
    /// </summary>
    public GoogleSettings? Google { get; set; }

    /// <summary>
    /// Generic OIDC provider configuration
    /// </summary>
    public List<GenericOidcProviderSettings> OidcProviders { get; set; } = new();
}

/// <summary>
/// Azure AD settings
/// </summary>
public class AzureAdSettings
{
    public bool Enabled { get; set; } = false;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string CallbackPath { get; set; } = "/signin-azuread";
}

/// <summary>
/// Google settings
/// </summary>
public class GoogleSettings
{
    public bool Enabled { get; set; } = false;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = "/signin-google";
}

/// <summary>
/// Generic OIDC provider settings
/// </summary>
public class GenericOidcProviderSettings
{
    public bool Enabled { get; set; } = false;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackPath { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;
}
