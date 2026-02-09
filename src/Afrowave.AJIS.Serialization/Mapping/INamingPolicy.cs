#nullable enable

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Defines how property names are converted between .NET and AJIS representations.
/// </summary>
/// <remarks>
/// <para>
/// Naming policies are used during serialization and deserialization to map between
/// .NET property names (typically PascalCase) and AJIS keys (which may use different conventions).
/// </para>
/// <para>
/// Policies must be deterministic: the same input always produces the same output.
/// </para>
/// </remarks>
public interface INamingPolicy
{
    /// <summary>
    /// Converts a .NET property name to an AJIS key name.
    /// </summary>
    /// <param name="propertyName">The .NET property name (typically PascalCase).</param>
    /// <returns>The converted AJIS key name according to this policy.</returns>
    /// <remarks>
    /// Examples:
    /// - PascalCase policy: "FirstName" → "FirstName"
    /// - CamelCase policy: "FirstName" → "firstName"
    /// - snake_case policy: "FirstName" → "first_name"
    /// </remarks>
    string ConvertName(string propertyName);
}

/// <summary>
/// Default naming policy: property names are used as-is (identity transformation).
/// </summary>
/// <remarks>
/// This policy maps .NET PascalCase names directly to AJIS keys without modification.
/// </remarks>
public sealed class PascalCaseNamingPolicy : INamingPolicy
{
    /// <summary>
    /// Gets the singleton instance of the PascalCase naming policy.
    /// </summary>
    public static readonly PascalCaseNamingPolicy Instance = new();

    /// <summary>
    /// Returns the property name unchanged (identity policy).
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>The same property name unchanged.</returns>
    public string ConvertName(string propertyName) => propertyName;

    /// <summary>
    /// Returns a user-friendly name for this policy.
    /// </summary>
    public override string ToString() => "PascalCase";
}

/// <summary>
/// Naming policy that converts PascalCase names to camelCase.
/// </summary>
/// <remarks>
/// <para>
/// Converts the first letter to lowercase while preserving the rest of the name.
/// This is commonly used for JavaScript/REST API compatibility.
/// </para>
/// <para>
/// Examples:
/// - "FirstName" → "firstName"
/// - "userId" → "userId"
/// - "ID" → "iD"
/// </para>
/// </remarks>
public sealed class CamelCaseNamingPolicy : INamingPolicy
{
    /// <summary>
    /// Gets the singleton instance of the CamelCase naming policy.
    /// </summary>
    public static readonly CamelCaseNamingPolicy Instance = new();

    /// <summary>
    /// Converts a property name from PascalCase to camelCase.
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>The name with the first letter converted to lowercase.</returns>
    public string ConvertName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        if (propertyName.Length == 1)
            return char.ToLowerInvariant(propertyName[0]).ToString();

        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    /// <summary>
    /// Returns a user-friendly name for this policy.
    /// </summary>
    public override string ToString() => "camelCase";
}

/// <summary>
/// Naming policy that converts PascalCase names to snake_case.
/// </summary>
/// <remarks>
/// <para>
/// Inserts underscores before uppercase letters and converts all letters to lowercase.
/// This is commonly used for database column names and configuration files.
/// </para>
/// <para>
/// Examples:
/// - "FirstName" → "first_name"
/// - "userId" → "user_id"
/// - "HTTPResponse" → "h_t_t_p_response"
/// </para>
/// </remarks>
public sealed class SnakeCaseNamingPolicy : INamingPolicy
{
    /// <summary>
    /// Gets the singleton instance of the snake_case naming policy.
    /// </summary>
    public static readonly SnakeCaseNamingPolicy Instance = new();

    /// <summary>
    /// Converts a property name from PascalCase to snake_case.
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>The name converted to snake_case with underscores and lowercase letters.</returns>
    public string ConvertName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        var result = new System.Text.StringBuilder();

        for (int i = 0; i < propertyName.Length; i++)
        {
            char c = propertyName[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns a user-friendly name for this policy.
    /// </summary>
    public override string ToString() => "snake_case";
}

/// <summary>
/// Naming policy that converts PascalCase names to kebab-case.
/// </summary>
/// <remarks>
/// <para>
/// Inserts hyphens before uppercase letters and converts all letters to lowercase.
/// This is commonly used for configuration files and command-line options.
/// </para>
/// <para>
/// Examples:
/// - "FirstName" → "first-name"
/// - "userId" → "user-id"
/// - "HTTPResponse" → "h-t-t-p-response"
/// </para>
/// </remarks>
public sealed class KebabCaseNamingPolicy : INamingPolicy
{
    /// <summary>
    /// Gets the singleton instance of the kebab-case naming policy.
    /// </summary>
    public static readonly KebabCaseNamingPolicy Instance = new();

    /// <summary>
    /// Converts a property name from PascalCase to kebab-case.
    /// </summary>
    /// <param name="propertyName">The property name to convert.</param>
    /// <returns>The name converted to kebab-case with hyphens and lowercase letters.</returns>
    public string ConvertName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        var result = new System.Text.StringBuilder();

        for (int i = 0; i < propertyName.Length; i++)
        {
            char c = propertyName[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Returns a user-friendly name for this policy.
    /// </summary>
    public override string ToString() => "kebab-case";
}
