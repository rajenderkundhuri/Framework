using System.Reflection;
using Framework.CodeGen.Analysis.Models;
using Framework.CodeGen.Utilities;

namespace Framework.CodeGen.Analysis;

/// <summary>
/// Analyzes entities using reflection on compiled assemblies
/// </summary>
public class ReflectionEntityAnalyzer : IEntityAnalyzer
{
    private readonly Assembly _assembly;

    // Properties to exclude from code generation
    private static readonly HashSet<string> AuditProperties = new()
    {
        "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy"
    };

    private static readonly HashSet<string> SoftDeleteProperties = new()
    {
        "IsDeleted", "DeletedAt", "DeletedBy"
    };

    public ReflectionEntityAnalyzer(Assembly assembly)
    {
        _assembly = assembly;
    }

    public ReflectionEntityAnalyzer(string assemblyPath)
    {
        _assembly = Assembly.LoadFrom(assemblyPath);
    }

    public EntityMetadata Analyze(string entityName)
    {
        var entityType = _assembly.GetTypes()
            .FirstOrDefault(t => t.Name == entityName && t.IsClass && !t.IsAbstract)
            ?? throw new InvalidOperationException($"Entity '{entityName}' not found in assembly");

        return AnalyzeType(entityType);
    }

    public EntityMetadata AnalyzeType(Type entityType)
    {
        var metadata = new EntityMetadata
        {
            Name = entityType.Name,
            Namespace = entityType.Namespace ?? string.Empty,
            PluralName = entityType.Name.Pluralize()
        };

        // Detect base class hierarchy and characteristics
        DetectBaseClassHierarchy(metadata, entityType);

        // Analyze properties
        foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propMeta = AnalyzeProperty(property);
            if (propMeta != null)
            {
                metadata.Properties.Add(propMeta);
            }
        }

        // Detect foreign keys from property patterns
        DetectForeignKeys(metadata);

        return metadata;
    }

    private void DetectBaseClassHierarchy(EntityMetadata metadata, Type entityType)
    {
        var baseType = entityType.BaseType;

        while (baseType != null && baseType != typeof(object))
        {
            var baseName = baseType.IsGenericType
                ? baseType.GetGenericTypeDefinition().Name.Split('`')[0]
                : baseType.Name;

            // Detect entity characteristics from base class
            if (baseName.Contains("AggregateRoot"))
            {
                metadata.IsAggregateRoot = true;
                metadata.IsAuditable = true;
            }
            else if (baseName.Contains("FullAuditableEntity") || baseName.Contains("FullMultiTenantEntity"))
            {
                metadata.IsAuditable = true;
                metadata.IsSoftDelete = true;
            }
            else if (baseName.Contains("AuditableEntity") || baseName.Contains("MultiTenantEntity"))
            {
                metadata.IsAuditable = true;
            }

            if (baseName.Contains("MultiTenant"))
            {
                metadata.IsMultiTenant = true;
            }

            // Extract key type from generic argument
            if (baseType.IsGenericType && metadata.KeyType == null)
            {
                var keyType = baseType.GetGenericArguments().FirstOrDefault();
                if (keyType != null)
                {
                    metadata.KeyType = keyType;
                }
            }

            metadata.BaseClass = baseName;
            baseType = baseType.BaseType;
        }

        // Also check implemented interfaces
        foreach (var iface in entityType.GetInterfaces())
        {
            var ifaceName = iface.Name;
            if (ifaceName == "ISoftDelete")
            {
                metadata.IsSoftDelete = true;
            }
            else if (ifaceName == "IMultiTenant")
            {
                metadata.IsMultiTenant = true;
            }
            else if (ifaceName == "IAuditableEntity")
            {
                metadata.IsAuditable = true;
            }
        }

        // Default key type
        metadata.KeyType ??= typeof(Guid);
    }

    private PropertyMetadata? AnalyzeProperty(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        var isNullable = false;
        var underlyingType = propertyType;

        // Handle nullable types
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            isNullable = true;
            underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        }
        else if (!propertyType.IsValueType)
        {
            // Check for nullable reference type (NRT)
            var nullableAttribute = property.CustomAttributes
                .FirstOrDefault(a => a.AttributeType.Name == "NullableAttribute");
            if (nullableAttribute != null)
            {
                // NRT flag: 2 = nullable, 1 = not nullable
                var args = nullableAttribute.ConstructorArguments.FirstOrDefault();
                if (args.Value is byte b && b == 2)
                {
                    isNullable = true;
                }
            }
        }

        // Check if it's a collection (navigation property)
        var isCollection = propertyType.IsGenericType &&
            (propertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
             propertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
             propertyType.GetGenericTypeDefinition() == typeof(List<>) ||
             propertyType.GetGenericTypeDefinition() == typeof(IList<>));

        // Check if it's a navigation property (complex type, not primitive)
        var isNavigation = !propertyType.IsPrimitive &&
                          propertyType != typeof(string) &&
                          propertyType != typeof(Guid) &&
                          propertyType != typeof(DateTime) &&
                          propertyType != typeof(DateTimeOffset) &&
                          propertyType != typeof(decimal) &&
                          !propertyType.IsEnum &&
                          !isNullable &&
                          !isCollection &&
                          propertyType.IsClass &&
                          property.GetMethod?.IsVirtual == true;

        // Get type name for code generation
        var typeName = GetTypeName(propertyType);

        var propMeta = new PropertyMetadata
        {
            Name = property.Name,
            TypeName = typeName,
            ClrType = underlyingType,
            IsNullable = isNullable,
            IsRequired = !isNullable && property.Name != "Id",
            IsCollection = isCollection,
            IsNavigationProperty = isNavigation || isCollection,
            IsAuditProperty = AuditProperties.Contains(property.Name),
            IsSoftDeleteProperty = SoftDeleteProperties.Contains(property.Name),
            IsPrimaryKey = property.Name == "Id",
            IsTenantProperty = property.Name == "TenantId",
            IsEnumType = underlyingType.IsEnum
        };

        // Check for MaxLength attribute
        var maxLengthAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>();
        if (maxLengthAttr != null)
        {
            propMeta.MaxLength = maxLengthAttr.Length;
        }

        var stringLengthAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>();
        if (stringLengthAttr != null)
        {
            propMeta.MaxLength = stringLengthAttr.MaximumLength;
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

            // Also consider it a FK if there's no navigation but the name pattern matches
            var relatedEntityName = navigationName;

            prop.IsForeignKey = true;
            prop.ForeignKeyInfo = new ForeignKeyInfo
            {
                PropertyName = prop.Name,
                NavigationPropertyName = navProp?.Name ?? navigationName,
                RelatedEntityName = relatedEntityName,
                RelatedEntityPluralName = relatedEntityName.Pluralize(),
                DisplayProperty = "Name", // Default, can be customized
                IsRequired = !prop.IsNullable
            };

            metadata.ForeignKeys.Add(prop.ForeignKeyInfo);
        }
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlying = Nullable.GetUnderlyingType(type);
            return GetSimpleTypeName(underlying!) + "?";
        }

        return GetSimpleTypeName(type);
    }

    private static string GetSimpleTypeName(Type type)
    {
        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Boolean" => "bool",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Byte" => "byte",
            "Char" => "char",
            "Object" => "object",
            _ => type.Name
        };
    }
}
