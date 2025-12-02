using Framework.CodeGen.Analysis.Models;
using Framework.CodeGen.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Framework.CodeGen.Analysis;

/// <summary>
/// Analyzes entities by parsing C# source files using Roslyn
/// </summary>
public class RoslynEntityAnalyzer : IEntityAnalyzer
{
    // Properties to exclude from code generation
    private static readonly HashSet<string> AuditProperties = new()
    {
        "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
    };

    private static readonly HashSet<string> SoftDeleteProperties = new()
    {
        "IsDeleted", "DeletedAt", "DeletedBy"
    };

    public EntityMetadata Analyze(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        var code = File.ReadAllText(filePath);
        return AnalyzeSource(code);
    }

    public EntityMetadata AnalyzeSource(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetCompilationUnitRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => !c.Modifiers.Any(SyntaxKind.AbstractKeyword))
            ?? throw new InvalidOperationException("No non-abstract class found in source file");

        var metadata = new EntityMetadata
        {
            Name = classDeclaration.Identifier.Text,
            PluralName = classDeclaration.Identifier.Text.Pluralize()
        };

        // Get namespace
        var namespaceDeclaration = classDeclaration.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        metadata.Namespace = namespaceDeclaration?.Name.ToString() ?? string.Empty;

        // Analyze base class
        AnalyzeBaseClass(metadata, classDeclaration);

        // Analyze properties
        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            var propMeta = AnalyzeProperty(property);
            if (propMeta != null)
            {
                metadata.Properties.Add(propMeta);
            }
        }

        // Detect foreign keys
        DetectForeignKeys(metadata);

        return metadata;
    }

    private void AnalyzeBaseClass(EntityMetadata metadata, ClassDeclarationSyntax classDecl)
    {
        var baseList = classDecl.BaseList;
        if (baseList == null) return;

        foreach (var baseType in baseList.Types)
        {
            var typeName = baseType.Type.ToString();

            // Detect entity characteristics from base class
            if (typeName.Contains("AggregateRoot"))
            {
                metadata.IsAggregateRoot = true;
                metadata.IsAuditable = true;
            }
            else if (typeName.Contains("FullAuditableEntity") || typeName.Contains("FullMultiTenantEntity"))
            {
                metadata.IsAuditable = true;
                metadata.IsSoftDelete = true;
            }
            else if (typeName.Contains("AuditableEntity") || typeName.Contains("MultiTenantEntity"))
            {
                metadata.IsAuditable = true;
            }

            if (typeName.Contains("MultiTenant"))
            {
                metadata.IsMultiTenant = true;
            }

            if (typeName.Contains("ISoftDelete"))
            {
                metadata.IsSoftDelete = true;
            }

            // Extract generic type argument for key type
            if (baseType.Type is GenericNameSyntax genericName)
            {
                var keyTypeName = genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
                metadata.KeyType = keyTypeName switch
                {
                    "Guid" => typeof(Guid),
                    "int" => typeof(int),
                    "long" => typeof(long),
                    "string" => typeof(string),
                    _ => typeof(Guid)
                };

                metadata.BaseClass = genericName.Identifier.Text;
            }
            else if (baseType.Type is IdentifierNameSyntax identifierName)
            {
                metadata.BaseClass = identifierName.Identifier.Text;
            }
        }

        metadata.KeyType ??= typeof(Guid);
    }

    private PropertyMetadata? AnalyzeProperty(PropertyDeclarationSyntax property)
    {
        var propertyName = property.Identifier.Text;
        var typeSyntax = property.Type;
        var typeName = typeSyntax.ToString();

        // Determine if nullable
        var isNullable = typeSyntax is NullableTypeSyntax ||
                        typeName.EndsWith("?");

        // Check if virtual (navigation property)
        var isVirtual = property.Modifiers.Any(SyntaxKind.VirtualKeyword);

        // Determine if collection
        var isCollection = typeName.StartsWith("ICollection<") ||
                          typeName.StartsWith("IEnumerable<") ||
                          typeName.StartsWith("List<") ||
                          typeName.StartsWith("IList<");

        // Check if navigation property
        var isNavigation = isVirtual && !IsPrimitiveType(typeName) && !isCollection;

        var propMeta = new PropertyMetadata
        {
            Name = propertyName,
            TypeName = typeName.TrimEnd('?'),
            IsNullable = isNullable,
            IsRequired = !isNullable && propertyName != "Id",
            IsCollection = isCollection,
            IsNavigationProperty = isNavigation || isCollection,
            IsAuditProperty = AuditProperties.Contains(propertyName),
            IsSoftDeleteProperty = SoftDeleteProperties.Contains(propertyName),
            IsPrimaryKey = propertyName == "Id",
            IsTenantProperty = propertyName == "TenantId",
            IsEnumType = false // Hard to determine without compilation
        };

        // Check for attributes
        foreach (var attributeList in property.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName is "MaxLength" or "MaxLengthAttribute")
                {
                    var arg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (arg != null && int.TryParse(arg.ToString(), out var maxLength))
                    {
                        propMeta.MaxLength = maxLength;
                    }
                }
                else if (attrName is "StringLength" or "StringLengthAttribute")
                {
                    var arg = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (arg != null && int.TryParse(arg.ToString(), out var maxLength))
                    {
                        propMeta.MaxLength = maxLength;
                    }
                }
                else if (attrName is "Required" or "RequiredAttribute")
                {
                    propMeta.IsRequired = true;
                }
            }
        }

        return propMeta;
    }

    private void DetectForeignKeys(EntityMetadata metadata)
    {
        foreach (var prop in metadata.Properties.Where(p => p.Name.EndsWith("Id") && p.Name != "Id" && p.Name != "TenantId"))
        {
            var navigationName = prop.Name[..^2]; // Remove "Id" suffix
            var navProp = metadata.Properties.FirstOrDefault(p =>
                p.Name == navigationName && p.IsNavigationProperty);

            var relatedEntityName = navigationName;

            prop.IsForeignKey = true;
            prop.ForeignKeyInfo = new ForeignKeyInfo
            {
                PropertyName = prop.Name,
                NavigationPropertyName = navProp?.Name ?? navigationName,
                RelatedEntityName = relatedEntityName,
                RelatedEntityPluralName = relatedEntityName.Pluralize(),
                DisplayProperty = "Name",
                IsRequired = !prop.IsNullable
            };

            metadata.ForeignKeys.Add(prop.ForeignKeyInfo);
        }
    }

    private static bool IsPrimitiveType(string typeName)
    {
        var primitives = new HashSet<string>
        {
            "string", "String",
            "int", "Int32",
            "long", "Int64",
            "short", "Int16",
            "bool", "Boolean",
            "decimal", "Decimal",
            "double", "Double",
            "float", "Single",
            "byte", "Byte",
            "char", "Char",
            "Guid",
            "DateTime",
            "DateTimeOffset",
            "TimeSpan"
        };

        var cleanType = typeName.TrimEnd('?');
        return primitives.Contains(cleanType);
    }
}
