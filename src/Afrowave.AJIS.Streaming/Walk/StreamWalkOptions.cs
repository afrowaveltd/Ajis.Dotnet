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

   /// <summary>
   /// Creates StreamWalk options from core configuration settings.
   /// </summary>
   public static AjisStreamWalkOptions FromSettings(global::Afrowave.AJIS.Core.Configuration.AjisSettings settings)
   {
      ArgumentNullException.ThrowIfNull(settings);

      AjisStreamWalkMode mode = settings.TextMode switch
      {
         global::Afrowave.AJIS.Core.AjisTextMode.Json => AjisStreamWalkMode.Json,
         global::Afrowave.AJIS.Core.AjisTextMode.Lex => AjisStreamWalkMode.Lax,
         _ => AjisStreamWalkMode.Ajis
      };

      bool commentsEnabled = settings.Comments.AllowLineComments || settings.Comments.AllowBlockComments;

      return DefaultForM1 with
      {
         Mode = mode,
         Comments = commentsEnabled,
         Directives = settings.AllowDirectives,
         Identifiers = settings.Strings.AllowUnquotedPropertyNames,
         MaxDepth = settings.MaxDepth,
         MaxTokenBytes = settings.MaxTokenBytes
      };
   }
}

/// <summary>
/// Runner-only options (behavioral switches not part of the parsing model).
/// </summary>
/// <remarks>
/// Values here affect execution behavior, not the parsing model itself.
/// </remarks>
/// <summary>
/// Engine selection preference (M1.1).
/// </summary>
public enum AjisStreamWalkEnginePreference
{
   /// <summary>
   /// Balance memory usage and throughput.
   /// </summary>
   Balanced = 0,
   /// <summary>
   /// Prefer lower memory usage over throughput.
   /// </summary>
   LowMemory = 1,
   /// <summary>
   /// Prefer throughput over memory usage.
   /// </summary>
   Speed = 2
}

/// <summary>
/// Runner-only options controlling StreamWalk execution behavior.
/// </summary>
public readonly record struct AjisStreamWalkRunnerOptions
{
   /// <summary>
   /// Whether the visitor is allowed to abort the walk.
   /// </summary>
   public bool AllowVisitorAbort { get; init; }

   /// <summary>
   /// Preferred engine selection strategy.
   /// </summary>
   public AjisStreamWalkEnginePreference EnginePreference { get; init; }

   /// <summary>
   /// Payload size threshold (in bytes) that triggers large-payload handling.
   /// </summary>
   public int LargePayloadThresholdBytes { get; init; }

   /// <summary>
   /// Whether debug diagnostics are emitted during execution.
   /// </summary>
   public bool EmitDebugDiagnostics { get; init; }

   /// <summary>
   /// Optional callback invoked for each diagnostic.
   /// </summary>
   public Action<AjisDiagnostic>? OnDiagnostic { get; init; }

   /// <summary>
   /// Initializes the default runner-only options.
   /// </summary>
   public AjisStreamWalkRunnerOptions()
   {
      AllowVisitorAbort = false;
      EnginePreference = AjisStreamWalkEnginePreference.Balanced;
      LargePayloadThresholdBytes = 256 * 1024; // 256 KiB
      EmitDebugDiagnostics = false;
      OnDiagnostic = null;
   }

   /// <summary>
   /// Canonical default runner-only profile.
   /// </summary>
   public static AjisStreamWalkRunnerOptions Default => new();

   /// <summary>
   /// Creates runner options from core settings, applying the parser processing profile.
   /// </summary>
   public static AjisStreamWalkRunnerOptions FromSettings(global::Afrowave.AJIS.Core.Configuration.AjisSettings? settings)
   {
      AjisStreamWalkRunnerOptions options = Default;

      if(settings is null)
         return options;

      return options.WithProcessingProfileIfDefault(settings.ParserProfile);
   }

   /// <summary>
   /// Applies a processing profile to the engine preference.
   /// </summary>
   public AjisStreamWalkRunnerOptions WithProcessingProfile(global::Afrowave.AJIS.Core.AjisProcessingProfile profile)
      => this with
      {
         EnginePreference = profile switch
         {
            global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory => AjisStreamWalkEnginePreference.LowMemory,
            global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput => AjisStreamWalkEnginePreference.Speed,
            _ => AjisStreamWalkEnginePreference.Balanced
         }
      };

   /// <summary>
   /// Applies a processing profile when the engine preference is still at its default.
   /// </summary>
   public AjisStreamWalkRunnerOptions WithProcessingProfileIfDefault(global::Afrowave.AJIS.Core.AjisProcessingProfile profile)
      => EnginePreference == AjisStreamWalkEnginePreference.Balanced
         ? WithProcessingProfile(profile)
         : this;
}