using Framework.Application.Identity;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Email;
using Framework.Domain.Identity;
using Framework.Domain.Localization;
using Framework.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Framework.Infrastructure.Persistence;

/// <summary>
/// Service for seeding initial application data
/// </summary>
public class SeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(
        ApplicationDbContext context,
        IDateTime dateTime,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger<SeedDataService> logger)
    {
        _context = context;
        _dateTime = dateTime;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all initial data
    /// </summary>
    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
        await SyncAdminRolePermissionsAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
        await SeedLanguagesAsync(cancellationToken);
        await SeedLocalizationResourcesAsync(cancellationToken);
        await SeedEmailTemplatesAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the Administrator role has all defined permissions (for when new permissions are added)
    /// </summary>
    public async Task SyncAdminRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        var adminRole = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == "Administrator", cancellationToken);

        if (adminRole == null)
            return;

        var allPermissions = Permissions.GetAll().ToHashSet();
        var existingPermissions = adminRole.Permissions.Select(p => p.Permission).ToHashSet();
        var missingPermissions = allPermissions.Except(existingPermissions).ToList();

        if (missingPermissions.Count == 0)
            return;

        _logger.LogInformation("Adding {Count} missing permissions to Administrator role: {Permissions}",
            missingPermissions.Count, string.Join(", ", missingPermissions));

        foreach (var permission in missingPermissions)
        {
            adminRole.Permissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                Permission = permission
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Seeds the default admin user
    /// </summary>
    public async Task SeedAdminUserAsync(CancellationToken cancellationToken = default)
    {
        const string adminEmail = "admin@framework.local";

        if (await _context.Users.AnyAsync(u => u.Email == adminEmail, cancellationToken))
        {
            _logger.LogInformation("Admin user already exists, skipping...");
            return;
        }

        _logger.LogInformation("Seeding default admin user...");

        var timestamp = _dateTime.Now;

        // Get the Administrator role
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator", cancellationToken);
        if (adminRole == null)
        {
            _logger.LogWarning("Administrator role not found. Cannot seed admin user.");
            return;
        }

        var adminUser = new ApplicationUser(Guid.NewGuid())
        {
            Email = adminEmail,
            NormalizedEmail = adminEmail.ToUpperInvariant(),
            UserName = adminEmail,
            NormalizedUserName = adminEmail.ToUpperInvariant(),
            FirstName = "System",
            LastName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        // Hash the default password: Admin@123
        adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "Admin@123");
        adminUser.SetCreated(timestamp, "system");

        // Assign admin role
        adminUser.UserRoles.Add(new ApplicationUserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default admin user: {Email} with password: Admin@123", adminEmail);
    }

    /// <summary>
    /// Seeds default roles with permissions
    /// </summary>
    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Roles.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Roles already seeded, skipping...");
            return;
        }

        _logger.LogInformation("Seeding default roles...");

        var timestamp = _dateTime.Now;

        // Administrator role with all permissions
        var adminRole = new ApplicationRole(Guid.NewGuid())
        {
            Name = "Administrator",
            NormalizedName = "ADMINISTRATOR",
            Description = "Full system administrator with all permissions",
            IsSystem = true,
            IsDefault = false
        };
        adminRole.SetCreated(timestamp, "system");

        foreach (var permission in Permissions.GetAll())
        {
            adminRole.Permissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                Permission = permission
            });
        }

        // Manager role with management permissions
        var managerRole = new ApplicationRole(Guid.NewGuid())
        {
            Name = "Manager",
            NormalizedName = "MANAGER",
            Description = "Manager with user and content management permissions",
            IsSystem = true,
            IsDefault = false
        };
        managerRole.SetCreated(timestamp, "system");

        var managerPermissions = new[]
        {
            Permissions.UsersView, Permissions.UsersCreate, Permissions.UsersEdit,
            Permissions.UsersManageRoles, Permissions.UsersResetPassword,
            Permissions.RolesView,
            Permissions.PermissionsView,
            Permissions.TenantsView
        };

        foreach (var permission in managerPermissions)
        {
            managerRole.Permissions.Add(new RolePermission
            {
                RoleId = managerRole.Id,
                Permission = permission
            });
        }

        // Basic User role with view permissions
        var userRole = new ApplicationRole(Guid.NewGuid())
        {
            Name = "User",
            NormalizedName = "USER",
            Description = "Basic user with limited permissions",
            IsSystem = true,
            IsDefault = true
        };
        userRole.SetCreated(timestamp, "system");

        var userPermissions = new[]
        {
            Permissions.UsersView,
            Permissions.RolesView,
            Permissions.PermissionsView
        };

        foreach (var permission in userPermissions)
        {
            userRole.Permissions.Add(new RolePermission
            {
                RoleId = userRole.Id,
                Permission = permission
            });
        }

        _context.Roles.AddRange(adminRole, managerRole, userRole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default roles", 3);
    }

    /// <summary>
    /// Seeds default supported languages
    /// </summary>
    public async Task SeedLanguagesAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.SupportedLanguages.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Languages already seeded, skipping...");
            return;
        }

        _logger.LogInformation("Seeding default languages...");

        var timestamp = _dateTime.Now;

        var languages = new List<SupportedLanguage>
        {
            CreateLanguage("en-US", "English (United States)", "English", "us", true, true),
            CreateLanguage("en-GB", "English (United Kingdom)", "English", "gb", true, false),
            CreateLanguage("es-ES", "Spanish (Spain)", "Español", "es", true, false),
            CreateLanguage("fr-FR", "French (France)", "Français", "fr", true, false),
            CreateLanguage("de-DE", "German (Germany)", "Deutsch", "de", true, false),
            CreateLanguage("it-IT", "Italian (Italy)", "Italiano", "it", true, false),
            CreateLanguage("pt-BR", "Portuguese (Brazil)", "Português", "br", true, false),
            CreateLanguage("zh-CN", "Chinese (Simplified)", "简体中文", "cn", true, false),
            CreateLanguage("ja-JP", "Japanese", "日本語", "jp", true, false),
            CreateLanguage("ko-KR", "Korean", "한국어", "kr", true, false),
            CreateLanguage("ar-SA", "Arabic (Saudi Arabia)", "العربية", "sa", true, false, isRtl: true)
        };

        foreach (var language in languages)
        {
            language.SetCreated(timestamp, "system");
        }

        _context.SupportedLanguages.AddRange(languages);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default languages", languages.Count);
    }

    /// <summary>
    /// Seeds default localization resources
    /// </summary>
    public async Task SeedLocalizationResourcesAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.LocalizationResources.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Localization resources already seeded, skipping...");
            return;
        }

        _logger.LogInformation("Seeding default localization resources...");

        var timestamp = _dateTime.Now;

        var resources = new List<LocalizationResource>
        {
            // Common
            CreateResource("Common.Save", "en-US", "Save", "Common"),
            CreateResource("Common.Cancel", "en-US", "Cancel", "Common"),
            CreateResource("Common.Delete", "en-US", "Delete", "Common"),
            CreateResource("Common.Edit", "en-US", "Edit", "Common"),
            CreateResource("Common.Create", "en-US", "Create", "Common"),
            CreateResource("Common.Search", "en-US", "Search", "Common"),
            CreateResource("Common.Loading", "en-US", "Loading...", "Common"),
            CreateResource("Common.NoData", "en-US", "No data available", "Common"),
            CreateResource("Common.Confirm", "en-US", "Confirm", "Common"),
            CreateResource("Common.Yes", "en-US", "Yes", "Common"),
            CreateResource("Common.No", "en-US", "No", "Common"),
            CreateResource("Common.Close", "en-US", "Close", "Common"),
            CreateResource("Common.Actions", "en-US", "Actions", "Common"),
            CreateResource("Common.Details", "en-US", "Details", "Common"),
            CreateResource("Common.Back", "en-US", "Back", "Common"),
            CreateResource("Common.Next", "en-US", "Next", "Common"),
            CreateResource("Common.Previous", "en-US", "Previous", "Common"),
            CreateResource("Common.Submit", "en-US", "Submit", "Common"),
            CreateResource("Common.Reset", "en-US", "Reset", "Common"),
            CreateResource("Common.Refresh", "en-US", "Refresh", "Common"),

            // Validation
            CreateResource("Validation.Required", "en-US", "This field is required", "Validation"),
            CreateResource("Validation.Email", "en-US", "Please enter a valid email address", "Validation"),
            CreateResource("Validation.MinLength", "en-US", "Minimum length is {0} characters", "Validation"),
            CreateResource("Validation.MaxLength", "en-US", "Maximum length is {0} characters", "Validation"),
            CreateResource("Validation.PasswordMismatch", "en-US", "Passwords do not match", "Validation"),

            // Auth
            CreateResource("Auth.Login", "en-US", "Login", "Auth"),
            CreateResource("Auth.Logout", "en-US", "Logout", "Auth"),
            CreateResource("Auth.Register", "en-US", "Register", "Auth"),
            CreateResource("Auth.ForgotPassword", "en-US", "Forgot Password", "Auth"),
            CreateResource("Auth.ResetPassword", "en-US", "Reset Password", "Auth"),
            CreateResource("Auth.InvalidCredentials", "en-US", "Invalid email or password", "Auth"),
            CreateResource("Auth.AccountLocked", "en-US", "Your account has been locked", "Auth"),
            CreateResource("Auth.AccountDisabled", "en-US", "Your account has been disabled", "Auth"),

            // Users
            CreateResource("Users.Title", "en-US", "User Management", "Users"),
            CreateResource("Users.Create", "en-US", "Create User", "Users"),
            CreateResource("Users.Edit", "en-US", "Edit User", "Users"),
            CreateResource("Users.Delete", "en-US", "Delete User", "Users"),
            CreateResource("Users.Email", "en-US", "Email", "Users"),
            CreateResource("Users.FirstName", "en-US", "First Name", "Users"),
            CreateResource("Users.LastName", "en-US", "Last Name", "Users"),
            CreateResource("Users.Password", "en-US", "Password", "Users"),
            CreateResource("Users.ConfirmPassword", "en-US", "Confirm Password", "Users"),
            CreateResource("Users.Active", "en-US", "Active", "Users"),
            CreateResource("Users.Roles", "en-US", "Roles", "Users"),

            // Roles
            CreateResource("Roles.Title", "en-US", "Role Management", "Roles"),
            CreateResource("Roles.Create", "en-US", "Create Role", "Roles"),
            CreateResource("Roles.Edit", "en-US", "Edit Role", "Roles"),
            CreateResource("Roles.Delete", "en-US", "Delete Role", "Roles"),
            CreateResource("Roles.Name", "en-US", "Role Name", "Roles"),
            CreateResource("Roles.Description", "en-US", "Description", "Roles"),
            CreateResource("Roles.Permissions", "en-US", "Permissions", "Roles"),
            CreateResource("Roles.Default", "en-US", "Default Role", "Roles"),

            // Tenants
            CreateResource("Tenants.Title", "en-US", "Tenant Management", "Tenants"),
            CreateResource("Tenants.Create", "en-US", "Create Tenant", "Tenants"),
            CreateResource("Tenants.Edit", "en-US", "Edit Tenant", "Tenants"),
            CreateResource("Tenants.Delete", "en-US", "Delete Tenant", "Tenants"),
            CreateResource("Tenants.Name", "en-US", "Tenant Name", "Tenants"),
            CreateResource("Tenants.Identifier", "en-US", "Identifier", "Tenants"),
            CreateResource("Tenants.AdminEmail", "en-US", "Admin Email", "Tenants"),
            CreateResource("Tenants.Features", "en-US", "Features", "Tenants"),

            // Messages
            CreateResource("Messages.SaveSuccess", "en-US", "Changes saved successfully", "Messages"),
            CreateResource("Messages.SaveError", "en-US", "Error saving changes", "Messages"),
            CreateResource("Messages.DeleteSuccess", "en-US", "Item deleted successfully", "Messages"),
            CreateResource("Messages.DeleteError", "en-US", "Error deleting item", "Messages"),
            CreateResource("Messages.DeleteConfirm", "en-US", "Are you sure you want to delete this item?", "Messages"),
            CreateResource("Messages.NotFound", "en-US", "Item not found", "Messages"),
            CreateResource("Messages.AccessDenied", "en-US", "Access denied", "Messages"),
            CreateResource("Messages.ServerError", "en-US", "An error occurred. Please try again later.", "Messages")
        };

        foreach (var resource in resources)
        {
            resource.SetCreated(timestamp, "system");
            resource.MarkAsSystem();
        }

        _context.LocalizationResources.AddRange(resources);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default localization resources", resources.Count);
    }

    /// <summary>
    /// Seeds default email templates
    /// </summary>
    public async Task SeedEmailTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Set<EmailTemplate>().AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Email templates already seeded, skipping...");
            return;
        }

        _logger.LogInformation("Seeding default email templates...");

        var timestamp = _dateTime.Now;

        var templates = new List<EmailTemplate>
        {
            // Welcome / Registration Email
            CreateEmailTemplate(
                "WelcomeEmail",
                "Welcome to {{ApplicationName}}!",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Welcome</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">Welcome to {{ApplicationName}}!</h1>
        <p>Hello {{FirstName}},</p>
        <p>Thank you for registering with us. Your account has been created successfully.</p>
        <p><strong>Your account details:</strong></p>
        <ul>
            <li>Email: {{Email}}</li>
            <li>Username: {{UserName}}</li>
        </ul>
        <p>To get started, please verify your email address by clicking the button below:</p>
        <p style=""text-align: center; margin: 30px 0;"">
            <a href=""{{VerificationLink}}"" style=""background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;"">Verify Email Address</a>
        </p>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #666;"">{{VerificationLink}}</p>
        <p>This link will expire in {{ExpirationHours}} hours.</p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you did not create this account, please ignore this email.</p>
    </div>
</body>
</html>",
                EmailTemplateType.Welcome,
                "Email sent to new users after registration"),

            // Password Reset Email
            CreateEmailTemplate(
                "PasswordReset",
                "Reset Your Password - {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Password Reset</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">Password Reset Request</h1>
        <p>Hello {{FirstName}},</p>
        <p>We received a request to reset the password for your account associated with {{Email}}.</p>
        <p>Click the button below to reset your password:</p>
        <p style=""text-align: center; margin: 30px 0;"">
            <a href=""{{ResetLink}}"" style=""background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;"">Reset Password</a>
        </p>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #666;"">{{ResetLink}}</p>
        <p><strong>This link will expire in {{ExpirationHours}} hours.</strong></p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you did not request a password reset, please ignore this email or contact support if you have concerns about your account security.</p>
    </div>
</body>
</html>",
                EmailTemplateType.PasswordReset,
                "Email sent when user requests password reset"),

            // Email Confirmation
            CreateEmailTemplate(
                "EmailConfirmation",
                "Confirm Your Email - {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Email Confirmation</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">Confirm Your Email Address</h1>
        <p>Hello {{FirstName}},</p>
        <p>Please confirm your email address by clicking the button below:</p>
        <p style=""text-align: center; margin: 30px 0;"">
            <a href=""{{ConfirmationLink}}"" style=""background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;"">Confirm Email</a>
        </p>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #666;"">{{ConfirmationLink}}</p>
        <p>This link will expire in {{ExpirationHours}} hours.</p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you did not request this confirmation, please ignore this email.</p>
    </div>
</body>
</html>",
                EmailTemplateType.EmailConfirmation,
                "Email sent to confirm user's email address"),

            // Two-Factor Authentication Code
            CreateEmailTemplate(
                "TwoFactorCode",
                "Your Verification Code - {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Verification Code</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">Your Verification Code</h1>
        <p>Hello {{FirstName}},</p>
        <p>Your two-factor authentication code is:</p>
        <p style=""text-align: center; margin: 30px 0;"">
            <span style=""font-size: 32px; font-weight: bold; letter-spacing: 8px; background-color: #f3f4f6; padding: 16px 32px; border-radius: 8px; display: inline-block;"">{{Code}}</span>
        </p>
        <p><strong>This code will expire in {{ExpirationMinutes}} minutes.</strong></p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you did not request this code, please secure your account immediately by changing your password.</p>
    </div>
</body>
</html>",
                EmailTemplateType.TwoFactorCode,
                "Email sent with 2FA verification code"),

            // Account Locked
            CreateEmailTemplate(
                "AccountLocked",
                "Account Security Alert - {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Account Locked</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #dc2626;"">Account Locked</h1>
        <p>Hello {{FirstName}},</p>
        <p>Your account has been temporarily locked due to multiple failed login attempts.</p>
        <p><strong>Details:</strong></p>
        <ul>
            <li>Account: {{Email}}</li>
            <li>Locked at: {{LockoutTime}}</li>
            <li>Unlock time: {{UnlockTime}}</li>
        </ul>
        <p>If this was you, please wait until the lockout period expires and try again with the correct password.</p>
        <p>If you've forgotten your password, you can <a href=""{{ResetPasswordLink}}"" style=""color: #2563eb;"">reset it here</a>.</p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you did not attempt to login, please contact support immediately as someone may be trying to access your account.</p>
    </div>
</body>
</html>",
                EmailTemplateType.AccountLocked,
                "Email sent when account is locked due to failed login attempts"),

            // Password Changed
            CreateEmailTemplate(
                "PasswordChanged",
                "Password Changed - {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Password Changed</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">Password Changed Successfully</h1>
        <p>Hello {{FirstName}},</p>
        <p>This email confirms that the password for your account ({{Email}}) was changed on {{ChangeTime}}.</p>
        <p>If you made this change, no further action is needed.</p>
        <p style=""background-color: #fef2f2; border: 1px solid #fecaca; padding: 16px; border-radius: 8px; margin: 20px 0;"">
            <strong style=""color: #dc2626;"">Didn't make this change?</strong><br>
            If you did not change your password, your account may have been compromised. Please <a href=""{{ResetPasswordLink}}"" style=""color: #2563eb;"">reset your password</a> immediately and contact our support team.
        </p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">This is an automated security notification.</p>
    </div>
</body>
</html>",
                EmailTemplateType.PasswordChanged,
                "Email sent after password is changed"),

            // Invite User
            CreateEmailTemplate(
                "InviteUser",
                "You've Been Invited to {{ApplicationName}}",
                @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Invitation</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <h1 style=""color: #2563eb;"">You're Invited!</h1>
        <p>Hello,</p>
        <p>{{InviterName}} has invited you to join {{ApplicationName}}.</p>
        {{#if PersonalMessage}}
        <p style=""background-color: #f3f4f6; padding: 16px; border-radius: 8px; font-style: italic;"">
            ""{{PersonalMessage}}""
        </p>
        {{/if}}
        <p>Click the button below to accept the invitation and create your account:</p>
        <p style=""text-align: center; margin: 30px 0;"">
            <a href=""{{InvitationLink}}"" style=""background-color: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;"">Accept Invitation</a>
        </p>
        <p>If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #666;"">{{InvitationLink}}</p>
        <p><strong>This invitation will expire in {{ExpirationDays}} days.</strong></p>
        <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;"">
        <p style=""color: #666; font-size: 12px;"">If you were not expecting this invitation, you can safely ignore this email.</p>
    </div>
</body>
</html>",
                EmailTemplateType.InviteUser,
                "Email sent to invite new users to the platform")
        };

        foreach (var template in templates)
        {
            template.SetCreated(timestamp, "system");
        }

        _context.Set<EmailTemplate>().AddRange(templates);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} default email templates", templates.Count);
    }

    private static EmailTemplate CreateEmailTemplate(
        string name, string subject, string body, EmailTemplateType type, string? description = null)
    {
        var template = new EmailTemplate(Guid.NewGuid(), name, subject, body, type);
        if (description != null)
        {
            template.Update(subject, body, description);
        }
        return template;
    }

    private static SupportedLanguage CreateLanguage(
        string cultureName, string displayName, string nativeName,
        string flagCode, bool isEnabled, bool isDefault, bool isRtl = false)
    {
        var language = new SupportedLanguage(cultureName, displayName, nativeName);
        language.Update(displayName, nativeName, flagCode, isRtl, 0);

        if (isEnabled)
            language.Enable();
        else
            language.Disable();

        if (isDefault)
            language.SetAsDefault();

        return language;
    }

    private static LocalizationResource CreateResource(
        string key, string cultureName, string value, string group)
    {
        var resource = new LocalizationResource(key, cultureName, value);
        resource.UpdateMetadata(group, null);
        return resource;
    }
}

/// <summary>
/// Extension methods for seeding data
/// </summary>
public static class SeedDataExtensions
{
    /// <summary>
    /// Seeds initial application data
    /// </summary>
    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.SeedAllAsync();
    }
}
