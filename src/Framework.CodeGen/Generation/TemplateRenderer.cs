using System.Reflection;
using Framework.CodeGen.Analysis.Models;
using Framework.CodeGen.Utilities;
using Scriban;
using Scriban.Runtime;

namespace Framework.CodeGen.Generation;

/// <summary>
/// Renders Scriban templates for code generation
/// </summary>
public class TemplateRenderer
{
    private readonly string? _customTemplatesPath;

    public TemplateRenderer(string? customTemplatesPath = null)
    {
        _customTemplatesPath = customTemplatesPath;
    }

    /// <summary>
    /// Renders a template with the given context
    /// </summary>
    public string Render(string templateName, GenerationContext context)
    {
        var templateContent = LoadTemplate(templateName);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            var errors = string.Join("\n", template.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template '{templateName}' has errors:\n{errors}");
        }

        var scriptObject = CreateScriptObject(context);
        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        // Add custom functions
        templateContext.BuiltinObject.SetValue("string", new StringFunctions(), true);

        return template.Render(templateContext);
    }

    private string LoadTemplate(string templateName)
    {
        // Try custom templates first
        if (!string.IsNullOrEmpty(_customTemplatesPath))
        {
            var customPath = Path.Combine(_customTemplatesPath, templateName);
            if (File.Exists(customPath))
            {
                return File.ReadAllText(customPath);
            }
        }

        // Fall back to embedded templates
        var resourceName = templateName.Replace("/", ".").Replace("\\", ".");
        var assembly = Assembly.GetExecutingAssembly();

        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Try with different path format
            var altResourceName = $"Api.{Path.GetFileName(templateName)}";
            stream = assembly.GetManifestResourceStream(altResourceName);

            if (stream == null)
            {
                throw new FileNotFoundException($"Template not found: {templateName}");
            }
        }

        using (stream)
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    private static ScriptObject CreateScriptObject(GenerationContext context)
    {
        var scriptObject = new ScriptObject();

        // Entity info
        scriptObject.Add("entity_name", context.EntityName);
        scriptObject.Add("entity_plural", context.EntityPlural);
        scriptObject.Add("namespace", context.Namespace);
        scriptObject.Add("domain_namespace", context.DomainNamespace);
        scriptObject.Add("application_namespace", context.ApplicationNamespace);
        scriptObject.Add("route_path", context.RoutePath);

        // Entity characteristics
        scriptObject.Add("is_auditable", context.IsAuditable);
        scriptObject.Add("is_soft_delete", context.IsSoftDelete);
        scriptObject.Add("is_multi_tenant", context.IsMultiTenant);

        // Properties
        scriptObject.Add("create_properties", context.CreateProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("update_properties", context.UpdateProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("list_properties", context.ListProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("filter_properties", context.FilterProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("form_properties", context.FormProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("search_properties", context.SearchProperties.Select(ToPropertyObject).ToList());
        scriptObject.Add("list_columns", context.ListColumns.Select(ToColumnObject).ToList());

        // Foreign keys
        scriptObject.Add("foreign_keys", context.ForeignKeys.Select(ToForeignKeyObject).ToList());

        return scriptObject;
    }

    private static ScriptObject ToPropertyObject(PropertyMetadata prop)
    {
        var obj = new ScriptObject();
        obj.Add("name", prop.Name);
        obj.Add("type_name", prop.TypeName);
        obj.Add("display_name", prop.Name.ToDisplayName());
        obj.Add("is_nullable", prop.IsNullable);
        obj.Add("is_required", prop.IsRequired);
        obj.Add("is_foreign_key", prop.IsForeignKey);
        obj.Add("max_length", prop.MaxLength);
        obj.Add("default_value", GetDefaultValueString(prop));
        obj.Add("input_type", prop.InputType);
        obj.Add("form_control_type", prop.FormControlType);

        if (prop.ForeignKeyInfo != null)
        {
            obj.Add("fk_info", ToForeignKeyObject(prop.ForeignKeyInfo));
        }

        return obj;
    }

    private static ScriptObject ToColumnObject(PropertyMetadata prop)
    {
        var obj = new ScriptObject();
        obj.Add("name", prop.Name);
        obj.Add("type_name", prop.TypeName);
        obj.Add("display_name", prop.Name.ToDisplayName());
        return obj;
    }

    private static ScriptObject ToForeignKeyObject(ForeignKeyInfo fk)
    {
        var obj = new ScriptObject();
        obj.Add("property_name", fk.PropertyName);
        obj.Add("navigation_property_name", fk.NavigationPropertyName);
        obj.Add("related_entity_name", fk.RelatedEntityName);
        obj.Add("related_entity_plural_name", fk.RelatedEntityPluralName);
        obj.Add("display_property", fk.DisplayProperty);
        obj.Add("is_required", fk.IsRequired);
        obj.Add("variable_name", fk.VariableName);
        obj.Add("list_variable_name", fk.ListVariableName);
        obj.Add("filter_variable_name", fk.FilterVariableName);
        return obj;
    }

    private static string? GetDefaultValueString(PropertyMetadata prop)
    {
        if (prop.TypeName == "string" && !prop.IsNullable)
            return "string.Empty";
        if (prop.IsCollection)
            return "new()";
        return null;
    }
}

/// <summary>
/// Custom string functions for Scriban templates
/// </summary>
public class StringFunctions : ScriptObject
{
    public static string Downcase(string text) => text?.ToLowerInvariant() ?? string.Empty;
    public static string Upcase(string text) => text?.ToUpperInvariant() ?? string.Empty;
    public static string Capitalize(string text) => text != null ? StringExtensions.ToPascalCase(text) : string.Empty;
    public static string Camelize(string text) => text != null ? StringExtensions.ToCamelCase(text) : string.Empty;
}
