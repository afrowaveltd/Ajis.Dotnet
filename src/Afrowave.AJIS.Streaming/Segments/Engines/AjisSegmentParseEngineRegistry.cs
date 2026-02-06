#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Registry of available segment parsing engines.
/// </summary>
public static class AjisSegmentParseEngineRegistry
{
   private static readonly AjisSegmentParseEngineDescriptor[] s_all =
   [
      new(
         AjisSegmentParseEngineIds.SpanLexer,
         AjisSegmentParseEngineKind.SpanLexer,
         AjisSegmentParseEngineCapabilities.HighThroughput,
         AjisProcessingProfile.Universal),
      new(
         AjisSegmentParseEngineIds.StreamLexer,
         AjisSegmentParseEngineKind.StreamLexer,
         AjisSegmentParseEngineCapabilities.Streaming,
         AjisProcessingProfile.Universal),
      new(
         AjisSegmentParseEngineIds.StreamMappedFile,
         AjisSegmentParseEngineKind.StreamMappedFile,
         AjisSegmentParseEngineCapabilities.Streaming | AjisSegmentParseEngineCapabilities.LowMemory,
         AjisProcessingProfile.LowMemory),
      new(
         AjisSegmentParseEngineIds.StreamMappedFile,
         AjisSegmentParseEngineKind.StreamMappedFile,
         AjisSegmentParseEngineCapabilities.Streaming | AjisSegmentParseEngineCapabilities.HighThroughput,
         AjisProcessingProfile.HighThroughput)
   ];

   /// <summary>
   /// Gets the available segment parsing engine descriptors.
   /// </summary>
   public static IReadOnlyList<AjisSegmentParseEngineDescriptor> All { get; } = s_all;
}
