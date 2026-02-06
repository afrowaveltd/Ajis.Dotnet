// File: tests/Afrowave.AJIS.Testing/StreamWalk/AjisStreamWalkTestRunner.cs
#nullable enable

using Afrowave.AJIS.Streaming.Walk;

namespace Afrowave.AJIS.Testing.StreamWalk;

/// <summary>
/// Executes StreamWalk test cases against the production StreamWalk implementation
/// and compares produced results with expectations from <c>.case</c> files.
/// </summary>
public static class AjisStreamWalkTestRunner
{
   public static AjisStreamWalkTestRunResult Run(
       AjisStreamWalkTestCase testCase,
       AjisStreamWalkRunnerOptions runnerOptions)
      => Run(testCase, settings: null, runnerOptions);

   public static AjisStreamWalkTestRunResult Run(
       AjisStreamWalkTestCase testCase,
       global::Afrowave.AJIS.Core.Configuration.AjisSettings? settings,
       AjisStreamWalkRunnerOptions runnerOptions)
   {
      ArgumentNullException.ThrowIfNull(testCase);

      List<string> mismatches = [];
      List<AjisStreamWalkEvent> producedEvents = [];
      AjisStreamWalkError? producedError = null;
      bool completed = false;

      // Map test options -> production options
      AjisStreamWalkOptions options = AjisStreamWalkOptions.DefaultForM1;

      if(testCase.Options.Mode is not null)
         options = options with
         {
            Mode = testCase.Options.Mode switch
            {
               AjisStreamWalkTestMode.JSON => AjisStreamWalkMode.Json,
               AjisStreamWalkTestMode.LAX => AjisStreamWalkMode.Lax,
               _ => AjisStreamWalkMode.Ajis
            }
         };


      if(testCase.Options.Comments is not null)
         options = options with { Comments = testCase.Options.Comments.Value };

      if(testCase.Options.Directives is not null)
         options = options with { Directives = testCase.Options.Directives.Value };

      if(testCase.Options.Identifiers is not null)
         options = options with { Identifiers = testCase.Options.Identifiers.Value };

      if(testCase.Options.MaxDepth is not null)
         options = options with { MaxDepth = testCase.Options.MaxDepth.Value };

      if(testCase.Options.MaxTokenBytes is not null)
         options = options with { MaxTokenBytes = testCase.Options.MaxTokenBytes.Value };

      CollectingVisitor visitor = new(
          onEvent: e => producedEvents.Add(e),
          onError: err => producedError = err,
          onCompleted: () => completed = true);

      AjisStreamWalkRunnerOptions effectiveRunnerOptions = settings is null
         ? runnerOptions
         : runnerOptions.WithProcessingProfileIfDefault(settings.ParserProfile);

      try
      {
         // Production runner consumes ReadOnlySpan<byte> (not ReadOnlyMemory<byte>).
         AjisStreamWalkRunner.Run(
            inputUtf8: testCase.InputUtf8.Span,
            options: options,
            visitor: visitor,
            runnerOptions: effectiveRunnerOptions);
      }
      catch(Exception ex)
      {
         mismatches.Add($"Unhandled exception: {ex.GetType().Name}: {ex.Message}");
      }

      CompareExpected(testCase, producedEvents, producedError, completed, runnerOptions, mismatches);

      return new AjisStreamWalkTestRunResult(
          CaseId: testCase.CaseId,
          Success: mismatches.Count == 0,
          Mismatches: mismatches);
   }

   private static void CompareExpected(
       AjisStreamWalkTestCase testCase,
       IReadOnlyList<AjisStreamWalkEvent> producedEvents,
       AjisStreamWalkError? producedError,
       bool completed,
       AjisStreamWalkRunnerOptions runnerOptions,
       List<string> mismatches)
   {
      AjisStreamWalkTestExpected expected = testCase.Expected;

      if(expected is AjisStreamWalkTestExpected.Failure fail)
      {
         if(producedError is null)
         {
            mismatches.Add("Expected an error, but no error was reported.");
            return;
         }

         if(!string.Equals(fail.Code, producedError.Code, StringComparison.Ordinal))
            mismatches.Add($"Error code mismatch. Expected '{fail.Code}', got '{producedError.Code}'.");

         if(fail.Offset != producedError.Offset)
            mismatches.Add($"Error offset mismatch. Expected {fail.Offset}, got {producedError.Offset}.");

         if(fail.Line is not null && fail.Line.Value != producedError.Line)
            mismatches.Add($"Error line mismatch. Expected {fail.Line}, got {producedError.Line}.");

         if(fail.Col is not null && fail.Col.Value != producedError.Column)
            mismatches.Add($"Error column mismatch. Expected {fail.Col}, got {producedError.Column}.");

         // Even on error, partial traces may exist (we intentionally do not compare them in M1).
         return;
      }

      if(expected is AjisStreamWalkTestExpected.Success ok)
      {
         if(producedError is not null)
            mismatches.Add($"Did not expect an error, but got '{producedError.Code}' at offset {producedError.Offset}.");

         if(!completed)
            mismatches.Add("Expected successful completion, but visitor OnCompleted() was not called.");

         CompareTrace(ok.Trace, producedEvents, runnerOptions, mismatches);
         return;
      }

      mismatches.Add("Unknown expected result type.");
   }

   private static void CompareTrace(
       IReadOnlyList<AjisStreamWalkTestTraceEvent> expected,
       IReadOnlyList<AjisStreamWalkEvent> produced,
       AjisStreamWalkRunnerOptions runnerOptions,
       List<string> mismatches)
   {
      if(expected.Count != produced.Count)
      {
         mismatches.Add($"Trace length mismatch. Expected {expected.Count}, got {produced.Count}.");
         return;
      }

      for(int i = 0; i < expected.Count; i++)
      {
         AjisStreamWalkTestTraceEvent exp = expected[i];
         AjisStreamWalkEvent act = produced[i];

         if(!string.Equals(exp.Kind, act.Kind, StringComparison.Ordinal))
         {
            mismatches.Add($"Trace[{i}] kind mismatch. Expected '{exp.Kind}', got '{act.Kind}'.");
            continue;
         }

         if(exp.Slice is not null)
         {
            string rendered = AjisStreamWalkTestCaseFile.RenderSlice(act.Slice.Span);
            if(!string.Equals(exp.Slice, rendered, StringComparison.Ordinal))
               mismatches.Add($"Trace[{i}] slice mismatch. Expected {exp.Slice}, got {rendered}.");
         }
      }
   }
}

/// <summary>
/// Result of executing a single StreamWalk test case.
/// </summary>
public sealed record AjisStreamWalkTestRunResult(
    string CaseId,
    bool Success,
    IReadOnlyList<string> Mismatches);

internal sealed class CollectingVisitor(
    Action<AjisStreamWalkEvent> onEvent,
    Action<AjisStreamWalkError> onError,
    Action onCompleted) : IAjisStreamWalkVisitor
{
   private readonly Action<AjisStreamWalkEvent> _onEvent = onEvent;
   private readonly Action<AjisStreamWalkError> _onError = onError;
   private readonly Action _onCompleted = onCompleted;

   public bool OnEvent(AjisStreamWalkEvent evt)
   {
      _onEvent(evt);
      return true;
   }

   public void OnError(AjisStreamWalkError error) => _onError(error);

   public void OnCompleted() => _onCompleted();
}
