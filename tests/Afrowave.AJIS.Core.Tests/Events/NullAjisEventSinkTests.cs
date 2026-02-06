#nullable enable

using CoreEvents = global::Afrowave.AJIS.Core.Events;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Events;

public sealed class NullAjisEventSinkTests
{
   [Fact]
   public async Task EmitAsync_CompletesImmediately()
   {
      var sink = CoreEvents.NullAjisEventSink.Instance;
      await sink.EmitAsync(new CoreEvents.AjisMilestoneEvent(DateTimeOffset.UtcNow, "m"));
   }

   [Fact]
   public void Instance_IsSingleton()
   {
      Assert.Same(CoreEvents.NullAjisEventSink.Instance, CoreEvents.NullAjisEventSink.Instance);
   }
}
