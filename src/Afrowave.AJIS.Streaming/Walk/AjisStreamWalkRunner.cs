#nullable enable

using Afrowave.AJIS.Core.Diagnostics;

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// StreamWalk runner (public entry point).
/// </summary>
public static class AjisStreamWalkRunner
{
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
      // emit AjisDiagnostic alongside legacy AjisStreamWalkError string codes.
      IAjisStreamWalkVisitor v = runnerOptions.OnDiagnostic is null
         ? visitor
         : new DiagnosticForwardingVisitor(visitor, runnerOptions);

      // M1 currently supports JSON-core only.
      // Lax recovery will be implemented in later milestones.
      if(options.Mode == AjisStreamWalkMode.Lax)
      {
         Emit(
            runnerOptions,
            AjisDiagnostics.Create(
               AjisDiagnosticCode.ModeNotSupported,
               offset: 0,
               severity: AjisDiagnosticSeverity.Error,
               data: new { Mode = options.Mode.ToString() },
               message: "Lax mode is not supported by the M1 runner."));

         v.OnError(new AjisStreamWalkError(
            AjisDiagnosticKeys.ModeNotSupported,
            Offset: 0,
            Line: null,
            Column: null));
         return;
      }

      // M1 = JSON-core subset used by current .case tests.
      AjisStreamWalkRunnerM1.Run(inputUtf8, options, v, runnerOptions);

      EmitDebug(
         runnerOptions,
         AjisDiagnosticCode.Unknown,
         offset: inputUtf8.Length,
         data: new { Stage = "streamwalk_run_end" },
         message: "StreamWalk run finished (debug marker)."
      );
   }

   /// <summary>
   /// Convenience overload for callers holding memory.
   /// </summary>
   public static void Run(
      ReadOnlyMemory<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions = default)
      => Run(inputUtf8.Span, options, visitor, runnerOptions);

   /// <summary>
   /// Emits a diagnostic to the optional sink.
   /// </summary>
   private static void Emit(AjisStreamWalkRunnerOptions runnerOptions, AjisDiagnostic diag)
   {
      runnerOptions.OnDiagnostic?.Invoke(diag);
   }

   /// <summary>
   /// Emits a Debug-level diagnostic if (and only if) debug diagnostics are enabled.
   /// </summary>
   /// <remarks>
   /// This helper is intended for LAX recovery reporting and other non-fatal observability.
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

      public void OnCompleted() => _inner.OnCompleted();

      public void OnError(AjisStreamWalkError error)
      {
         // Map legacy string codes to the new stable enum.
         AjisDiagnosticCode code = Map(error.Code);

         // If we don't recognize the legacy code, still forward it as "Unknown" and keep the original string in Data.
         object? data = code == AjisDiagnosticCode.Unknown
            ? new { StreamWalkCode = error.Code }
            : null;

         Emit(
            _runnerOpt,
            AjisDiagnostics.Create(
               code,
               offset: error.Offset,
               severity: AjisDiagnosticSeverity.Error,
               line: error.Line,
               column: error.Column,
               data: data));

         _inner.OnError(error);
      }

      private static AjisDiagnosticCode Map(string code) => code switch
      {
         // Structure
         "unexpected_eof" => AjisDiagnosticCode.UnexpectedEof,
         "trailing_garbage" => AjisDiagnosticCode.TrailingGarbage,
         "max_depth_exceeded" => AjisDiagnosticCode.DepthLimit,

         // Objects/Arrays
         "expected_name" => AjisDiagnosticCode.ObjectExpectedKey,
         "expected_colon" => AjisDiagnosticCode.ObjectExpectedColon,

         // Strings
         "unterminated_string" => AjisDiagnosticCode.StringUnterminated,
         "invalid_string_control_char" => AjisDiagnosticCode.StringControlChar,

         // Numbers
         "invalid_number" => AjisDiagnosticCode.NumberInvalid,

         // Limits
         "max_token_bytes_exceeded" => AjisDiagnosticCode.TokenTooLarge,

         // Misc
         "invalid_value" => AjisDiagnosticCode.UnexpectedChar,
         "invalid_literal" => AjisDiagnosticCode.UnexpectedChar,
         "visitor_abort" => AjisDiagnosticCode.VisitorAbort,

         _ => AjisDiagnosticCode.Unknown,
      };
   }
}
