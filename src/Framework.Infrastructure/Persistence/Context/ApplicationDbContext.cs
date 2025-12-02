using System.Linq.Expressions;
using Framework.Application.Common.Interfaces;
using Framework.Domain.Auditing;
using Framework.Domain.Common.Entities;
using Framework.Domain.Common.Events;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Email;
using Framework.Domain.Identity;
using Framework.Domain.Localization;
using Framework.Domain.MultiTenancy;
using Framework.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Framework.Infrastructure.Persistence.Context;

/// <summary>
/// Main application database context
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IDateTime? _dateTime;
    private readonly IMediator? _mediator;
    private readonly ITenantContext? _tenantContext;

    // Identity DbSets
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();
    public DbSet<ApplicationUserRole> UserRoles => Set<ApplicationUserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Multi-tenancy DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();

    // User profile DbSets
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // Localization DbSets
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<SupportedLanguage> SupportedLanguages => Set<SupportedLanguage>();

    // Auditing DbSets
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Email DbSets
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    // Notification DbSets
    public DbSet<Notification> Notifications => Set<Notification>();

    // API Key DbSets
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // Two-Factor Auth DbSets
    public DbSet<TwoFactorAuthenticator> TwoFactorAuthenticators => Set<TwoFactorAuthenticator>();
    public DbSet<TwoFactorRecoveryCode> TwoFactorRecoveryCodes => Set<TwoFactorRecoveryCode>();

    // External Login DbSets
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUser currentUser,
        IDateTime dateTime,
        IMediator mediator,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
        _mediator = mediator;
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure Identity entities
        ConfigureIdentityEntities(modelBuilder);

        // Configure Tenant entity
        ConfigureTenantEntity(modelBuilder);

        // Configure TenantFeature entity
        ConfigureTenantFeatureEntity(modelBuilder);

        // Configure UserProfile entity
        ConfigureUserProfileEntity(modelBuilder);

        // Configure Localization entities
        ConfigureLocalizationEntities(modelBuilder);

        // Configure AuditLog entity
        ConfigureAuditLogEntity(modelBuilder);

        // Configure Email entities
        ConfigureEmailEntities(modelBuilder);

        // Configure Notification entities
        ConfigureNotificationEntities(modelBuilder);

        // Configure ApiKey entities
        ConfigureApiKeyEntities(modelBuilder);

        // Configure TwoFactor entities
        ConfigureTwoFactorEntities(modelBuilder);

        // Configure ExternalLogin entities
        ConfigureExternalLoginEntities(modelBuilder);

        // Apply global query filters
        ApplyGlobalFilters(modelBuilder);
    }

    private static void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.ServiceName).HasMaxLength(200);
            entity.Property(e => e.MethodName).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.Action);
        });
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);
            var isMultiTenant = typeof(IMultiTenant).IsAssignableFrom(clrType);

            if (isSoftDelete || isMultiTenant)
            {
                var filter = CreateCombinedFilter(clrType, isSoftDelete, isMultiTenant);
                if (filter != null)
                {
                    modelBuilder.Entity(clrType).HasQueryFilter(filter);
                }
            }
        }
    }

    private LambdaExpression? CreateCombinedFilter(Type entityType, bool isSoftDelete, bool isMultiTenant)
    {
        var parameter = Expression.Parameter(entityType, "e");
        Expression? combinedFilter = null;

        // Soft delete filter: !e.IsDeleted
        if (isSoftDelete)
        {
            var isDeletedProperty = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Equal(isDeletedProperty, Expression.Constant(false));
            combinedFilter = notDeleted;
        }

        // Multi-tenant filter: e.TenantId == _tenantContext.TenantId || e.TenantId == null
        // Only apply if _tenantContext is available
        if (isMultiTenant && _tenantContext != null)
        {
            var tenantIdProperty = Expression.Property(parameter, nameof(IMultiTenant.TenantId));

            // Get current tenant ID from context
            var tenantContextField = Expression.Field(
                Expression.Constant(this),
                typeof(ApplicationDbContext).GetField("_tenantContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!);

            var tenantIdAccessor = Expression.Property(tenantContextField, nameof(ITenantContext.TenantId));

            // e.TenantId == currentTenantId
            var tenantMatch = Expression.Equal(tenantIdProperty, Expression.Convert(tenantIdAccessor, typeof(Guid?)));

            // e.TenantId == null (host-level data accessible to all)
            var isHostData = Expression.Equal(tenantIdProperty, Expression.Constant(null, typeof(Guid?)));

            // Combined: (e.TenantId == currentTenantId) || (e.TenantId == null)
            var tenantFilter = Expression.OrElse(tenantMatch, isHostData);

            combinedFilter = combinedFilter != null
                ? Expression.AndAlso(combinedFilter, tenantFilter)
                : tenantFilter;
        }

        // Return null if no filters to apply
        if (combinedFilter == null)
            return null;

        return Expression.Lambda(combinedFilter, parameter);
    }

    private static void ConfigureIdentityEntities(ModelBuilder modelBuilder)
    {
        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.UserName).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);

            entity.HasIndex(e => e.NormalizedEmail).IsUnique();
            entity.HasIndex(e => e.NormalizedUserName).IsUnique();

            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationRole configuration
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.NormalizedName).IsUnique();

            entity.HasMany(e => e.UserRoles)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Permissions)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationUserRole configuration (join table)
        modelBuilder.Entity<ApplicationUserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
        });

        // RolePermission configuration
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);

            // Guid is assigned in constructor, don't let EF generate it
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Permission).HasMaxLength(100).IsRequired();

            entity.HasIndex(e => new { e.RoleId, e.Permission }).IsUnique();
        });
    }

    private static void ConfigureTenantEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(100);
            entity.Property(e => e.Identifier).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ConnectionString).HasMaxLength(500);
            entity.Property(e => e.AdminEmail).HasMaxLength(256);

            entity.HasIndex(e => e.Identifier).IsUnique();
            entity.HasIndex(e => e.NormalizedName);
        });
    }

    private static void ConfigureTenantFeatureEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantFeature>(entity =>
        {
            entity.ToTable("TenantFeatures");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FeatureName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Configuration).HasMaxLength(2000);

            entity.HasIndex(e => new { e.TenantId, e.FeatureName }).IsUnique();

            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserProfileEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DateFormat).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TimeFormat).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Locale).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NumberFormatLocale).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ThemeColorName).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<UserProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLocalizationEntities(ModelBuilder modelBuilder)
    {
        // LocalizationResource configuration
        modelBuilder.Entity<LocalizationResource>(entity =>
        {
            entity.ToTable("LocalizationResources");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Key).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CultureName).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Group).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => new { e.Key, e.CultureName, e.TenantId }).IsUnique();
            entity.HasIndex(e => e.CultureName);
            entity.HasIndex(e => e.Group);
            entity.HasIndex(e => e.TenantId);
        });

        // SupportedLanguage configuration
        modelBuilder.Entity<SupportedLanguage>(entity =>
        {
            entity.ToTable("SupportedLanguages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CultureName).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NativeName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FlagCode).HasMaxLength(10);

            entity.HasIndex(e => e.CultureName).IsUnique();
            entity.HasIndex(e => e.IsDefault);
        });
    }

    private static void ConfigureEmailEntities(ModelBuilder modelBuilder)
    {
        // EmailTemplate configuration
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EmailTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LanguageCode).HasMaxLength(20);

            entity.HasIndex(e => new { e.Name, e.LanguageCode }).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
        });

        // EmailLog configuration
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EmailLogs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.To).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Cc).HasMaxLength(1000);
            entity.Property(e => e.Bcc).HasMaxLength(1000);
            entity.Property(e => e.Subject).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasIndex(e => e.SentAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TemplateId);
        });
    }

    private static void ConfigureNotificationEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasMaxLength(4000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsRead);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
        });
    }

    private static void ConfigureApiKeyEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.KeyHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.KeyPrefix).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LastUsedIp).HasMaxLength(50);
            entity.Property(e => e.Scopes).HasMaxLength(2000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.KeyPrefix);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTwoFactorEntities(ModelBuilder modelBuilder)
    {
        // TwoFactorAuthenticator configuration
        modelBuilder.Entity<TwoFactorAuthenticator>(entity =>
        {
            entity.ToTable("TwoFactorAuthenticators");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SecretKey).HasMaxLength(500).IsRequired();

            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // TwoFactorRecoveryCode configuration
        modelBuilder.Entity<TwoFactorRecoveryCode>(entity =>
        {
            entity.ToTable("TwoFactorRecoveryCodes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CodeHash).HasMaxLength(500).IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsUsed });
        });
    }

    private static void ConfigureExternalLoginEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("ExternalLogins");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProviderKey).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ProviderDisplayName).HasMaxLength(200);

            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Apply tenant ID to new multi-tenant entities
        ApplyTenantId();

        // Apply audit information
        ApplyAuditInfo();

        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantId()
    {
        if (_tenantContext == null || !_tenantContext.IsEnabled)
            return;

        var tenantId = _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                // Only set tenant ID if not already set and we have a current tenant
                if (entry.Entity.TenantId == null && tenantId != null)
                {
                    // Use reflection to set TenantId since interface property has no setter
                    var property = entry.Entity.GetType().GetProperty(nameof(IMultiTenant.TenantId));
                    if (property?.CanWrite == true)
                    {
                        property.SetValue(entry.Entity, tenantId);
                    }
                    else
                    {
                        // Try using the SetTenantId method from MultiTenantEntity
                        var method = entry.Entity.GetType().GetMethod("SetTenantId");
                        method?.Invoke(entry.Entity, new object?[] { tenantId });
                    }
                }
            }
        }
    }

    private void ApplyAuditInfo()
    {
        var timestamp = _dateTime?.UtcNow ?? DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is AuditableEntity<Guid> auditableGuid)
                    {
                        auditableGuid.SetCreated(timestamp, userId);
                    }
                    else if (entry.Entity is AuditableEntity auditable)
                    {
                        auditable.SetCreated(timestamp, userId);
                    }
                    break;

                case EntityState.Modified:
                    if (entry.Entity is AuditableEntity<Guid> modifiedGuid)
                    {
                        modifiedGuid.SetModified(timestamp, userId);
                    }
                    else if (entry.Entity is AuditableEntity modified)
                    {
                        modified.SetModified(timestamp, userId);
                    }
                    break;
            }
        }

        // Handle soft delete
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                if (entry.Entity is FullAuditableEntity<Guid> fullAuditableGuid)
                {
                    fullAuditableGuid.SoftDelete(timestamp, userId);
                }
                else if (entry.Entity is FullAuditableEntity fullAuditable)
                {
                    fullAuditable.SoftDelete(timestamp, userId);
                }
            }
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (_mediator == null) return;

        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

}
