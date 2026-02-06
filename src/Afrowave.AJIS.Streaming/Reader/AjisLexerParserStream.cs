#nullable enable

using Afrowave.AJIS.Streaming.Segments;

namespace Afrowave.AJIS.Streaming.Reader;

public static class AjisLexerParserStream
{
   public static IReadOnlyList<AjisSegment> Parse(
      Stream stream,
      int bufferSize = 4096,
      global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
      global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
      global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
      global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
      bool allowTrailingCommas = false,
      bool allowDirectives = true,
      bool preserveStringEscapes = false,
      bool emitDirectiveSegments = false,
      bool emitCommentSegments = false)
   {
      using var reader = new AjisStreamReader(stream, bufferSize);
      var parser = new AjisLexerParser(reader, numberOptions, stringOptions, commentOptions, textMode, allowTrailingCommas, allowDirectives, preserveStringEscapes, emitDirectiveSegments, emitCommentSegments);
      return parser.Parse();
   }
}
