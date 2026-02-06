#nullable enable

using Afrowave.AJIS.Core.Diagnostics;
using Afrowave.AJIS.Streaming.Walk.Engines;
using Afrowave.AJIS.Streaming.Walk.Input;

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// StreamWalk runner (public entry point).
/// </summary>
public static class AjisStreamWalkRunner
{
   /// <summary>
   /// Runs StreamWalk over an in-memory UTF-8 payload with core settings applied.
   /// </summary>
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      global::Afrowave.AJIS.Core.Configuration.AjisSettings? settings,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      AjisStreamWalkRunnerOptions effective = settings is null
         ? runnerOptions
         : runnerOptions.WithProcessingProfileIfDefault(settings.ParserProfile);

      Run(inputUtf8, options, visitor, effective);
   }

   /// <summary>
   /// Runs StreamWalk over an in-memory UTF-8 payload using core configuration settings.
   /// </summary>
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      IAjisStreamWalkVisitor visitor,
      global::Afrowave.AJIS.Core.Configuration.AjisSettings settings,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      ArgumentNullException.ThrowIfNull(settings);
      AjisStreamWalkOptions options = AjisStreamWalkOptions.FromSettings(settings);
      Run(inputUtf8, options, visitor, settings, runnerOptions);
   }
   /// <summary>
   /// Runs StreamWalk over an in-memory UTF-8 payload.
   /// </summary>
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      ArgumentNullException.ThrowIfNull(visitor);

      // Debug marker: proves the diagnostic pipeline works without changing parsing behavior.
      // (Emitted only when EmitDebugDiagnostics == true AND OnDiagnostic is provided.)
      EmitDebug(
         runnerOptions,
         AjisDiagnosticCode.Unknown,
         offset: 0,
         data: (Stage: "streamwalk_run_start", Mode: options.Mode.ToString(), options.MaxDepth, options.MaxTokenBytes),
         message: "StreamWalk run started (debug marker)."
      );

      // Bridge: if caller provided a diagnostic sink, wrap the visitor so we can
      // emit AjisDiagnostic alongside legacy error reporting.
      IAjisStreamWalkVisitor v = runnerOptions.OnDiagnostic is null
         ? visitor
         : new DiagnosticForwardingVisitor(visitor, runnerOptions);

      // Fast fail: LAX is not supported in M1.
      if(options.Mode == AjisStreamWalkMode.Lax)
      {
         AjisDiagnostic diag = AjisDiagnostics.Create(
            AjisDiagnosticCode.ModeNotSupported,
            offset: 0,
            severity: AjisDiagnosticSeverity.Error,
            message: "LAX mode is not supported in milestone M1.");

         Emit(runnerOptions, diag);

         v.OnError(new AjisStreamWalkError(
            Code: AjisDiagnosticKeys.ModeNotSupported,
            Offset: 0,
            Line: null,
            Column: null));

         return;
      }

      // M1.1: select engine (pluggable implementations).
      // M1 = JSON-core subset used by current .case tests.
      IAjisStreamWalkEngine engine = AjisStreamWalkEngineSelector.Select(inputUtf8, options, runnerOptions);

      // IMPORTANT: Data must have a stable EngineId property (unit tests rely on it).
      EmitDebug(
   runnerOptions,
   AjisDiagnosticCode.EngineSelected,
   offset: 0,
   data: new AjisEngineSelectedData(
      EngineId: engine.EngineId,
      Preference: runnerOptions.EnginePreference.ToString(),
      LargePayloadThresholdBytes: runnerOptions.LargePayloadThresholdBytes),
   message: null);

      engine.Run(inputUtf8, options, v, runnerOptions);
   }

   public static void Run(
   IAjisInput input,
   AjisStreamWalkOptions options,
   IAjisStreamWalkVisitor visitor,
   AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      ArgumentNullException.ThrowIfNull(input);

      if(input.TryGetUtf8Span(out ReadOnlySpan<byte> span))
      {
         Run(span, options, visitor, runnerOptions);
         return;
      }

      // M1.3 contract exists, but engines for stream/file are future milestones.
      AjisDiagnostic diag = AjisDiagnostics.Create(
         AjisDiagnosticCode.InputNotSupported,
         offset: 0,
         severity: AjisDiagnosticSeverity.Error,
         message: "Input source does not provide contiguous UTF-8 span. Stream/file inputs are not supported yet.");

      if(runnerOptions.OnDiagnostic is not null)
         runnerOptions.OnDiagnostic(diag);

      visitor.OnError(new AjisStreamWalkError(
         Code: AjisDiagnosticKeys.InputNotSupported,
         Offset: 0,
         Line: null,
         Column: null));
   }

   /// <summary>
   /// Runs StreamWalk over an input source with core settings applied.
   /// </summary>
   public static void Run(
      IAjisInput input,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      global::Afrowave.AJIS.Core.Configuration.AjisSettings? settings,
      AjisStreamWalkRunnerOptions runnerOptions = default)
   {
      AjisStreamWalkRunnerOptions effective = settings is null
         ? runnerOptions
         : runnerOptions.WithProcessingProfileIfDefault(settings.ParserProfile);

      Run(input, options, visitor, effective);
   }

   private static void Emit(AjisStreamWalkRunnerOptions runnerOptions, AjisDiagnostic diag)
   {
      if(runnerOptions.OnDiagnostic is null) return;
      runnerOptions.OnDiagnostic(diag);
   }

   private sealed record EngineSelectedDebugData(
      string EngineId,
      string Preference,
      int LargePayloadThresholdBytes);

   /// <summary>
   /// Emits a Debug diagnostic only when enabled by runner options.
   /// </summary>
   /// <remarks>
   /// This method is used by tests to verify deterministic runner/engine selection.
   /// It MUST NOT affect parsing outcomes.
   /// </remarks>
   internal static void EmitDebug(
      AjisStreamWalkRunnerOptions runnerOptions,
      AjisDiagnosticCode code,
      long offset,
      object? data = null,
      string? message = null)
   {
      if(!runnerOptions.EmitDebugDiagnostics) return;
      if(runnerOptions.OnDiagnostic is null) return;

      Emit(
         runnerOptions,
         AjisDiagnostics.Create(
            code,
            offset: offset,
            severity: AjisDiagnosticSeverity.Debug,
            data: data,
            message: message));
   }

   private sealed class DiagnosticForwardingVisitor(IAjisStreamWalkVisitor inner, AjisStreamWalkRunnerOptions runnerOpt) : IAjisStreamWalkVisitor
   {
      private readonly IAjisStreamWalkVisitor _inner = inner;
      private readonly AjisStreamWalkRunnerOptions _runnerOpt = runnerOpt;

      public bool OnEvent(AjisStreamWalkEvent evt) => _inner.OnEvent(evt);

      public void OnError(AjisStreamWalkError error)
      {
         _inner.OnError(error);

         // NOTE: In M1 we only forward a generic diagnostic for visibility.
         // More detailed error mapping will be handled in later milestones.
         AjisDiagnostic diag = AjisDiagnostics.Create(
            AjisDiagnosticCode.Unknown,
            offset: error.Offset,
            severity: AjisDiagnosticSeverity.Error,
            message: $"StreamWalk error: {error.Code}");

         Emit(_runnerOpt, diag);
      }

      public void OnCompleted() => _inner.OnCompleted();
   }
}
