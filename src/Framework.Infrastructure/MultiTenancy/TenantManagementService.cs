using Framework.Application.Common.Models;
using Framework.Application.MultiTenancy;
using Framework.Domain.Common.Interfaces;
using Framework.Domain.Identity;
using Framework.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Framework.Infrastructure.MultiTenancy;

/// <summary>
/// Tenant management service implementation
/// </summary>
public class TenantManagementService : ITenantManagementService
{
    private readonly DbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<TenantManagementService> _logger;

    public TenantManagementService(
        DbContext context,
        ICurrentUser currentUser,
        IDateTime dateTime,
        ILogger<TenantManagementService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    #region Tenant CRUD

    public async Task<Result<PagedList<TenantListResponse>>> GetTenantsAsync(
        TenantListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Tenant>()
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(search) ||
                t.Identifier.ToLower().Contains(search) ||
                (t.AdminEmail != null && t.AdminEmail.ToLower().Contains(search)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "identifier" => request.SortDescending
                ? query.OrderByDescending(t => t.Identifier)
                : query.OrderBy(t => t.Identifier),
            "createdat" => request.SortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            "isactive" => request.SortDescending
                ? query.OrderByDescending(t => t.IsActive)
                : query.OrderBy(t => t.IsActive),
            _ => request.SortDescending
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var tenants = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TenantListResponse
            {
                Id = t.Id,
                Name = t.Name,
                Identifier = t.Identifier,
                AdminEmail = t.AdminEmail,
                IsActive = t.IsActive,
                ValidUpto = t.ValidUpto,
                CreatedAt = t.CreatedAt,
                EnabledFeatureCount = _context.Set<TenantFeature>().Count(f => f.TenantId == t.Id && f.IsEnabled)
            })
            .ToListAsync(cancellationToken);

        return new PagedList<TenantListResponse>(tenants, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<Result<TenantDetailResponse>> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result<TenantDetailResponse>.NotFound("Tenant not found");

        var features = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return new TenantDetailResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Identifier = tenant.Identifier,
            AdminEmail = tenant.AdminEmail,
            ConnectionString = tenant.ConnectionString,
            IsActive = tenant.IsActive,
            ValidUpto = tenant.ValidUpto,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            LastModifiedAt = tenant.LastModifiedAt,
            LastModifiedBy = tenant.LastModifiedBy,
            IsValid = tenant.IsValid(),
            Features = features.Select(f => new TenantFeatureResponse
            {
                Id = f.Id,
                FeatureName = f.FeatureName,
                DisplayName = GetFeatureDisplayName(f.FeatureName),
                Description = GetFeatureDescription(f.FeatureName),
                IsEnabled = f.IsEnabled,
                Configuration = f.Configuration,
                ExpiresAt = f.ExpiresAt,
                IsActive = f.IsActive()
            }).ToList()
        };
    }

    public async Task<Result<TenantDetailResponse>> GetTenantByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Identifier == identifier.ToLowerInvariant() && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result<TenantDetailResponse>.NotFound("Tenant not found");

        return await GetTenantByIdAsync(tenant.Id, cancellationToken);
    }

    public async Task<Result<Guid>> CreateTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for existing identifier
        var existingTenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Identifier == request.Identifier.ToLowerInvariant(), cancellationToken);

        if (existingTenant != null)
            return Result<Guid>.Conflict("A tenant with this identifier already exists");

        var tenant = new Tenant(request.Name, request.Identifier, request.AdminEmail);

        if (!string.IsNullOrWhiteSpace(request.ConnectionString))
            tenant.SetConnectionString(request.ConnectionString);

        if (request.ValidUpto.HasValue)
            tenant.SetValidity(request.ValidUpto);

        if (!request.IsActive)
            tenant.Deactivate();

        tenant.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");

        _context.Set<Tenant>().Add(tenant);

        // Add features
        foreach (var featureRequest in request.Features)
        {
            var feature = new TenantFeature(
                Guid.NewGuid(),
                tenant.Id,
                featureRequest.FeatureName,
                featureRequest.IsEnabled);

            if (!string.IsNullOrWhiteSpace(featureRequest.Configuration))
                feature.SetConfiguration(featureRequest.Configuration);

            if (featureRequest.ExpiresAt.HasValue)
                feature.SetExpiration(featureRequest.ExpiresAt);

            feature.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");
            _context.Set<TenantFeature>().Add(feature);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tenant {TenantId} with identifier {Identifier}", tenant.Id, tenant.Identifier);

        return tenant.Id;
    }

    public async Task<Result> UpdateTenantAsync(
        Guid tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result.NotFound("Tenant not found");

        if (!string.IsNullOrWhiteSpace(request.Name))
            tenant.UpdateName(request.Name);

        if (request.ConnectionString != null)
            tenant.SetConnectionString(request.ConnectionString);

        if (request.ValidUpto.HasValue)
            tenant.SetValidity(request.ValidUpto);

        tenant.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated tenant {TenantId}", tenantId);

        return Result.Success();
    }

    public async Task<Result> DeleteTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result.NotFound("Tenant not found");

        tenant.SoftDelete(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tenant {TenantId}", tenantId);

        return Result.Success();
    }

    public async Task<Result> ActivateTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result.NotFound("Tenant not found");

        tenant.Activate();
        tenant.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated tenant {TenantId}", tenantId);

        return Result.Success();
    }

    public async Task<Result> DeactivateTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result.NotFound("Tenant not found");

        tenant.Deactivate();
        tenant.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated tenant {TenantId}", tenantId);

        return Result.Success();
    }

    #endregion

    #region Feature Management

    public async Task<Result<IEnumerable<TenantFeatureResponse>>> GetTenantFeaturesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenantExists = await _context.Set<Tenant>()
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (!tenantExists)
            return Result<IEnumerable<TenantFeatureResponse>>.NotFound("Tenant not found");

        var features = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == tenantId)
            .Select(f => new TenantFeatureResponse
            {
                Id = f.Id,
                FeatureName = f.FeatureName,
                DisplayName = GetFeatureDisplayName(f.FeatureName),
                Description = GetFeatureDescription(f.FeatureName),
                IsEnabled = f.IsEnabled,
                Configuration = f.Configuration,
                ExpiresAt = f.ExpiresAt,
                IsActive = f.IsEnabled && (!f.ExpiresAt.HasValue || f.ExpiresAt > DateTimeOffset.UtcNow)
            })
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<TenantFeatureResponse>>.Success(features);
    }

    public async Task<Result> EnableFeatureAsync(
        Guid tenantId,
        string featureName,
        string? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var tenantExists = await _context.Set<Tenant>()
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (!tenantExists)
            return Result.NotFound("Tenant not found");

        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureName == featureName, cancellationToken);

        if (feature == null)
        {
            feature = new TenantFeature(Guid.NewGuid(), tenantId, featureName, true);
            if (!string.IsNullOrWhiteSpace(configuration))
                feature.SetConfiguration(configuration);
            feature.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");
            _context.Set<TenantFeature>().Add(feature);
        }
        else
        {
            feature.Enable();
            if (!string.IsNullOrWhiteSpace(configuration))
                feature.SetConfiguration(configuration);
            feature.SetModified(_dateTime.Now, _currentUser.UserId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Enabled feature {FeatureName} for tenant {TenantId}", featureName, tenantId);

        return Result.Success();
    }

    public async Task<Result> DisableFeatureAsync(
        Guid tenantId,
        string featureName,
        CancellationToken cancellationToken = default)
    {
        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureName == featureName, cancellationToken);

        if (feature == null)
            return Result.NotFound("Feature not found for this tenant");

        feature.Disable();
        feature.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Disabled feature {FeatureName} for tenant {TenantId}", featureName, tenantId);

        return Result.Success();
    }

    public async Task<Result> SetFeatureConfigurationAsync(
        Guid tenantId,
        string featureName,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureName == featureName, cancellationToken);

        if (feature == null)
            return Result.NotFound("Feature not found for this tenant");

        feature.SetConfiguration(configuration);
        feature.SetModified(_dateTime.Now, _currentUser.UserId);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<bool>> IsFeatureEnabledAsync(
        Guid tenantId,
        string featureName,
        CancellationToken cancellationToken = default)
    {
        var feature = await _context.Set<TenantFeature>()
            .FirstOrDefaultAsync(f => f.TenantId == tenantId && f.FeatureName == featureName, cancellationToken);

        return feature?.IsActive() ?? false;
    }

    public async Task<Result> SetFeaturesAsync(
        Guid tenantId,
        IEnumerable<SetFeatureRequest> features,
        CancellationToken cancellationToken = default)
    {
        var tenantExists = await _context.Set<Tenant>()
            .AnyAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (!tenantExists)
            return Result.NotFound("Tenant not found");

        var existingFeatures = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var featureRequest in features)
        {
            var existing = existingFeatures.FirstOrDefault(f => f.FeatureName == featureRequest.FeatureName);

            if (existing != null)
            {
                if (featureRequest.IsEnabled)
                    existing.Enable();
                else
                    existing.Disable();

                existing.SetConfiguration(featureRequest.Configuration);
                existing.SetExpiration(featureRequest.ExpiresAt);
                existing.SetModified(_dateTime.Now, _currentUser.UserId);
            }
            else
            {
                var feature = new TenantFeature(
                    Guid.NewGuid(),
                    tenantId,
                    featureRequest.FeatureName,
                    featureRequest.IsEnabled);

                feature.SetConfiguration(featureRequest.Configuration);
                feature.SetExpiration(featureRequest.ExpiresAt);
                feature.SetCreated(_dateTime.Now, _currentUser.UserId ?? "system");
                _context.Set<TenantFeature>().Add(feature);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Set {Count} features for tenant {TenantId}", features.Count(), tenantId);

        return Result.Success();
    }

    #endregion

    #region Statistics

    public async Task<Result<TenantStatisticsResponse>> GetTenantStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant == null)
            return Result<TenantStatisticsResponse>.NotFound("Tenant not found");

        var features = await _context.Set<TenantFeature>()
            .Where(f => f.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return new TenantStatisticsResponse
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TotalFeatures = features.Count,
            EnabledFeatures = features.Count(f => f.IsActive())
        };
    }

    public async Task<Result<DashboardStatisticsResponse>> GetDashboardStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var totalTenants = await _context.Set<Tenant>()
            .CountAsync(t => !t.IsDeleted, cancellationToken);

        var activeTenants = await _context.Set<Tenant>()
            .CountAsync(t => !t.IsDeleted && t.IsActive, cancellationToken);

        var totalUsers = await _context.Set<ApplicationUser>()
            .CountAsync(u => !u.IsDeleted, cancellationToken);

        var activeUsers = await _context.Set<ApplicationUser>()
            .CountAsync(u => !u.IsDeleted && u.IsActive, cancellationToken);

        var totalRoles = await _context.Set<ApplicationRole>()
            .CountAsync(cancellationToken);

        var recentTenants = await _context.Set<Tenant>()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new TenantSummary
            {
                Id = t.Id,
                Name = t.Name,
                Identifier = t.Identifier,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentUsers = await _context.Set<ApplicationUser>()
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new UserSummary
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new DashboardStatisticsResponse
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalRoles = totalRoles,
            RecentTenants = recentTenants,
            RecentUsers = recentUsers
        };
    }

    #endregion

    #region Private Methods

    private static string GetFeatureDisplayName(string featureName)
    {
        // Convert feature name to display name
        return featureName.Replace(".", " - ").Replace("_", " ");
    }

    private static string GetFeatureDescription(string featureName)
    {
        // Return description based on feature name
        return $"Feature: {featureName}";
    }

    #endregion
}
