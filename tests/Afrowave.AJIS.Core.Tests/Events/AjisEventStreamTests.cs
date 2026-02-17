#nullable enable

using Afrowave.AJIS.Core.Events;
using CoreEvents = global::Afrowave.AJIS.Core.Events;

namespace Afrowave.AJIS.Core.Tests.Events;

public sealed class AjisEventStreamTests
{
   [Fact]
   public async Task EmitAsync_WritesToStream()
   {
      AjisEventStream stream = new CoreEvents.AjisEventStream();
      CoreEvents.AjisProgressEvent evt = new CoreEvents.AjisProgressEvent(DateTimeOffset.UtcNow, "op", 10, null, null);

      await stream.EmitAsync(evt, TestContext.Current.CancellationToken);
      stream.Complete();

      List<CoreEvents.AjisEvent> received = new List<CoreEvents.AjisEvent>();
      await foreach(var item in stream)
      {
         received.Add(item);
      }

      Assert.Single(received);
      Assert.Same(evt, received[0]);
   }

   [Fact]
   public async Task EmitAsync_ReturnsCanceled_WhenCancellationRequested()
   {
      AjisEventStream stream = new CoreEvents.AjisEventStream();
      using CancellationTokenSource cts = new CancellationTokenSource();
      cts.Cancel();

      var task = stream.EmitAsync(new CoreEvents.AjisMilestoneEvent(DateTimeOffset.UtcNow, "m"), cts.Token);

      await Assert.ThrowsAsync<TaskCanceledException>(async () => await task.AsTask());
   }
}