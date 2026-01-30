#nullable enable

namespace Afrowave.AJIS.Core.Events;

public sealed class NullAjisEventSink : IAjisEventSink
{
   public static NullAjisEventSink Instance { get; } = new();

   private NullAjisEventSink()
   { }

   public ValueTask EmitAsync(AjisEvent evt, CancellationToken ct = default)
       => ValueTask.CompletedTask;
}