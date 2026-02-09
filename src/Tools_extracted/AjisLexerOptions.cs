namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Options for the AJIS lexer.
/// </summary>
public sealed class AjisLexerOptions
{
    /// <summary>
    /// Allow multiline strings (AJIS extension).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowMultilineStrings { get; set; }

    /// <summary>
    /// Allow number separators like 1_000_000 (AJIS extension).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowNumberSeparators { get; set; }

    /// <summary>
    /// Allow comments (// and /* */ style).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowComments { get; set; }

    /// <summary>
    /// Allow hexadecimal number literals like 0xFF (AJIS extension).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowHexLiterals { get; set; }

    /// <summary>
    /// Allow binary number literals like 0b1010 (AJIS extension).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowBinaryLiterals { get; set; }

    /// <summary>
    /// Allow octal number literals like 0o755 (AJIS extension).
    /// Default: false (JSON-compatible).
    /// </summary>
    public bool AllowOctalLiterals { get; set; }

    /// <summary>
    /// Gets options for strict JSON compatibility.
    /// </summary>
    public static AjisLexerOptions Json => new();

    /// <summary>
    /// Gets options with all AJIS extensions enabled.
    /// </summary>
    public static AjisLexerOptions Ajis => new()
    {
        AllowMultilineStrings = true,
        AllowNumberSeparators = true,
        AllowComments = true,
        AllowHexLiterals = true,
        AllowBinaryLiterals = true,
        AllowOctalLiterals = true,
    };
}
