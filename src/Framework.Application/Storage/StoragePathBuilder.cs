using System.Security.Cryptography;
using System.Text;

namespace Framework.Application.Storage;

/// <summary>
/// Helper class for building storage paths
/// </summary>
public class StoragePathBuilder
{
    private readonly List<string> _segments = new();
    private string? _fileName;
    private string? _extension;
    private bool _generateUniqueFileName;
    private bool _datePartition;
    private DateTime? _partitionDate;

    /// <summary>
    /// Creates a new path builder
    /// </summary>
    public static StoragePathBuilder Create() => new();

    /// <summary>
    /// Adds a path segment
    /// </summary>
    public StoragePathBuilder WithSegment(string segment)
    {
        if (!string.IsNullOrWhiteSpace(segment))
        {
            _segments.Add(SanitizeSegment(segment));
        }
        return this;
    }

    /// <summary>
    /// Adds tenant isolation segment
    /// </summary>
    public StoragePathBuilder WithTenant(Guid tenantId)
    {
        _segments.Insert(0, $"tenants/{tenantId}");
        return this;
    }

    /// <summary>
    /// Adds user isolation segment
    /// </summary>
    public StoragePathBuilder WithUser(Guid userId)
    {
        _segments.Add($"users/{userId}");
        return this;
    }

    /// <summary>
    /// Adds date-based partitioning
    /// </summary>
    public StoragePathBuilder WithDatePartition(DateTime? date = null)
    {
        _datePartition = true;
        _partitionDate = date;
        return this;
    }

    /// <summary>
    /// Sets the file name
    /// </summary>
    public StoragePathBuilder WithFileName(string fileName)
    {
        var sanitized = SanitizeFileName(fileName);
        _fileName = Path.GetFileNameWithoutExtension(sanitized);
        _extension = Path.GetExtension(sanitized);
        return this;
    }

    /// <summary>
    /// Generates a unique file name (preserves extension)
    /// </summary>
    public StoragePathBuilder WithUniqueFileName(string? originalFileName = null)
    {
        _generateUniqueFileName = true;
        if (!string.IsNullOrEmpty(originalFileName))
        {
            _extension = Path.GetExtension(SanitizeFileName(originalFileName));
        }
        return this;
    }

    /// <summary>
    /// Sets the file extension
    /// </summary>
    public StoragePathBuilder WithExtension(string extension)
    {
        _extension = extension.StartsWith('.') ? extension : $".{extension}";
        return this;
    }

    /// <summary>
    /// Builds the final path
    /// </summary>
    public string Build()
    {
        var parts = new List<string>(_segments);

        if (_datePartition)
        {
            var date = _partitionDate ?? DateTime.UtcNow;
            parts.Add($"{date.Year:D4}/{date.Month:D2}/{date.Day:D2}");
        }

        var fileName = _generateUniqueFileName
            ? GenerateUniqueFileName()
            : _fileName ?? throw new InvalidOperationException("File name is required");

        if (!string.IsNullOrEmpty(_extension))
        {
            fileName += _extension;
        }

        parts.Add(fileName);

        return string.Join("/", parts);
    }

    private static string SanitizeSegment(string segment)
    {
        // Remove invalid path characters
        var invalid = Path.GetInvalidPathChars()
            .Concat(new[] { '\\', ':' });

        var sanitized = new StringBuilder(segment);
        foreach (var c in invalid)
        {
            sanitized.Replace(c, '-');
        }

        return sanitized.ToString().Trim('/');
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(fileName);
        foreach (var c in invalid)
        {
            sanitized.Replace(c, '-');
        }
        return sanitized.ToString();
    }

    private string GenerateUniqueFileName()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var randomBytes = new byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        var random = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return $"{timestamp:x}_{random}";
    }
}

/// <summary>
/// Common storage path constants
/// </summary>
public static class StoragePaths
{
    public const string Uploads = "uploads";
    public const string Images = "images";
    public const string Documents = "documents";
    public const string Temp = "temp";
    public const string Avatars = "avatars";
    public const string Attachments = "attachments";
    public const string Exports = "exports";
    public const string Imports = "imports";
    public const string Backups = "backups";
    public const string Logs = "logs";

    /// <summary>
    /// Creates a path for user uploads
    /// </summary>
    public static string UserUploads(Guid userId) =>
        $"{Uploads}/users/{userId}";

    /// <summary>
    /// Creates a path for user avatars
    /// </summary>
    public static string UserAvatar(Guid userId) =>
        $"{Avatars}/{userId}";

    /// <summary>
    /// Creates a temp file path
    /// </summary>
    public static string TempFile(string? extension = null)
    {
        var fileName = Guid.NewGuid().ToString("N");
        if (!string.IsNullOrEmpty(extension))
        {
            fileName += extension.StartsWith('.') ? extension : $".{extension}";
        }
        return $"{Temp}/{fileName}";
    }

    /// <summary>
    /// Creates a dated path for exports
    /// </summary>
    public static string Export(string name, DateTime? date = null)
    {
        var d = date ?? DateTime.UtcNow;
        return $"{Exports}/{d.Year:D4}/{d.Month:D2}/{name}";
    }
}

/// <summary>
/// Extension methods for content type detection
/// </summary>
public static class ContentTypeHelper
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".svg", "image/svg+xml" },
        { ".ico", "image/x-icon" },

        // Documents
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },

        // Text
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".js", "text/javascript" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".md", "text/markdown" },

        // Archives
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z", "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" },

        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" },

        // Video
        { ".mp4", "video/mp4" },
        { ".webm", "video/webm" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },

        // Other
        { ".woff", "font/woff" },
        { ".woff2", "font/woff2" },
        { ".ttf", "font/ttf" },
        { ".eot", "application/vnd.ms-fontobject" }
    };

    /// <summary>
    /// Gets content type from file extension
    /// </summary>
    public static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return MimeTypes.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }

    /// <summary>
    /// Gets file extension from content type
    /// </summary>
    public static string? GetExtension(string contentType)
    {
        return MimeTypes.FirstOrDefault(x =>
            x.Value.Equals(contentType, StringComparison.OrdinalIgnoreCase)).Key;
    }

    /// <summary>
    /// Checks if content type is an image
    /// </summary>
    public static bool IsImage(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if content type is a video
    /// </summary>
    public static bool IsVideo(string contentType) =>
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if content type is audio
    /// </summary>
    public static bool IsAudio(string contentType) =>
        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if content type is a document
    /// </summary>
    public static bool IsDocument(string contentType) =>
        contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("word", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
        contentType.Contains("powerpoint", StringComparison.OrdinalIgnoreCase);
}
