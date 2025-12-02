using Humanizer;

namespace Framework.CodeGen.Utilities;

/// <summary>
/// String utility extensions for code generation
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Pluralizes a word (e.g., "Category" -> "Categories")
    /// </summary>
    public static string Pluralize(this string word)
    {
        return word.Pluralize(inputIsKnownToBeSingular: false);
    }

    /// <summary>
    /// Singularizes a word (e.g., "Categories" -> "Category")
    /// </summary>
    public static string Singularize(this string word)
    {
        return word.Singularize(inputIsKnownToBePlural: false);
    }

    /// <summary>
    /// Converts to camelCase
    /// </summary>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    /// <summary>
    /// Converts to PascalCase
    /// </summary>
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpperInvariant(str[0]) + str[1..];
    }

    /// <summary>
    /// Converts PascalCase to kebab-case
    /// </summary>
    public static string ToKebabCase(this string str)
    {
        return str.Kebaberize();
    }

    /// <summary>
    /// Converts PascalCase to snake_case
    /// </summary>
    public static string ToSnakeCase(this string str)
    {
        return str.Underscore();
    }

    /// <summary>
    /// Humanizes a PascalCase string (e.g., "FirstName" -> "First Name")
    /// </summary>
    public static string ToDisplayName(this string str)
    {
        return str.Humanize(LetterCasing.Title);
    }

    /// <summary>
    /// Gets the default value string for a type
    /// </summary>
    public static string GetDefaultValueString(this Type type)
    {
        if (type == typeof(string)) return "string.Empty";
        if (type == typeof(bool)) return "false";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "0";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "0";
        if (type == typeof(DateTime)) return "DateTime.MinValue";
        if (type == typeof(DateTimeOffset)) return "DateTimeOffset.MinValue";
        if (type == typeof(Guid)) return "Guid.Empty";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return "null";
        if (type.IsClass) return "null!";
        return "default";
    }

    /// <summary>
    /// Checks if a type is nullable
    /// </summary>
    public static bool IsNullableType(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Gets the underlying type if nullable, otherwise returns the type itself
    /// </summary>
    public static Type GetUnderlyingTypeIfNullable(this Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }
}
