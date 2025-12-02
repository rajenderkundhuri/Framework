using Framework.Application.FeatureFlags;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Configuration-based feature manager
/// </summary>
public class ConfigurationFeatureManager : IFeatureManager
{
    private readonly FeatureFlagSettings _settings;
    private readonly IEnumerable<IFeatureFilterEvaluator> _filterEvaluators;
    private readonly ILogger<ConfigurationFeatureManager> _logger;

    public ConfigurationFeatureManager(
        IOptions<FeatureFlagSettings> settings,
        IEnumerable<IFeatureFilterEvaluator> filterEvaluators,
        ILogger<ConfigurationFeatureManager> logger)
    {
        _settings = settings.Value;
        _filterEvaluators = filterEvaluators;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        return await IsEnabledAsync<object>(featureName, null!, cancellationToken);
    }

    public async Task<bool> IsEnabledAsync<TContext>(string featureName, TContext context, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        if (!_settings.Features.TryGetValue(featureName, out var feature))
        {
            _logger.LogDebug("Feature {FeatureName} not found, returning false", featureName);
            return false;
        }

        if (!feature.Enabled)
        {
            return false;
        }

        // If no filters, return enabled status
        if (feature.Filters.Count == 0)
        {
            return true;
        }

        // Evaluate all filters (ALL must pass)
        foreach (var filter in feature.Filters)
        {
            var evaluator = _filterEvaluators.FirstOrDefault(e => e.FilterName == filter.Name);
            if (evaluator == null)
            {
                _logger.LogWarning("Filter evaluator for {FilterName} not found", filter.Name);
                continue;
            }

            var result = await evaluator.EvaluateAsync(filter, context, cancellationToken);
            if (!result)
            {
                return false;
            }
        }

        return true;
    }

    public async IAsyncEnumerable<string> GetFeatureNamesAsync()
    {
        foreach (var feature in _settings.Features.Keys)
        {
            yield return feature;
        }
    }

    public Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName, CancellationToken cancellationToken = default)
    {
        _settings.Features.TryGetValue(featureName, out var feature);
        return Task.FromResult(feature);
    }
}

/// <summary>
/// Percentage-based feature filter evaluator
/// </summary>
public class PercentageFilterEvaluator : IFeatureFilterEvaluator
{
    public string FilterName => "Percentage";

    public Task<bool> EvaluateAsync(FeatureFilter filter, object? context, CancellationToken cancellationToken = default)
    {
        if (!filter.Parameters.TryGetValue("Value", out var valueObj))
        {
            return Task.FromResult(false);
        }

        var percentage = Convert.ToDouble(valueObj);
        var random = Random.Shared.NextDouble() * 100;

        return Task.FromResult(random < percentage);
    }
}

/// <summary>
/// Time window feature filter evaluator
/// </summary>
public class TimeWindowFilterEvaluator : IFeatureFilterEvaluator
{
    public string FilterName => "TimeWindow";

    public Task<bool> EvaluateAsync(FeatureFilter filter, object? context, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (filter.Parameters.TryGetValue("Start", out var startObj))
        {
            var start = DateTime.Parse(startObj.ToString()!);
            if (now < start)
            {
                return Task.FromResult(false);
            }
        }

        if (filter.Parameters.TryGetValue("End", out var endObj))
        {
            var end = DateTime.Parse(endObj.ToString()!);
            if (now > end)
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// User targeting feature filter evaluator
/// </summary>
public class TargetingFilterEvaluator : IFeatureFilterEvaluator
{
    public string FilterName => "Targeting";

    public Task<bool> EvaluateAsync(FeatureFilter filter, object? context, CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            return Task.FromResult(false);
        }

        // Check if context has UserId property
        var userIdProp = context.GetType().GetProperty("UserId");
        if (userIdProp == null)
        {
            return Task.FromResult(false);
        }

        var userId = userIdProp.GetValue(context)?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(false);
        }

        // Check users list
        if (filter.Parameters.TryGetValue("Users", out var usersObj) && usersObj is IEnumerable<object> users)
        {
            if (users.Select(u => u.ToString()).Contains(userId))
            {
                return Task.FromResult(true);
            }
        }

        // Check groups
        var groupProp = context.GetType().GetProperty("Groups");
        if (groupProp != null && filter.Parameters.TryGetValue("Groups", out var groupsObj) && groupsObj is IEnumerable<object> targetGroups)
        {
            var userGroups = groupProp.GetValue(context) as IEnumerable<string>;
            if (userGroups != null && targetGroups.Select(g => g.ToString()).Intersect(userGroups).Any())
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }
}
