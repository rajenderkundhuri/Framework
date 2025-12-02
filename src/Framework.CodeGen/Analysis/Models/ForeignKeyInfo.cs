namespace Framework.CodeGen.Analysis.Models;

/// <summary>
/// Information about a foreign key relationship
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>
    /// The FK property name (e.g., "CategoryId")
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The navigation property name (e.g., "Category")
    /// </summary>
    public string NavigationPropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The related entity name (e.g., "Category")
    /// </summary>
    public string RelatedEntityName { get; set; } = string.Empty;

    /// <summary>
    /// The related entity plural name (e.g., "Categories")
    /// </summary>
    public string RelatedEntityPluralName { get; set; } = string.Empty;

    /// <summary>
    /// The property to display in dropdowns (e.g., "Name")
    /// </summary>
    public string DisplayProperty { get; set; } = "Name";

    /// <summary>
    /// Whether the relationship is required
    /// </summary>
    public bool IsRequired { get; set; }

    // Computed properties for templates
    public string VariableName => char.ToLowerInvariant(RelatedEntityName[0]) + RelatedEntityName[1..];
    public string ListVariableName => $"_{VariableName}List";
    public string FilterVariableName => $"_{char.ToLowerInvariant(PropertyName[0]) + PropertyName[1..]}Filter";
}
