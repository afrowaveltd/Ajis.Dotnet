#nullable enable

using Afrowave.AJIS.Core.Diagnostics;

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
   /// When enabled, the runner may stop early if the visitor requests it.
   /// </summary>
   public bool AllowVisitorAbort { get; init; }

   /// <summary>
   /// If true, emit <c>Debug</c>-level diagnostics for recoverable issues (primarily for <see cref="AjisStreamWalkMode.Lax"/>).
   /// </summary>
   public bool EmitDebugDiagnostics { get; init; }

   /// <summary>
   /// Optional diagnostic sink.
   /// </summary>
   /// <remarks>
   /// <para>
   /// This is intentionally on the runner options (not the parsing options): it does not affect parsing results,
   /// only observability.
   /// </para>
   /// <para>
   /// When null, no diagnostics are forwarded.
   /// </para>
   /// </remarks>
   public Action<AjisDiagnostic>? OnDiagnostic { get; init; }

   public AjisStreamWalkRunnerOptions()
   {
      AllowVisitorAbort = false;
      EmitDebugDiagnostics = false;
      OnDiagnostic = null;
   }

   public static AjisStreamWalkRunnerOptions Default => new();
}
