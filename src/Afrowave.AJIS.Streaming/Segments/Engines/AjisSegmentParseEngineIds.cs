#nullable enable

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Stable identifiers for segment parsing engines.
/// </summary>
public static class AjisSegmentParseEngineIds
{
   /// <summary>
   /// Lexer-based span parser.
   /// </summary>
   public const string SpanLexer = "SEG_SPAN_LEXER";

   /// <summary>
   /// Lexer-based stream parser.
   /// </summary>
   public const string StreamLexer = "SEG_STREAM_LEXER";

   /// <summary>
   /// Memory-mapped JSON reader path.
   /// </summary>
   public const string StreamMappedFile = "SEG_STREAM_MAPPED";
}
