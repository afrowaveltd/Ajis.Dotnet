#nullable enable

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Segment parsing engine kind.
/// </summary>
public enum AjisSegmentParseEngineKind
{
   /// <summary>
   /// Lexer-based span parser.
   /// </summary>
   SpanLexer = 0,

   /// <summary>
   /// Lexer-based stream parser.
   /// </summary>
   StreamLexer = 1,

   /// <summary>
   /// Memory-mapped file parsing path.
   /// </summary>
   StreamMappedFile = 2
}
