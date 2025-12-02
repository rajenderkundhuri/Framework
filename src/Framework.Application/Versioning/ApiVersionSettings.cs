namespace Framework.Application.Versioning;

/// <summary>
/// Configuration settings for API versioning
/// </summary>
public class ApiVersionSettings
{
    public const string SectionName = "ApiVersioning";

    /// <summary>
    /// Whether to use API versioning
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default API version
    /// </summary>
    public string DefaultVersion { get; set; } = "1.0";

    /// <summary>
    /// Whether to assume default version when not specified
    /// </summary>
    public bool AssumeDefaultVersionWhenUnspecified { get; set; } = true;

    /// <summary>
    /// Whether to report API versions in response headers
    /// </summary>
    public bool ReportApiVersions { get; set; } = true;

    /// <summary>
    /// How to read the API version
    /// </summary>
    public ApiVersionReader Reader { get; set; } = ApiVersionReader.All;

    /// <summary>
    /// Header name for version (when using header reader)
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Version";

    /// <summary>
    /// Query parameter name for version
    /// </summary>
    public string QueryParameterName { get; set; } = "api-version";

    /// <summary>
    /// URL segment for version (e.g., v{version})
    /// </summary>
    public string UrlSegmentTemplate { get; set; } = "v{version:apiVersion}";

    /// <summary>
    /// Media type parameter name
    /// </summary>
    public string MediaTypeParameterName { get; set; } = "v";

    /// <summary>
    /// Supported versions
    /// </summary>
    public List<string> SupportedVersions { get; set; } = new() { "1.0" };

    /// <summary>
    /// Deprecated versions
    /// </summary>
    public List<string> DeprecatedVersions { get; set; } = new();

    /// <summary>
    /// Sunset header format
    /// </summary>
    public string? SunsetHeaderFormat { get; set; }
}

/// <summary>
/// How to read API version from request
/// </summary>
[Flags]
public enum ApiVersionReader
{
    /// <summary>
    /// Read from query string
    /// </summary>
    Query = 1,

    /// <summary>
    /// Read from header
    /// </summary>
    Header = 2,

    /// <summary>
    /// Read from URL segment
    /// </summary>
    UrlSegment = 4,

    /// <summary>
    /// Read from media type
    /// </summary>
    MediaType = 8,

    /// <summary>
    /// All readers combined
    /// </summary>
    All = Query | Header | UrlSegment
}

/// <summary>
/// Represents an API version
/// </summary>
public class ApiVersion : IComparable<ApiVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public string? Status { get; }

    public ApiVersion(int major, int minor = 0, string? status = null)
    {
        Major = major;
        Minor = minor;
        Status = status;
    }

    public static ApiVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be empty", nameof(version));

        var parts = version.Split('-');
        var versionPart = parts[0];
        var status = parts.Length > 1 ? parts[1] : null;

        var versionNumbers = versionPart.Split('.');
        var major = int.Parse(versionNumbers[0]);
        var minor = versionNumbers.Length > 1 ? int.Parse(versionNumbers[1]) : 0;

        return new ApiVersion(major, minor, status);
    }

    public static bool TryParse(string version, out ApiVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public int CompareTo(ApiVersion? other)
    {
        if (other == null) return 1;

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        return Minor.CompareTo(other.Minor);
    }

    public override string ToString()
    {
        var version = Minor == 0 ? $"{Major}" : $"{Major}.{Minor}";
        return string.IsNullOrEmpty(Status) ? version : $"{version}-{Status}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is ApiVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Status == other.Status;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Status);

    public static bool operator ==(ApiVersion? left, ApiVersion? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ApiVersion? left, ApiVersion? right) => !(left == right);
    public static bool operator <(ApiVersion left, ApiVersion right) => left.CompareTo(right) < 0;
    public static bool operator >(ApiVersion left, ApiVersion right) => left.CompareTo(right) > 0;
    public static bool operator <=(ApiVersion left, ApiVersion right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ApiVersion left, ApiVersion right) => left.CompareTo(right) >= 0;
}

/// <summary>
/// Attribute to specify API version for a controller/action
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ApiVersionAttribute : Attribute
{
    public string Version { get; }
    public bool Deprecated { get; set; }

    public ApiVersionAttribute(string version)
    {
        Version = version;
    }

    public ApiVersionAttribute(int majorVersion, int minorVersion = 0)
    {
        Version = minorVersion == 0 ? $"{majorVersion}" : $"{majorVersion}.{minorVersion}";
    }
}

/// <summary>
/// Attribute to map actions to specific API versions
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class MapToApiVersionAttribute : Attribute
{
    public string Version { get; }

    public MapToApiVersionAttribute(string version)
    {
        Version = version;
    }
}
