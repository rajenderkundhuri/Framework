using System.Text;
using Framework.Application.BackgroundJobs;
using Framework.Application.Caching;
using Framework.Application.Common.Interfaces;
using Framework.Application.Email;
using Framework.Application.HealthChecks;
using Framework.Application.Identity;
using Framework.Application.Identity.Interfaces;
using Framework.Application.Auditing;
using Framework.Application.Localization;
using Framework.Application.MultiTenancy;
using Framework.Application.Notifications;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Framework.Domain.MultiTenancy;
using Framework.Infrastructure.Auditing;
using Framework.Infrastructure.Caching;
using Framework.Infrastructure.Email;
using Framework.Infrastructure.HealthChecks;
using Framework.Infrastructure.Identity;
using Framework.Infrastructure.Localization;
using Framework.Infrastructure.MultiTenancy;
using Framework.Infrastructure.Notifications;
using Framework.Infrastructure.Persistence;
using Framework.Infrastructure.Persistence.Context;
using Framework.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Framework.Infrastructure;

/// <summary>
/// Dependency injection extensions for the Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HTTP Context Accessor (needed by audit interceptor)
        services.AddHttpContextAccessor();

        // Audit services (must be registered before DbContext)
        services.Configure<AuditSettings>(configuration.GetSection(AuditSettings.SectionName));
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuditInterceptor>();

        // Database context with configurable provider
        services.AddDatabase(configuration);

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(Repository<,>));

        // Services
        services.AddScoped<IDateTime, SystemDateTime>();
        services.AddScoped<ICurrentUser, CurrentUserService>();


        // Register DbContext for Identity services
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Password hasher
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

        // Identity services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        // Multi-tenancy services
        services.AddMemoryCache();
        services.Configure<MultiTenancySettings>(configuration.GetSection(MultiTenancySettings.SectionName));
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantStore, TenantStore>();

        // Tenant resolution strategies
        services.AddScoped<ITenantResolutionStrategy, HeaderTenantResolutionStrategy>();
        services.AddScoped<ITenantResolutionStrategy, QueryStringTenantResolutionStrategy>();
        services.AddScoped<ITenantResolutionStrategy, SubdomainTenantResolutionStrategy>();
        services.AddScoped<ITenantResolutionStrategy, RouteTenantResolutionStrategy>();
        services.AddScoped<ITenantResolutionStrategy, ClaimTenantResolutionStrategy>();

        // User management services
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Role management services
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        // Tenant management services
        services.AddScoped<ITenantManagementService, TenantManagementService>();

        // Localization services
        services.Configure<LocalizationSettings>(configuration.GetSection(LocalizationSettings.SectionName));
        services.AddScoped<IResourceManagementService, ResourceManagementService>();
        services.AddScoped(typeof(ILocalizer<>), typeof(Localizer<>));

        // Seed data service
        services.AddScoped<SeedDataService>();

        // Notification services
        services.AddScoped<INotificationService, NotificationService>();

        // Two-factor authentication services
        services.AddScoped<ITwoFactorService, TwoFactorService>();

        // API Key services
        services.AddScoped<IApiKeyService, ApiKeyService>();

        // Email template management services
        services.AddScoped<IEmailTemplateManagementService, EmailTemplateManagementService>();

        // Caching services
        services.AddCaching(configuration);

        // Health check services
        services.Configure<HealthCheckSettings>(configuration.GetSection(HealthCheckSettings.SectionName));
        services.AddScoped<HealthCheckService>();
        services.AddScoped<IHealthCheck, MemoryHealthCheck>();

        return services;
    }

    /// <summary>
    /// Adds JWT authentication and authorization
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception is SecurityTokenExpiredException)
                    {
                        context.Response.Headers["Token-Expired"] = "true";
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Permission-based authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization();

        return services;
    }
}
