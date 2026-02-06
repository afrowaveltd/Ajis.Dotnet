#nullable enable

using CoreEvents = global::Afrowave.AJIS.Core.Events;
using NSubstitute;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Events;

public sealed class AjisProgressReporterTests
{
   [Fact]
   public async Task ReportAsync_ClampsPercent_AndEmitsOncePerValue()
   {
      var sink = Substitute.For<CoreEvents.IAjisEventSink>();
      sink.EmitAsync(Arg.Any<CoreEvents.AjisEvent>(), Arg.Any<CancellationToken>())
         .Returns(ValueTask.CompletedTask);

      var reporter = new CoreEvents.AjisProgressReporter(sink, "op");

      await reporter.ReportAsync(-5);
      await reporter.ReportAsync(0);
      await reporter.ReportAsync(150);

      await sink.Received(1).EmitAsync(
         Arg.Is<CoreEvents.AjisProgressEvent>(e => e.Percent == 0 && e.Operation == "op"),
         Arg.Any<CancellationToken>());

      await sink.Received(1).EmitAsync(
         Arg.Is<CoreEvents.AjisProgressEvent>(e => e.Percent == 100),
         Arg.Any<CancellationToken>());
   }
}
