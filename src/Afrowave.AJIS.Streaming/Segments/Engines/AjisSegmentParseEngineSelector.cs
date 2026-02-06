#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Selects a segment parsing engine based on profile and input type.
/// </summary>
public static class AjisSegmentParseEngineSelector
{
   /// <summary>
   /// Selects a segment parsing engine for the requested profile and input kind.
   /// </summary>
   public static AjisSegmentParseEngineDescriptor Select(
      AjisProcessingProfile profile,
      AjisSegmentParseInputKind inputKind)
   {
      foreach(AjisSegmentParseEngineDescriptor descriptor in AjisSegmentParseEngineRegistry.All)
      {
         if(descriptor.Kind.ToInputKind() != inputKind)
            continue;

         if(descriptor.Profile == profile)
            return descriptor;
      }

      foreach(AjisSegmentParseEngineDescriptor descriptor in AjisSegmentParseEngineRegistry.All)
      {
         if(descriptor.Kind.ToInputKind() == inputKind)
         {
            if(descriptor.Profile == AjisProcessingProfile.Universal)
               return descriptor;
         }
      }

      return AjisSegmentParseEngineRegistry.All[0];
   }

   private static AjisSegmentParseInputKind ToInputKind(this AjisSegmentParseEngineKind kind)
      => kind switch
      {
         AjisSegmentParseEngineKind.SpanLexer => AjisSegmentParseInputKind.Span,
         AjisSegmentParseEngineKind.StreamLexer => AjisSegmentParseInputKind.Stream,
         AjisSegmentParseEngineKind.StreamMappedFile => AjisSegmentParseInputKind.Stream,
         _ => AjisSegmentParseInputKind.Stream
      };
}

/// <summary>
/// Segment parse input kind.
/// </summary>
public enum AjisSegmentParseInputKind
{
   /// <summary>
   /// In-memory UTF-8 span input.
   /// </summary>
   Span = 0,

   /// <summary>
   /// Stream-based input.
   /// </summary>
   Stream = 1
}
