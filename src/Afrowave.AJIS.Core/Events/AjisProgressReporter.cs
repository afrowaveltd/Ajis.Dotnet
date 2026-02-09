#nullable enable

namespace Afrowave.AJIS.Core.Events;

/// <summary>
/// Emits progress events for a named operation.
/// </summary>
/// <param name="sink">The event sink to emit progress events to.</param>
/// <param name="operation">The operation name to include in progress events.</param>
public sealed class AjisProgressReporter(IAjisEventSink sink, string operation)
{
   private readonly IAjisEventSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
   private readonly string _operation = operation ?? throw new ArgumentNullException(nameof(operation));
   private int _lastPercent = -1;

   /// <summary>
   /// Reports progress as a percentage and optional byte counts.
   /// </summary>
   /// <param name="percent">Progress percentage from 0 to 100.</param>
   /// <param name="processedBytes">Processed byte count, when known.</param>
   /// <param name="totalBytes">Total byte count, when known.</param>
   /// <param name="ct">Cancellation token.</param>
   public ValueTask ReportAsync(int percent, long? processedBytes = null, long? totalBytes = null, CancellationToken ct = default)
   {
      if(percent < 0) percent = 0;
      if(percent > 100) percent = 100;

      if(percent == _lastPercent)
         return ValueTask.CompletedTask;

      _lastPercent = percent;

      return _sink.EmitAsync(
          new AjisProgressEvent(DateTimeOffset.UtcNow, _operation, percent, processedBytes, totalBytes),
          ct);
   }
}