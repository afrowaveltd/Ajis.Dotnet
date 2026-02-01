namespace Afrowave.AJIS.Streaming.Walk;

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
   public StreamWalkMode Mode { get; init; } = StreamWalkMode.Ajis;

   /// <summary>
   /// Whether comment tokens are recognized/produced.
   /// </summary>
   public bool Comments { get; init; } = true;

   /// <summary>
   /// Whether directives are recognized/produced.
   /// </summary>
   public bool Directives { get; init; } = true;

   /// <summary>
   /// Whether identifiers are recognized/produced.
   /// </summary>
   public bool Identifiers { get; init; } = true;
   /// <summary>
   /// Maximum nesting depth allowed.
   /// </summary>
   public int MaxDepth { get; init; } = 256;

   /// <summary>
   /// Maximum bytes allowed per token (safety limit for streaming scenarios).
   /// </summary>
   public int MaxTokenBytes { get; init; } = 8 * 1024 * 1024; // 8 MiB

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
