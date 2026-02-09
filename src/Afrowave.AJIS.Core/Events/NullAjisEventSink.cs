#nullable enable

namespace Afrowave.AJIS.Core.Events;

/// <summary>
/// No-op event sink implementation.
/// </summary>
public sealed class NullAjisEventSink : IAjisEventSink
{
   /// <summary>
   /// Shared singleton instance.
   /// </summary>
   public static NullAjisEventSink Instance { get; } = new();

   private NullAjisEventSink()
   { }

   /// <inheritdoc />
   public ValueTask EmitAsync(AjisEvent evt, CancellationToken ct = default)
       => ValueTask.CompletedTask;
}