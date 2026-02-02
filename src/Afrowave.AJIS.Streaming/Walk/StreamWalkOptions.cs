#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Options for the StreamWalk engine.
/// Defaults represent the baseline profile used by milestone M1.
/// </summary>
/// <remarks>
/// <para>
/// This is a <c>record struct</c> so callers can use <c>with { ... }</c>.
/// </para>
/// </remarks>
public readonly record struct AjisStreamWalkOptions
{
   /// <summary>
   /// Parse mode for StreamWalk.
   /// </summary>
   public AjisStreamWalkMode Mode { get; init; }

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

   // CS8983 fix: Structs with field/property initializers require an explicitly declared constructor.
   // We keep defaults centralized here instead of using inline initializers.
   public AjisStreamWalkOptions()
   {
      Mode = AjisStreamWalkMode.Ajis;
      Comments = true;
      Directives = true;
      Identifiers = true;
      MaxDepth = 256;
      MaxTokenBytes = 1024 * 1024; // 1 MiB
   }

   /// <summary>
   /// Canonical default profile for milestone M1.
   /// </summary>
   public static AjisStreamWalkOptions DefaultForM1 => new();
}

/// <summary>
/// Runner-only options (behavioral switches not part of the parsing model).
/// </summary>
public readonly record struct AjisStreamWalkRunnerOptions
{
   /// <summary>
   /// If true, the runner respects <see cref="IAjisStreamWalkVisitor.OnEvent"/> returning false
   /// as a request to abort early.
   /// </summary>
   public bool AllowVisitorAbort { get; init; }

   public AjisStreamWalkRunnerOptions()
   {
      AllowVisitorAbort = false;
   }

   public static AjisStreamWalkRunnerOptions Default => new();
}