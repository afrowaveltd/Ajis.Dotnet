#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Segments.Engines;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSegmentParseEngineSelectorTests
{
   [Fact]
   public void Select_StreamUniversal_ReturnsStreamLexer()
   {
      AjisSegmentParseEngineDescriptor selected = AjisSegmentParseEngineSelector.Select(
         AjisProcessingProfile.Universal,
         AjisSegmentParseInputKind.Stream);

      Assert.Equal(AjisSegmentParseEngineIds.StreamLexer, selected.EngineId);
   }

   [Fact]
   public void Select_StreamLowMemory_ReturnsMappedFile()
   {
      AjisSegmentParseEngineDescriptor selected = AjisSegmentParseEngineSelector.Select(
         AjisProcessingProfile.LowMemory,
         AjisSegmentParseInputKind.Stream);

      Assert.Equal(AjisSegmentParseEngineIds.StreamMappedFile, selected.EngineId);
   }

   [Fact]
   public void Select_SpanHighThroughput_ReturnsSpanLexer()
   {
      AjisSegmentParseEngineDescriptor selected = AjisSegmentParseEngineSelector.Select(
         AjisProcessingProfile.HighThroughput,
         AjisSegmentParseInputKind.Span);

      Assert.Equal(AjisSegmentParseEngineIds.SpanLexer, selected.EngineId);
   }
}
