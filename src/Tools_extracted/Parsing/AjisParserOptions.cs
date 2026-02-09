namespace Afrowave.AJIS;

/// <summary>
/// Options for customizing AJIS parser behavior.
/// </summary>
public sealed class AjisParserOptions
{
    /// <summary>
    /// Gets or sets the naming policy for property names during deserialization.
    /// </summary>
    /// <remarks>
    /// When set, property names from the AJIS/JSON input will be converted according to the policy
    /// before being stored. For example, with CamelCase policy, "myVariable" in AJIS becomes "MyVariable" internally.
    /// </remarks>
    public AjisNamingPolicy? PropertyNamingPolicy { get; set; }

    /// <summary>
    /// Gets or sets whether to allow comments in the input.
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow trailing commas in objects and arrays.
    /// </summary>
    public bool AllowTrailingCommas { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow numeric separators (e.g., 1_000_000).
    /// </summary>
    public bool AllowNumericSeparators { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow hex/binary/octal number literals.
    /// </summary>
    public bool AllowExtendedNumberFormats { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw on duplicate object keys.
    /// </summary>
    public bool ThrowOnDuplicateKeys { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to pool repeated object keys during parsing.
    /// This reduces duplicated string instances across objects.
    /// </summary>
    public bool EnableKeyPooling { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to pool repeated string values during parsing.
    /// This reduces duplicated string instances across values.
    /// </summary>
    public bool EnableStringPooling { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to lazily materialize UTF-8 strings (decode on demand).
    /// Applies to UTF-8 parsing when backing buffers are available.
    /// </summary>
    public bool EnableLazyStringMaterialization { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum parsing depth to prevent stack overflow.
    /// </summary>
    public int MaxDepth { get; set; } = 64;

    /// <summary>
    /// Enable parallel parsing paths (when available) for large arrays/objects.
    /// Default: true.
    /// </summary>
    public bool EnableParallelParsing { get; set; } = true;

    /// <summary>
    /// Optional cap for parallel degree of parallelism. When null the parser will choose a heuristic.
    /// </summary>
    public int? MaxParallelDegree { get; set; } = null;

    /// <summary>
    /// When true, collect simple telemetry for parallel parsing and append to ajis_parallel_bench.csv in the current directory.
    /// Default: false.
    /// </summary>
    public bool CollectParallelMetrics { get; set; } = false;

    /// <summary>
    /// Creates parser options configured for strict JSON compatibility.
    /// </summary>
    public static AjisParserOptions Json => new AjisParserOptions
    {
        AllowComments = false,
        AllowTrailingCommas = false,
        AllowNumericSeparators = false,
        AllowExtendedNumberFormats = false,
        ThrowOnDuplicateKeys = true,
        EnableKeyPooling = false,
        EnableStringPooling = false,
        EnableLazyStringMaterialization = false,
        MaxDepth = 64
    };

    /// <summary>
    /// Creates parser options configured for full AJIS feature support.
    /// </summary>
    public static AjisParserOptions Ajis => new AjisParserOptions
    {
        AllowComments = true,
        AllowTrailingCommas = true,
        AllowNumericSeparators = true,
        AllowExtendedNumberFormats = true,
        ThrowOnDuplicateKeys = false,
        EnableKeyPooling = true,
        EnableStringPooling = false,
        EnableLazyStringMaterialization = true,
        MaxDepth = 64
    };
}
