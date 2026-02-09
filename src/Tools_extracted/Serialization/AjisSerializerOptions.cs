namespace Afrowave.AJIS;

/// <summary>
/// Options for customizing AJIS serialization behavior.
/// </summary>
public sealed class AjisSerializerOptions
{
    /// <summary>
    /// Gets or sets the naming policy for property names during serialization.
    /// </summary>
    /// <remarks>
    /// When set, property names will be converted according to the policy when writing.
    /// For example, with CamelCase policy, "MyVariable" becomes "myVariable" in the output.
    /// </remarks>
    public AjisNamingPolicy? PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether to write indented (pretty-printed) output.
    /// </summary>
    public bool WriteIndented { get; set; } = false;

    /// <summary>
    /// Gets or sets the indentation string to use when WriteIndented is true.
    /// </summary>
    public string IndentString { get; set; } = "  ";

    /// <summary>
    /// Gets or sets whether to use AJIS-specific features (hex, binary, separators).
    /// When false, outputs standard JSON.
    /// </summary>
    public bool UseAjisExtensions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to escape non-ASCII characters in strings.
    /// </summary>
    public bool EscapeNonAscii { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to write null values for null properties.
    /// </summary>
    public bool WriteNullValues { get; set; } = true;

    /// <summary>
    /// Creates serializer options configured for strict JSON output.
    /// </summary>
    public static AjisSerializerOptions Json => new AjisSerializerOptions
    {
        WriteIndented = false,
        UseAjisExtensions = false,
        EscapeNonAscii = true,
        WriteNullValues = true
    };

    /// <summary>
    /// Creates serializer options configured for AJIS output with all features.
    /// </summary>
    public static AjisSerializerOptions Ajis => new AjisSerializerOptions
    {
        WriteIndented = true,
        UseAjisExtensions = true,
        EscapeNonAscii = false,
        WriteNullValues = true
    };

    /// <summary>
    /// Creates serializer options configured for compact JSON output (minified).
    /// </summary>
    public static AjisSerializerOptions Compact => new AjisSerializerOptions
    {
        WriteIndented = false,
        UseAjisExtensions = false,
        EscapeNonAscii = false,
        WriteNullValues = false
    };
}
