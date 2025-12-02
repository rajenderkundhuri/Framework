namespace Framework.CodeGen.Analysis.Models;

/// <summary>
/// Metadata about an entity for code generation
/// </summary>
public class EntityMetadata
{
    public string Name { get; set; } = string.Empty;
    public string PluralName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string? BaseClass { get; set; }
    public Type? KeyType { get; set; } = typeof(Guid);

    // Entity characteristics
    public bool IsAuditable { get; set; }
    public bool IsSoftDelete { get; set; }
    public bool IsMultiTenant { get; set; }
    public bool IsAggregateRoot { get; set; }

    // Properties and relationships
    public List<PropertyMetadata> Properties { get; set; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();

    // Computed properties for template rendering
    public string KeyTypeName => KeyType?.Name ?? "Guid";
    public string RoutePath => PluralName.ToLowerInvariant();

    public IEnumerable<PropertyMetadata> CreateProperties =>
        Properties.Where(p => !p.IsExcludedFromCreate && !p.IsNavigationProperty);

    public IEnumerable<PropertyMetadata> UpdateProperties =>
        Properties.Where(p => !p.IsExcludedFromUpdate && !p.IsNavigationProperty);

    public IEnumerable<PropertyMetadata> ListProperties =>
        Properties.Where(p => p.IncludeInList && !p.IsNavigationProperty);

    public IEnumerable<PropertyMetadata> FilterProperties =>
        Properties.Where(p => p.IsFilterable);
}
