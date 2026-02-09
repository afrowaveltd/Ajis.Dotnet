#nullable enable

using System.Threading.Channels;

namespace Afrowave.AJIS.Core.Events;

/// <summary>
/// Default event stream implementation based on <see cref="Channel{T}"/>.
/// Producers publish via <see cref="IAjisEventSink"/>,
/// consumers read via <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public sealed class AjisEventStream : IAjisEventSink, IAsyncEnumerable<AjisEvent>, IDisposable
{
   private readonly Channel<AjisEvent> _channel;
   private readonly ChannelWriter<AjisEvent> _writer;
   private readonly ChannelReader<AjisEvent> _reader;

   /// <summary>
   /// Creates an unbounded event stream.
   /// </summary>
   /// <param name="capacity">Optional bounded capacity; when set, oldest events are dropped on overflow.</param>
   public AjisEventStream(int? capacity = null)
   {
      _channel = capacity is null
         ? Channel.CreateUnbounded<AjisEvent>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false })
         : Channel.CreateBounded<AjisEvent>(new BoundedChannelOptions(capacity.Value)
         {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
         });

      _writer = _channel.Writer;
      _reader = _channel.Reader;
   }

   /// <summary>
   /// Completes the stream, stopping all readers.
   /// </summary>
   /// <param name="error">Optional error to propagate to readers.</param>
   public void Complete(Exception? error = null) => _writer.TryComplete(error);

   /// <inheritdoc />
   public ValueTask EmitAsync(AjisEvent evt, CancellationToken ct = default)
   {
      if(ct.IsCancellationRequested)
         return ValueTask.FromCanceled(ct);

      // Non-throwing best-effort: if channel is completed or full, we drop.
      return _writer.TryWrite(evt) ? ValueTask.CompletedTask : ValueTask.CompletedTask;
   }

   /// <inheritdoc />
   public IAsyncEnumerator<AjisEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
       => _reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

   /// <summary>
   /// Completes the stream and releases resources.
   /// </summary>
   public void Dispose() => Complete();
}