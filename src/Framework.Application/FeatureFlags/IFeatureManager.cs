namespace Framework.Application.FeatureFlags;

/// <summary>
/// Interface for feature flag management
/// </summary>
public interface IFeatureManager
{
    /// <summary>
    /// Checks if a feature is enabled
    /// </summary>
    Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature is enabled for a specific context
    /// </summary>
    Task<bool> IsEnabledAsync<TContext>(string featureName, TContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feature names
    /// </summary>
    IAsyncEnumerable<string> GetFeatureNamesAsync();

    /// <summary>
    /// Gets feature definition
    /// </summary>
    Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for feature flag providers
/// </summary>
public interface IFeatureFlagProvider
{
    /// <summary>
    /// Gets all feature definitions
    /// </summary>
    Task<IEnumerable<FeatureDefinition>> GetAllFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific feature definition
    /// </summary>
    Task<FeatureDefinition?> GetFeatureAsync(string featureName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Feature definition
/// </summary>
public class FeatureDefinition
{
    /// <summary>
    /// Feature name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Feature description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Feature filters to evaluate
    /// </summary>
    public List<FeatureFilter> Filters { get; set; } = new();

    /// <summary>
    /// Additional parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Feature filter for conditional enabling
/// </summary>
public class FeatureFilter
{
    /// <summary>
    /// Filter name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Filter parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Interface for evaluating feature filters
/// </summary>
public interface IFeatureFilterEvaluator
{
    /// <summary>
    /// Filter name this evaluator handles
    /// </summary>
    string FilterName { get; }

    /// <summary>
    /// Evaluates the filter
    /// </summary>
    Task<bool> EvaluateAsync(FeatureFilter filter, object? context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Settings for feature flags
/// </summary>
public class FeatureFlagSettings
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Whether feature flags are enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache duration in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Provider type
    /// </summary>
    public FeatureFlagProvider Provider { get; set; } = FeatureFlagProvider.Configuration;

    /// <summary>
    /// Feature definitions (for configuration provider)
    /// </summary>
    public Dictionary<string, FeatureDefinition> Features { get; set; } = new();
}

/// <summary>
/// Feature flag provider types
/// </summary>
public enum FeatureFlagProvider
{
    /// <summary>
    /// Read from configuration
    /// </summary>
    Configuration = 0,

    /// <summary>
    /// Read from database
    /// </summary>
    Database = 1,

    /// <summary>
    /// Read from external service
    /// </summary>
    External = 2
}

/// <summary>
/// Attribute to gate features
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class FeatureGateAttribute : Attribute
{
    /// <summary>
    /// Feature names required
    /// </summary>
    public string[] Features { get; }

    /// <summary>
    /// Requirement type (All or Any)
    /// </summary>
    public FeatureGateRequirement Requirement { get; set; } = FeatureGateRequirement.All;

    public FeatureGateAttribute(params string[] features)
    {
        Features = features;
    }
}

/// <summary>
/// How multiple features are evaluated
/// </summary>
public enum FeatureGateRequirement
{
    /// <summary>
    /// All features must be enabled
    /// </summary>
    All = 0,

    /// <summary>
    /// Any feature must be enabled
    /// </summary>
    Any = 1
}
