#nullable enable

using Afrowave.AJIS.Streaming;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
// Avoid name collision with Afrowave.AJIS.Streaming.StreamWalkCaseFile
using CaseFile = Afrowave.AJIS.Testing.StreamWalk.StreamWalkCaseFile;

namespace Afrowave.AJIS.Testing.StreamWalk;

/// <summary>
/// Runs a single StreamWalk .case file against an IStreamWalker implementation.
/// This is the test glue that compares produced events/errors to the expected trace.
/// </summary>
public static class StreamWalkRunner
{
   public static StreamWalkRunResult Run(
       string caseFilePath,
       IStreamWalker walker,
       StreamWalkRunnerOptions? runnerOptions = null)
   {
      if(string.IsNullOrWhiteSpace(caseFilePath))
         throw new ArgumentException("Case file path is required.", nameof(caseFilePath));
      if(walker is null) throw new ArgumentNullException(nameof(walker));

      runnerOptions ??= new StreamWalkRunnerOptions();

      var parsed = CaseFile.Load(caseFilePath);

      var producedEvents = new List<StreamWalkEvent>();
      StreamWalkError? producedError = null;
      var completed = false;

      var visitor = new CollectingVisitor(
          producedEvents,
          err => producedError = err,
          () => completed = true);

      // Provide input bytes
      var inputPath = ResolveInputPath(caseFilePath, parsed.InputPath);
      var inputBytes = File.ReadAllBytes(inputPath);

      // Use options from the case file
      var options = parsed.Options.Validate();

      // Run walker
      walker.Walk(inputBytes, options, visitor);

      // Validate outcome
      var expected = parsed.Expected;

      var mismatches = new List<string>();

      if(expected.ExpectError)
      {
         if(producedError is null)
         {
            mismatches.Add("Expected an error, but no error was reported.");
         }
         else
         {
            if(!string.IsNullOrWhiteSpace(expected.ErrorCode) &&
                !string.Equals(expected.ErrorCode, producedError.Code, StringComparison.Ordinal))
            {
               mismatches.Add($"Error code mismatch. Expected '{expected.ErrorCode}', got '{producedError.Code}'.");
            }

            if(expected.ErrorOffset.HasValue && expected.ErrorOffset.Value != producedError.Offset)
            {
               mismatches.Add($"Error offset mismatch. Expected {expected.ErrorOffset.Value}, got {producedError.Offset}.");
            }
         }

         // In error cases, we still allow partial traces.
         if(expected.Trace.Count > 0)
         {
            CompareTrace(expected.Trace, producedEvents, runnerOptions, mismatches);
         }
      }
      else
      {
         if(producedError is not null)
         {
            mismatches.Add($"Did not expect an error, but got '{producedError.Code}' at offset {producedError.Offset}.");
         }

         if(!completed)
         {
            mismatches.Add("Expected successful completion, but visitor OnCompleted() was not called.");
         }

         CompareTrace(expected.Trace, producedEvents, runnerOptions, mismatches);
      }

      return new StreamWalkRunResult(
          CaseFilePath: caseFilePath,
          InputPath: inputPath,
          ProducedEvents: producedEvents,
          ProducedError: producedError,
          Completed: completed,
          Success: mismatches.Count == 0,
          Mismatches: mismatches);
   }

   private static void CompareTrace(
       IReadOnlyList<StreamWalkExpectedTraceLine> expected,
       IReadOnlyList<StreamWalkEvent> produced,
       StreamWalkRunnerOptions runnerOptions,
       List<string> mismatches)
   {
      var expCount = expected.Count;
      var prodCount = produced.Count;

      if(runnerOptions.StrictEventCount && expCount != prodCount)
      {
         mismatches.Add($"Event count mismatch. Expected {expCount}, got {prodCount}.");
      }

      var n = Math.Min(expCount, prodCount);
      for(var i = 0; i < n; i++)
      {
         var e = expected[i];
         var p = produced[i];

         if(e.Kind != p.Kind)
         {
            mismatches.Add($"Event[{i}] kind mismatch. Expected '{e.Kind}', got '{p.Kind}'.");
            continue;
         }

         if(runnerOptions.CompareOffsets && e.Offset.HasValue && e.Offset.Value != p.Offset)
         {
            mismatches.Add($"Event[{i}] offset mismatch. Expected {e.Offset.Value}, got {p.Offset}.");
         }

         if(runnerOptions.CompareSlices)
         {
            // Expected slice is stored as UTF-8 bytes (already raw), compare as bytes.
            if(e.Utf8Slice is not null)
            {
               var expBytes = e.Utf8Slice.Value;
               var prodBytes = p.Utf8Slice.ToArray();

               if(!expBytes.SequenceEqual(prodBytes))
               {
                  var expPreview = Preview(expBytes, runnerOptions.SlicePreviewBytes);
                  var prodPreview = Preview(prodBytes, runnerOptions.SlicePreviewBytes);
                  mismatches.Add($"Event[{i}] slice mismatch. Expected {expPreview}, got {prodPreview}.");
               }
            }
         }
      }

      if(!runnerOptions.StrictEventCount)
      {
         if(expCount > prodCount)
            mismatches.Add($"Missing {expCount - prodCount} expected events (produced fewer than expected).");
         else if(prodCount > expCount)
            mismatches.Add($"Produced {prodCount - expCount} extra events (produced more than expected).");
      }
   }

   private static string ResolveInputPath(string caseFilePath, string? inputPath)
   {
      if(string.IsNullOrWhiteSpace(inputPath))
         throw new InvalidOperationException("Case file does not specify inputPath.");

      // If already absolute, use it. Otherwise resolve relative to the .case file directory.
      if(Path.IsPathRooted(inputPath)) return inputPath;

      var dir = Path.GetDirectoryName(caseFilePath) ?? ".";
      return Path.GetFullPath(Path.Combine(dir, inputPath));
   }

   private static string Preview(byte[] bytes, int max)
   {
      if(bytes.Length == 0) return "<empty>";
      var take = Math.Min(max, bytes.Length);
      var sb = new StringBuilder();
      sb.Append("0x");
      for(var i = 0; i < take; i++) sb.Append(bytes[i].ToString("X2"));
      if(take < bytes.Length) sb.Append("…");
      return sb.ToString();
   }

   private sealed class CollectingVisitor : IStreamWalkVisitor
   {
      private readonly List<StreamWalkEvent> _events;
      private readonly Action<StreamWalkError> _onError;
      private readonly Action _onCompleted;

      public CollectingVisitor(List<StreamWalkEvent> events, Action<StreamWalkError> onError, Action onCompleted)
      {
         _events = events;
         _onError = onError;
         _onCompleted = onCompleted;
      }

      public bool OnEvent(in StreamWalkEvent evt)
      {
         _events.Add(evt);
         return true;
      }

      public void OnCompleted() => _onCompleted();

      public void OnError(StreamWalkError error) => _onError(error);
   }
}

/// <summary>
/// Runner behavior tuning. Defaults match "strict" test expectations.
/// </summary>
public sealed class StreamWalkRunnerOptions
{
   public bool StrictEventCount { get; set; } = true;
   public bool CompareOffsets { get; set; } = true;
   public bool CompareSlices { get; set; } = true;
   public int SlicePreviewBytes { get; set; } = 64;
}

/// <summary>
/// Result of a single .case execution.
/// </summary>
public sealed record StreamWalkRunResult(
    string CaseFilePath,
    string InputPath,
    IReadOnlyList<StreamWalkEvent> ProducedEvents,
    StreamWalkError? ProducedError,
    bool Completed,
    bool Success,
    IReadOnlyList<string> Mismatches
);

/// <summary>
/// Minimal abstraction for the concrete stream walker implementation.
/// The core library will implement this.
/// </summary>
public interface IStreamWalker
{
   void Walk(ReadOnlySpan<byte> utf8, StreamWalkOptions options, IStreamWalkVisitor visitor);
}
