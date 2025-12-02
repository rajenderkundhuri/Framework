namespace Framework.CodeGen.Analysis.Models;

/// <summary>
/// Metadata about an entity property for code generation
/// </summary>
public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public Type? ClrType { get; set; }

    // Property characteristics
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCollection { get; set; }
    public int? MaxLength { get; set; }
    public string? DefaultValue { get; set; }

    // Relationship info
    public bool IsForeignKey { get; set; }
    public bool IsNavigationProperty { get; set; }
    public ForeignKeyInfo? ForeignKeyInfo { get; set; }

    // Generation flags
    public bool IsAuditProperty { get; set; }
    public bool IsSoftDeleteProperty { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsTenantProperty { get; set; }

    // Computed properties
    public bool IsExcludedFromCreate =>
        IsPrimaryKey || IsAuditProperty || IsSoftDeleteProperty ||
        IsTenantProperty || IsNavigationProperty;

    public bool IsExcludedFromUpdate =>
        IsPrimaryKey || IsAuditProperty || IsSoftDeleteProperty ||
        IsTenantProperty || IsNavigationProperty;

    public bool IncludeInList =>
        !IsNavigationProperty && !IsSoftDeleteProperty &&
        Name != "Id" && !Name.Contains("Password");

    public bool IsFilterable =>
        IsForeignKey || TypeName == "bool" || TypeName == "bool?" ||
        IsEnumType || Name == "IsActive" || Name.EndsWith("Status");

    public bool IsEnumType { get; set; }

    // Form control mapping
    public string FormControlType => GetFormControlType();
    public string InputType => GetInputType();

    private string GetFormControlType()
    {
        if (IsForeignKey) return "MudSelect";
        if (IsEnumType) return "MudSelect";

        return TypeName switch
        {
            "bool" or "bool?" => "MudCheckBox",
            "int" or "int?" or "long" or "long?" or
            "decimal" or "decimal?" or "double" or "double?" or "float" or "float?" => "MudNumericField",
            "DateTime" or "DateTime?" or "DateTimeOffset" or "DateTimeOffset?" => "MudDatePicker",
            "TimeSpan" or "TimeSpan?" => "MudTimePicker",
            _ => "MudTextField"
        };
    }

    private string GetInputType()
    {
        if (Name.Contains("Email", StringComparison.OrdinalIgnoreCase)) return "InputType.Email";
        if (Name.Contains("Password", StringComparison.OrdinalIgnoreCase)) return "InputType.Password";
        if (Name.Contains("Phone", StringComparison.OrdinalIgnoreCase)) return "InputType.Telephone";
        if (Name.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
            Name.Contains("Uri", StringComparison.OrdinalIgnoreCase)) return "InputType.Url";
        return "InputType.Text";
    }

    // For nullable reference types in templates
    public string TypeNameForCreate => IsNullable ? TypeName : TypeName;
    public string TypeNameForUpdate => IsNullable || !IsRequired ? $"{TypeName.TrimEnd('?')}?" : TypeName;
}
