using Framework.CodeGen.Analysis.Models;

namespace Framework.CodeGen.Generation;

/// <summary>
/// Context for code generation containing all the data needed for templates
/// </summary>
public class GenerationContext
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityPlural { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string DomainNamespace { get; set; } = string.Empty;
    public string ApplicationNamespace { get; set; } = string.Empty;
    public string RoutePath { get; set; } = string.Empty;

    // Entity characteristics
    public bool IsAuditable { get; set; }
    public bool IsSoftDelete { get; set; }
    public bool IsMultiTenant { get; set; }

    // Property collections for different generation scenarios
    public List<PropertyMetadata> CreateProperties { get; set; } = new();
    public List<PropertyMetadata> UpdateProperties { get; set; } = new();
    public List<PropertyMetadata> ListProperties { get; set; } = new();
    public List<PropertyMetadata> FilterProperties { get; set; } = new();
    public List<PropertyMetadata> FormProperties { get; set; } = new();
    public List<PropertyMetadata> SearchProperties { get; set; } = new();
    public List<PropertyMetadata> ListColumns { get; set; } = new();

    // Foreign key relationships
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();

    /// <summary>
    /// Creates a generation context from entity metadata
    /// </summary>
    public static GenerationContext FromMetadata(EntityMetadata metadata, GenerationOptions options)
    {
        var context = new GenerationContext
        {
            EntityName = metadata.Name,
            EntityPlural = metadata.PluralName,
            Namespace = options.Namespace ?? $"Framework.Application.{metadata.PluralName}",
            DomainNamespace = options.DomainNamespace ?? "Framework.Domain",
            ApplicationNamespace = options.ApplicationNamespace ?? "Framework.Application",
            RoutePath = metadata.RoutePath,
            IsAuditable = metadata.IsAuditable,
            IsSoftDelete = metadata.IsSoftDelete,
            IsMultiTenant = metadata.IsMultiTenant,
            ForeignKeys = metadata.ForeignKeys
        };

        // Filter properties for different scenarios
        context.CreateProperties = metadata.Properties
            .Where(p => !p.IsExcludedFromCreate)
            .ToList();

        context.UpdateProperties = metadata.Properties
            .Where(p => !p.IsExcludedFromUpdate)
            .ToList();

        context.ListProperties = metadata.Properties
            .Where(p => p.IncludeInList)
            .ToList();

        context.FilterProperties = metadata.Properties
            .Where(p => p.IsFilterable)
            .ToList();

        context.FormProperties = metadata.Properties
            .Where(p => !p.IsExcludedFromCreate && !p.IsPrimaryKey)
            .ToList();

        context.SearchProperties = metadata.Properties
            .Where(p => p.TypeName == "string" && !p.IsNavigationProperty && p.Name != "Id")
            .Take(5) // Limit to 5 searchable properties
            .ToList();

        context.ListColumns = metadata.Properties
            .Where(p => p.IncludeInList)
            .Take(6) // Limit columns for readability
            .ToList();

        return context;
    }
}

/// <summary>
/// Options for code generation
/// </summary>
public class GenerationOptions
{
    public string EntityNameOrPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public string? Namespace { get; set; }
    public string? DomainNamespace { get; set; }
    public string? ApplicationNamespace { get; set; }
    public string? CustomTemplatesPath { get; set; }

    public bool UseReflection { get; set; } = true;
    public string? AssemblyPath { get; set; }

    public bool GenerateApi { get; set; } = true;
    public bool GenerateAdmin { get; set; } = true;

    public bool Overwrite { get; set; }
    public bool DryRun { get; set; }
}
