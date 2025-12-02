using System.Security.Cryptography;
using Framework.Application.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace Framework.Infrastructure.OpenIdConnect;

/// <summary>
/// OpenID Connect service extensions
/// </summary>
public static class OpenIdConnectExtensions
{
    /// <summary>
    /// Add OpenID Connect server (OpenIddict)
    /// </summary>
    public static IServiceCollection AddOpenIdConnectServer<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        var settings = configuration.GetSection(OpenIdConnectSettings.SectionName).Get<OpenIdConnectSettings>()
            ?? new OpenIdConnectSettings();

        services.Configure<OpenIdConnectSettings>(configuration.GetSection(OpenIdConnectSettings.SectionName));

        if (!settings.Enabled)
            return services;

        services.AddOpenIddict()
            // Register the OpenIddict core components
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TContext>();
            })
            // Register the OpenIddict server components
            .AddServer(options =>
            {
                // Enable the required endpoints
                options.SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetIntrospectionEndpointUris("connect/introspect")
                    .SetRevocationEndpointUris("connect/revoke")
                    .SetEndSessionEndpointUris("connect/logout");

                // Enable required flows
                if (settings.EnableAuthorizationCodeFlow)
                    options.AllowAuthorizationCodeFlow();

                if (settings.EnableClientCredentialsFlow)
                    options.AllowClientCredentialsFlow();

                if (settings.EnableRefreshTokenFlow)
                    options.AllowRefreshTokenFlow();

                if (settings.EnablePasswordFlow)
                    options.AllowPasswordFlow();

                // Set token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(settings.RefreshTokenLifetimeDays));
                options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(settings.IdentityTokenLifetimeMinutes));
                options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(settings.AuthorizationCodeLifetimeMinutes));

                // Register scopes
                options.RegisterScopes(settings.Scopes.ToArray());

                // Configure signing and encryption
                if (settings.UseDevelopmentCertificates)
                {
                    options.AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    if (!string.IsNullOrEmpty(settings.EncryptionKey))
                    {
                        var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.EncryptionKey));
                        options.AddEncryptionKey(encryptionKey);
                    }

                    if (!string.IsNullOrEmpty(settings.SigningKey))
                    {
                        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKey));
                        options.AddSigningKey(signingKey);
                    }
                }

                // Require PKCE
                if (settings.RequirePkce)
                {
                    options.RequireProofKeyForCodeExchange();
                }

                // Register ASP.NET Core host
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();
            })
            // Register the OpenIddict validation components
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Register external login service
        services.AddScoped<IExternalLoginService, ExternalLoginService>();

        return services;
    }

    /// <summary>
    /// Add external identity providers
    /// </summary>
    public static AuthenticationBuilder AddExternalProviders(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(OpenIdConnectSettings.SectionName).Get<OpenIdConnectSettings>()
            ?? new OpenIdConnectSettings();

        if (!settings.ExternalProviders.Enabled)
            return builder;

        // Azure AD
        if (settings.ExternalProviders.AzureAd?.Enabled == true)
        {
            var azureAd = settings.ExternalProviders.AzureAd;
            builder.AddOpenIdConnect("AzureAD", "Azure AD", options =>
            {
                options.Authority = $"{azureAd.Instance}{azureAd.TenantId}/v2.0";
                options.ClientId = azureAd.ClientId;
                options.ClientSecret = azureAd.ClientSecret;
                options.CallbackPath = azureAd.CallbackPath;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };
            });
        }

        // Google
        if (settings.ExternalProviders.Google?.Enabled == true)
        {
            var google = settings.ExternalProviders.Google;
            builder.AddGoogle("Google", options =>
            {
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.CallbackPath = google.CallbackPath;
                options.SaveTokens = true;
            });
        }

        // Generic OIDC providers
        foreach (var provider in settings.ExternalProviders.OidcProviders.Where(p => p.Enabled))
        {
            builder.AddOpenIdConnect(provider.Name, provider.DisplayName, options =>
            {
                options.Authority = provider.Authority;
                options.ClientId = provider.ClientId;
                options.ClientSecret = provider.ClientSecret;
                options.CallbackPath = provider.CallbackPath;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = provider.GetClaimsFromUserInfoEndpoint;

                foreach (var scope in provider.Scopes)
                {
                    options.Scope.Add(scope);
                }
            });
        }

        return builder;
    }

    /// <summary>
    /// Seed OpenIddict clients from configuration
    /// </summary>
    public static async Task SeedOpenIdConnectClientsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var settings = configuration.GetSection(OpenIdConnectSettings.SectionName).Get<OpenIdConnectSettings>()
            ?? new OpenIdConnectSettings();

        foreach (var clientSettings in settings.Clients)
        {
            var client = await manager.FindByClientIdAsync(clientSettings.ClientId, cancellationToken);

            if (client == null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = clientSettings.ClientId,
                    DisplayName = clientSettings.DisplayName,
                    ClientType = clientSettings.ClientType == OidcClientType.Public
                        ? OpenIddictConstants.ClientTypes.Public
                        : OpenIddictConstants.ClientTypes.Confidential,
                    ConsentType = clientSettings.RequireConsent
                        ? OpenIddictConstants.ConsentTypes.Explicit
                        : OpenIddictConstants.ConsentTypes.Implicit
                };

                if (!string.IsNullOrEmpty(clientSettings.ClientSecret))
                {
                    descriptor.ClientSecret = clientSettings.ClientSecret;
                }

                foreach (var uri in clientSettings.RedirectUris)
                {
                    descriptor.RedirectUris.Add(new Uri(uri));
                }

                foreach (var uri in clientSettings.PostLogoutRedirectUris)
                {
                    descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                }

                foreach (var grantType in clientSettings.AllowedGrantTypes)
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.GrantType + grantType);
                }

                foreach (var s in clientSettings.AllowedScopes)
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + s);
                }

                // Add standard permissions
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

                if (clientSettings.RequirePkce)
                {
                    descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
                }

                await manager.CreateAsync(descriptor, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Generate a secure random key for signing/encryption
    /// </summary>
    public static string GenerateSecureKey(int bytes = 32)
    {
        var key = new byte[bytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }
}
