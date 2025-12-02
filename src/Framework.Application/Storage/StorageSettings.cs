namespace Framework.Application.Storage;

/// <summary>
/// Configuration settings for storage services
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider type
    /// </summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Local;

    /// <summary>
    /// Base path for local storage
    /// </summary>
    public string LocalBasePath { get; set; } = "storage";

    /// <summary>
    /// Whether to create directories automatically
    /// </summary>
    public bool AutoCreateDirectories { get; set; } = true;

    /// <summary>
    /// Maximum file size in bytes (0 = unlimited)
    /// </summary>
    public long MaxFileSize { get; set; } = 0;

    /// <summary>
    /// Allowed file extensions (empty = all allowed)
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();

    /// <summary>
    /// Blocked file extensions
    /// </summary>
    public List<string> BlockedExtensions { get; set; } = new()
    {
        ".exe", ".dll", ".bat", ".cmd", ".sh", ".ps1", ".msi"
    };

    /// <summary>
    /// Azure Blob Storage settings
    /// </summary>
    public AzureBlobSettings? AzureBlob { get; set; }

    /// <summary>
    /// AWS S3 settings
    /// </summary>
    public AwsS3Settings? AwsS3 { get; set; }

    /// <summary>
    /// Whether to use tenant isolation
    /// </summary>
    public bool TenantIsolation { get; set; } = true;

    /// <summary>
    /// Default cache control header
    /// </summary>
    public string? DefaultCacheControl { get; set; }

    /// <summary>
    /// Whether to preserve original filenames or generate new ones
    /// </summary>
    public bool PreserveOriginalFilenames { get; set; } = true;
}

/// <summary>
/// Storage provider types
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// Local file system storage
    /// </summary>
    Local = 0,

    /// <summary>
    /// Azure Blob Storage
    /// </summary>
    AzureBlob = 1,

    /// <summary>
    /// AWS S3
    /// </summary>
    AwsS3 = 2,

    /// <summary>
    /// In-memory storage (for testing)
    /// </summary>
    InMemory = 99
}

/// <summary>
/// Azure Blob Storage settings
/// </summary>
public class AzureBlobSettings
{
    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Container name
    /// </summary>
    public string ContainerName { get; set; } = "files";

    /// <summary>
    /// Whether to create container if it doesn't exist
    /// </summary>
    public bool AutoCreateContainer { get; set; } = true;
}

/// <summary>
/// AWS S3 settings
/// </summary>
public class AwsS3Settings
{
    /// <summary>
    /// AWS Region
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Access key (if not using IAM roles)
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret key (if not using IAM roles)
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Whether to create bucket if it doesn't exist
    /// </summary>
    public bool AutoCreateBucket { get; set; } = false;

    /// <summary>
    /// Custom endpoint (for S3-compatible services)
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Whether to use path-style addressing
    /// </summary>
    public bool ForcePathStyle { get; set; } = false;
}
