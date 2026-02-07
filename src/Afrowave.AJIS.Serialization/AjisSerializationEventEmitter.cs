#nullable enable

namespace Afrowave.AJIS.Serialization;

internal static class AjisSerializationEventEmitter
{
   public static ValueTask EmitPhaseAsync(global::Afrowave.AJIS.Core.Events.IAjisEventSink sink, string phase, string detail, CancellationToken ct)
      => sink.EmitAsync(new global::Afrowave.AJIS.Core.Events.AjisMilestoneEvent(DateTimeOffset.UtcNow, phase, detail), ct);

   public static ValueTask EmitProgressAsync(global::Afrowave.AJIS.Core.Events.IAjisEventSink sink, string phase, int percent, CancellationToken ct)
      => sink.EmitAsync(new global::Afrowave.AJIS.Core.Events.AjisProgressEvent(DateTimeOffset.UtcNow, phase, percent, null, null), ct);
}
