#nullable enable

namespace Afrowave.AJIS.Core;

/// <summary>
/// Defines the text mode for AJIS parsing.
/// </summary>
/// <remarks>
/// <para>
/// Different modes enforce or relax syntax rules:
/// </para>
/// <list type="bullet">
/// <item><description>Json: Strict JSON compliance (RFC 8259)</description></item>
/// <item><description>Ajis: AJIS specification compliance (allows comments, directives, trailing commas, etc.)</description></item>
/// <item><description>Lex: Permissive mode with relaxed rules</description></item>
/// <item><description>Lax: JavaScript-tolerant mode (unquoted keys, single quotes, JS comments)</description></item>
/// </list>
/// </remarks>
public enum AjisTextMode
{
    /// <summary>
    /// Strict JSON mode (RFC 8259 compliant).
    /// No comments, no directives, no trailing commas, no extensions.
    /// </summary>
    Json = 0,

    /// <summary>
    /// AJIS specification mode.
    /// Allows comments, directives, trailing commas, and AJIS extensions.
    /// </summary>
    Ajis = 1,

    /// <summary>
    /// Permissive mode with relaxed rules.
    /// Similar to Lex/LAX with flexible handling of syntax.
    /// </summary>
    Lex = 2,

    /// <summary>
    /// JavaScript-tolerant mode.
    /// Allows unquoted keys, single-quoted strings, and JavaScript-style comments.
    /// Useful for parsing JavaScript objects and comments.
    /// </summary>
    Lax = 3
}
