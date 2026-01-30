#nullable enable

namespace Afrowave.AJIS.Core.Events;

/// <summary>
/// Optional event sink for progress/diagnostics and other runtime events.
/// Implementations can forward events to UI/CLI/loggers.
/// </summary>
public interface IAjisEventSink
{
   /// <summary>
   /// Emits an event. Implementations should be non-throwing and fast.
   /// </summary>
   ValueTask EmitAsync(AjisEvent evt, CancellationToken ct = default);
}