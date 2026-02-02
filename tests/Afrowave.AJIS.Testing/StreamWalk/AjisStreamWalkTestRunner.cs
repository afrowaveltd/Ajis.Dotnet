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
   {
      ArgumentNullException.ThrowIfNull(testCase);

      var mismatches = new List<string>();
      var producedEvents = new List<AjisStreamWalkEvent>();
      AjisStreamWalkError? producedError = null;
      var completed = false;

      // Map test options -> production options
      var options = AjisStreamWalkOptions.DefaultForM1;

      if(testCase.Options.Mode is not null)
         options = options with
         {
            Mode = testCase.Options.Mode == AjisStreamWalkTestMode.JSON
             ? AjisStreamWalkMode.Json
             : AjisStreamWalkMode.Ajis
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

      var visitor = new CollectingVisitor(
          onEvent: e => producedEvents.Add(e),
          onError: err => producedError = err,
          onCompleted: () => completed = true);

      try
      {
         AjisStreamWalkRunner.Run(
             inputUtf8: testCase.InputUtf8,
             options: options,
             visitor: visitor,
             runnerOptions: runnerOptions);
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
      var expected = testCase.Expected;

      if(expected is AjisStreamWalkTestExpected.Failure fail)
      {
         if(producedError is null)
         {
            mismatches.Add("Expected an error, but no error was reported.");
         }
         else
         {
            if(!string.Equals(fail.ErrorCode, producedError.Code, StringComparison.Ordinal))
               mismatches.Add($"Error code mismatch. Expected '{fail.ErrorCode}', got '{producedError.Code}'.");

            if(fail.ErrorOffset != producedError.Offset)
               mismatches.Add($"Error offset mismatch. Expected {fail.ErrorOffset}, got {producedError.Offset}.");
         }

         // Even on error, partial traces may exist
         if(fail is AjisStreamWalkTestExpected.Failure failure)
         {
            // future: failure.Code, failure.Offset, ...
         }
      }
      else if(expected is AjisStreamWalkTestExpected.Success ok)
      {
         if(producedError is not null)
         {
            mismatches.Add($"Did not expect an error, but got '{producedError.Code}' at offset {producedError.Offset}.");
         }

         if(!completed)
         {
            mismatches.Add("Expected successful completion, but visitor OnCompleted() was not called.");
         }

         CompareTrace(ok.Trace, producedEvents, runnerOptions, mismatches);
      }
      else
      {
         mismatches.Add("Unknown expected result type.");
      }
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

      for(var i = 0; i < expected.Count; i++)
      {
         var exp = expected[i];
         var act = produced[i];

         if(!string.Equals(exp.Kind, act.Kind, StringComparison.Ordinal))
         {
            mismatches.Add($"Trace[{i}] kind mismatch. Expected '{exp.Kind}', got '{act.Kind}'.");
            continue;
         }

         if(exp.Slice is not null)
         {
            var rendered = AjisStreamWalkTestCaseFile.RenderSlice(act.Slice.Span);
            if(!string.Equals(exp.Slice, rendered, StringComparison.Ordinal))
            {
               mismatches.Add($"Trace[{i}] slice mismatch. Expected {exp.Slice}, got {rendered}.");
            }
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

   public void OnEvent(AjisStreamWalkEvent evt) => _onEvent(evt);

   public void OnError(AjisStreamWalkError error) => _onError(error);

   public void OnCompleted() => _onCompleted();
}