#nullable enable

namespace Afrowave.AJIS.Core.Events;

public sealed class AjisProgressReporter(IAjisEventSink sink, string operation)
{
   private readonly IAjisEventSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
   private readonly string _operation = operation ?? throw new ArgumentNullException(nameof(operation));
   private int _lastPercent = -1;

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