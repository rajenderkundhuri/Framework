using Framework.Domain.Common.Entities;
using Framework.Domain.MultiTenancy;

namespace Framework.Domain.Localization;

/// <summary>
/// Represents a localized resource stored in the database
/// </summary>
public class LocalizationResource : AuditableEntity, IMultiTenant
{
    public LocalizationResource()
    {
    }

    public LocalizationResource(string key, string cultureName, string value)
    {
        Key = key;
        CultureName = cultureName;
        Value = value;
    }

    /// <summary>
    /// Resource key (e.g., "Common.Save", "Errors.NotFound")
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Culture name (e.g., "en-US", "fr-FR")
    /// </summary>
    public string CultureName { get; private set; } = string.Empty;

    /// <summary>
    /// Localized value
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Resource group for organization (e.g., "Common", "Errors", "Validation")
    /// </summary>
    public string? Group { get; private set; }

    /// <summary>
    /// Description of the resource for translators
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this is a system resource that shouldn't be deleted
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Associated tenant (null for global resources)
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Updates the resource value
    /// </summary>
    public void UpdateValue(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Updates the resource metadata
    /// </summary>
    public void UpdateMetadata(string? group, string? description)
    {
        Group = group;
        Description = description;
    }

    /// <summary>
    /// Sets the tenant
    /// </summary>
    public void SetTenant(Guid? tenantId)
    {
        TenantId = tenantId;
    }

    /// <summary>
    /// Marks as system resource
    /// </summary>
    public void MarkAsSystem()
    {
        IsSystem = true;
    }
}

/// <summary>
/// Supported language configuration
/// </summary>
public class SupportedLanguage : AuditableEntity
{
    public SupportedLanguage()
    {
    }

    public SupportedLanguage(string cultureName, string displayName, string nativeName)
    {
        CultureName = cultureName;
        DisplayName = displayName;
        NativeName = nativeName;
    }

    /// <summary>
    /// Culture name (e.g., "en-US")
    /// </summary>
    public string CultureName { get; private set; } = string.Empty;

    /// <summary>
    /// Display name in English (e.g., "English (United States)")
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Native name (e.g., "English")
    /// </summary>
    public string NativeName { get; private set; } = string.Empty;

    /// <summary>
    /// Flag emoji or icon code
    /// </summary>
    public string? FlagCode { get; private set; }

    /// <summary>
    /// Whether this language is enabled
    /// </summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>
    /// Whether this is the default language
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// Whether RTL (right-to-left) direction
    /// </summary>
    public bool IsRtl { get; private set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Enables the language
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
    }

    /// <summary>
    /// Disables the language
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
    }

    /// <summary>
    /// Sets as default language
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
    }

    /// <summary>
    /// Clears default status
    /// </summary>
    public void ClearDefault()
    {
        IsDefault = false;
    }

    /// <summary>
    /// Updates language info
    /// </summary>
    public void Update(string displayName, string nativeName, string? flagCode, bool isRtl, int sortOrder)
    {
        DisplayName = displayName;
        NativeName = nativeName;
        FlagCode = flagCode;
        IsRtl = isRtl;
        SortOrder = sortOrder;
    }
}
