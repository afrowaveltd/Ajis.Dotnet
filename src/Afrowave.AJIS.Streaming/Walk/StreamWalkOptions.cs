namespace Afrowave.AJIS.Streaming;

/// <summary>
/// Options parsed from a StreamWalk test-case file.
/// Defaults represent the baseline profile used by the first milestone (M1).
///
/// NOTE: This must be a record/record struct so callers can use the <c>with { ... }</c> expression.
/// </summary>
public readonly record struct StreamWalkOptions
{
   /// <summary>
   /// Parse mode for StreamWalk.
   /// </summary>
   public StreamWalkMode Mode { get; init; }

   /// <summary>
   /// Whether comment tokens are recognized/produced.
   /// </summary>
   public bool Comments { get; init; }

   /// <summary>
   /// Whether directives are recognized/produced.
   /// </summary>
   public bool Directives { get; init; }

   /// <summary>
   /// Whether identifiers are recognized/produced.
   /// </summary>
   public bool Identifiers { get; init; }

   /// <summary>
   /// Maximum nesting depth allowed.
   /// </summary>
   public int MaxDepth { get; init; }

   /// <summary>
   /// Maximum bytes allowed per token (safety limit for streaming scenarios).
   /// </summary>
   public int MaxTokenBytes { get; init; }

   // CS8983 fix:
   // Structs with field/property initializers require an explicitly declared constructor.
   // We keep defaults centralized here instead of using inline initializers.
   public StreamWalkOptions()
   {
      Mode = StreamWalkMode.Ajis;
      Comments = true;
      Directives = true;
      Identifiers = true;
      MaxDepth = 256;
      MaxTokenBytes = 1024 * 1024; // 1 MiB
   }

   /// <summary>
   /// Canonical default profile for milestone M1.
   /// </summary>
   public static StreamWalkOptions DefaultForM1 => new();
}

public enum StreamWalkMode
{
   Auto = 0,
   Ajis = 1,
   Json = 2,
}
