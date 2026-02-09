using System;
using System.Text;

namespace Afrowave.AJIS;

/// <summary>
/// Defines how property names are converted during serialization/deserialization.
/// Similar to System.Text.Json.JsonNamingPolicy.
/// </summary>
public abstract class AjisNamingPolicy
{
    /// <summary>
    /// When overridden in a derived class, converts the specified name according to the policy.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The converted name.</returns>
    public abstract string ConvertName(string name);

    /// <summary>
    /// Gets a naming policy that converts names to camelCase (e.g., MyVariable → myVariable).
    /// </summary>
    public static AjisNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that converts names to snake_case (e.g., MyVariable → my_variable).
    /// </summary>
    public static AjisNamingPolicy SnakeCase { get; } = new SnakeCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that converts names to kebab-case (e.g., MyVariable → my-variable).
    /// </summary>
    public static AjisNamingPolicy KebabCase { get; } = new KebabCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that keeps names unchanged.
    /// </summary>
    public static AjisNamingPolicy None { get; } = new NoConversionNamingPolicy();

    private sealed class CamelCaseNamingPolicy : AjisNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
                return name;

            var chars = name.ToCharArray();
            FixCasing(chars, 0, chars.Length);
            return new string(chars);
        }

        private static void FixCasing(char[] chars, int start, int end)
        {
            // Convert first character to lowercase
            if (start < end && char.IsUpper(chars[start]))
            {
                chars[start] = char.ToLowerInvariant(chars[start]);
            }
        }
    }

    private sealed class SnakeCaseNamingPolicy : AjisNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var sb = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (char.IsUpper(c))
                {
                    // Add underscore before uppercase letter (except at start)
                    if (i > 0)
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }

    private sealed class KebabCaseNamingPolicy : AjisNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var sb = new StringBuilder(name.Length + 4);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (char.IsUpper(c))
                {
                    // Add hyphen before uppercase letter (except at start)
                    if (i > 0)
                        sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }

    private sealed class NoConversionNamingPolicy : AjisNamingPolicy
    {
        public override string ConvertName(string name) => name;
    }
}
