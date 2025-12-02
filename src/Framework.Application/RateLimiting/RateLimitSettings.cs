namespace Framework.Application.RateLimiting;

/// <summary>
/// Configuration settings for rate limiting
/// </summary>
public class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default rate limit policy
    /// </summary>
    public RateLimitPolicy DefaultPolicy { get; set; } = new();

    /// <summary>
    /// Named rate limit policies
    /// </summary>
    public Dictionary<string, RateLimitPolicy> Policies { get; set; } = new();

    /// <summary>
    /// IP addresses to whitelist (bypass rate limiting)
    /// </summary>
    public List<string> WhitelistedIps { get; set; } = new();

    /// <summary>
    /// Client IDs to whitelist
    /// </summary>
    public List<string> WhitelistedClients { get; set; } = new();

    /// <summary>
    /// Whether to include rate limit headers in responses
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// HTTP status code when rate limited
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// Message when rate limited
    /// </summary>
    public string Message { get; set; } = "Too many requests. Please try again later.";
}

/// <summary>
/// Rate limit policy configuration
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Maximum number of requests allowed
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Queue limit for pending requests (0 = no queue)
    /// </summary>
    public int QueueLimit { get; set; } = 0;

    /// <summary>
    /// Whether to use sliding window
    /// </summary>
    public bool SlidingWindow { get; set; } = true;

    /// <summary>
    /// Segments per window for sliding window
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 4;

    /// <summary>
    /// Rate limit by (IP, User, Client, Endpoint)
    /// </summary>
    public RateLimitBy LimitBy { get; set; } = RateLimitBy.Ip;

    /// <summary>
    /// Auto replenish permits
    /// </summary>
    public bool AutoReplenishment { get; set; } = true;
}

/// <summary>
/// What to rate limit by
/// </summary>
public enum RateLimitBy
{
    /// <summary>
    /// Rate limit by IP address
    /// </summary>
    Ip = 0,

    /// <summary>
    /// Rate limit by authenticated user
    /// </summary>
    User = 1,

    /// <summary>
    /// Rate limit by client ID (API key)
    /// </summary>
    Client = 2,

    /// <summary>
    /// Rate limit by endpoint
    /// </summary>
    Endpoint = 3,

    /// <summary>
    /// Global rate limit
    /// </summary>
    Global = 4
}

/// <summary>
/// Attribute to apply rate limiting to endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Policy name to use
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// Override permit limit
    /// </summary>
    public int PermitLimit { get; set; } = -1;

    /// <summary>
    /// Override window seconds
    /// </summary>
    public int WindowSeconds { get; set; } = -1;

    public RateLimitAttribute() { }

    public RateLimitAttribute(string policy)
    {
        Policy = policy;
    }

    public RateLimitAttribute(int permitLimit, int windowSeconds)
    {
        PermitLimit = permitLimit;
        WindowSeconds = windowSeconds;
    }
}

/// <summary>
/// Attribute to disable rate limiting
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DisableRateLimitAttribute : Attribute { }
