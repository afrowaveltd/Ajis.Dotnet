#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Text;

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisLexerParser
{
   private readonly AjisLexer _lexer;
   private readonly global::Afrowave.AJIS.Core.AjisStringOptions _stringOptions;
   private AjisToken _current;
   private readonly List<AjisSegment> _segments = [];
   private int _depth;
   private readonly bool _allowTrailingCommas;
   private readonly bool _emitDirectiveSegments;
   private readonly bool _emitCommentSegments;

   public AjisLexerParser(
      IAjisReader reader,
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
      _lexer = new AjisLexer(reader, numberOptions, stringOptions, commentOptions, textMode, allowDirectives, preserveStringEscapes, emitCommentSegments);
      _stringOptions = stringOptions ?? new global::Afrowave.AJIS.Core.AjisStringOptions();
      _current = _lexer.NextToken();
      _allowTrailingCommas = allowTrailingCommas || textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex;
      _emitDirectiveSegments = emitDirectiveSegments;
      _emitCommentSegments = emitCommentSegments;
   }

   public IReadOnlyList<AjisSegment> Parse()
   {
      EmitMetaTokens();
      ParseValue();
      EmitMetaTokens();
      Expect(AjisTokenKind.End);
      return _segments;
   }

   private void EmitMetaTokens()
   {
      while(_current.Kind is AjisTokenKind.Directive or AjisTokenKind.Comment)
      {
         if(_current.Kind == AjisTokenKind.Directive && _emitDirectiveSegments)
            _segments.Add(AjisSegment.Directive(_current.Offset, _depth, CreateSlice(_current.Text, GetStringFlags(_current.Text))));

         if(_current.Kind == AjisTokenKind.Comment && _emitCommentSegments)
            _segments.Add(AjisSegment.Comment(_current.Offset, _depth, CreateSlice(_current.Text, GetStringFlags(_current.Text))));

         Advance();
      }
   }

   private void ParseValue()
   {
      EmitMetaTokens();

      switch(_current.Kind)
      {
         case AjisTokenKind.LeftBrace:
            ParseObject();
            break;
         case AjisTokenKind.LeftBracket:
            ParseArray();
            break;
         case AjisTokenKind.String:
            EmitValue(AjisValueKind.String, CreateSlice(_current.Text, GetStringFlags(_current.Text)));
            Advance();
            break;
         case AjisTokenKind.Number:
            EmitValue(AjisValueKind.Number, CreateSlice(_current.Text, GetNumberFlags(_current.Text)));
            Advance();
            break;
         case AjisTokenKind.True:
            EmitValue(AjisValueKind.Boolean, CreateSlice("true", AjisSliceFlags.None));
            Advance();
            break;
         case AjisTokenKind.False:
            EmitValue(AjisValueKind.Boolean, CreateSlice("false", AjisSliceFlags.None));
            Advance();
            break;
         case AjisTokenKind.Null:
            EmitValue(AjisValueKind.Null, null);
            Advance();
            break;
         default:
            throw new FormatException($"Unexpected token '{_current.Kind}' at {_current.Line}:{_current.Column}.");
      }
   }

   private void ParseObject()
   {
      var start = _current;
      _segments.Add(AjisSegment.Enter(AjisContainerKind.Object, start.Offset, _depth));
      _depth++;
      Advance();

      if(_current.Kind == AjisTokenKind.RightBrace)
      {
         var end = _current;
         _depth--;
         _segments.Add(AjisSegment.Exit(AjisContainerKind.Object, end.Offset, _depth));
         Advance();
         return;
      }

      while(true)
      {
         EmitMetaTokens();
         if(_current.Kind != AjisTokenKind.String && _current.Kind != AjisTokenKind.Identifier)
            throw new FormatException($"Expected property name at {_current.Line}:{_current.Column}.");

         EnsurePropertyNameLimit(_current.Text, _current.Offset);
         var nameFlags = _current.Kind == AjisTokenKind.Identifier
            ? AjisSliceFlags.IsIdentifierStyle | GetStringFlags(_current.Text)
            : GetStringFlags(_current.Text);
         _segments.Add(AjisSegment.Name(_current.Offset, _depth, CreateSlice(_current.Text, nameFlags)));
         Advance();
         EmitMetaTokens();
         Expect(AjisTokenKind.Colon);
         ParseValue();

         EmitMetaTokens();
         if(_current.Kind == AjisTokenKind.Comma)
         {
            Advance();
            EmitMetaTokens();
            if(_current.Kind == AjisTokenKind.RightBrace)
            {
               if(!_allowTrailingCommas)
                  throw new FormatException($"Trailing commas are not allowed at {_current.Line}:{_current.Column}.");
            }
            else
            {
               continue;
            }
         }

         if(_current.Kind == AjisTokenKind.RightBrace)
         {
            var end = _current;
            _depth--;
            _segments.Add(AjisSegment.Exit(AjisContainerKind.Object, end.Offset, _depth));
            Advance();
            break;
         }

         throw new FormatException($"Expected ',' or '}}' at {_current.Line}:{_current.Column}.");
      }
   }

   private void ParseArray()
   {
      var start = _current;
      _segments.Add(AjisSegment.Enter(AjisContainerKind.Array, start.Offset, _depth));
      _depth++;
      Advance();

      if(_current.Kind == AjisTokenKind.RightBracket)
      {
         var end = _current;
         _depth--;
         _segments.Add(AjisSegment.Exit(AjisContainerKind.Array, end.Offset, _depth));
         Advance();
         return;
      }

      while(true)
      {
         EmitMetaTokens();
         ParseValue();

         EmitMetaTokens();
         if(_current.Kind == AjisTokenKind.Comma)
         {
            Advance();
            EmitMetaTokens();
            if(_current.Kind == AjisTokenKind.RightBracket)
            {
               if(!_allowTrailingCommas)
                  throw new FormatException($"Trailing commas are not allowed at {_current.Line}:{_current.Column}.");
            }
            else
            {
               continue;
            }
         }

         if(_current.Kind == AjisTokenKind.RightBracket)
         {
            var end = _current;
            _depth--;
            _segments.Add(AjisSegment.Exit(AjisContainerKind.Array, end.Offset, _depth));
            Advance();
            break;
         }

         throw new FormatException($"Expected ',' or ']' at {_current.Line}:{_current.Column}.");
      }
   }

   private void EmitValue(AjisValueKind kind, AjisSliceUtf8? slice)
      => _segments.Add(AjisSegment.Value(_current.Offset, _depth, kind, slice));

   private void EnsurePropertyNameLimit(string? text, long offset)
   {
      if(_stringOptions.MaxPropertyNameBytes is not int max || max <= 0)
         return;

      int byteCount = text is null ? 0 : Encoding.UTF8.GetByteCount(text);
      if(byteCount > max)
         throw new FormatException($"Property name exceeds maximum size at offset {offset}.");
   }

   private static AjisSliceFlags GetNumberFlags(string? text)
   {
      if(string.IsNullOrEmpty(text) || text.Length < 2)
         return AjisSliceFlags.None;

      return text[0] == '0' && (text[1] == 'x' || text[1] == 'X')
         ? AjisSliceFlags.IsNumberHex
         : text[0] == '0' && (text[1] == 'b' || text[1] == 'B')
            ? AjisSliceFlags.IsNumberBinary
            : text[0] == '0' && (text[1] == 'o' || text[1] == 'O')
               ? AjisSliceFlags.IsNumberOctal
               : AjisSliceFlags.None;
   }

   private static AjisSliceFlags GetStringFlags(string? text)
   {
      if(string.IsNullOrEmpty(text)) return AjisSliceFlags.None;

      AjisSliceFlags flags = AjisSliceFlags.None;
      foreach(char c in text)
      {
         if(c == '\\')
            flags |= AjisSliceFlags.HasEscapes;
         if(c > 0x7F)
            flags |= AjisSliceFlags.HasNonAscii;
      }

      return flags;
   }

   private static AjisSliceUtf8 CreateSlice(string? text, AjisSliceFlags flags)
      => new(text is null ? ReadOnlyMemory<byte>.Empty : Encoding.UTF8.GetBytes(text), flags);

   private void Expect(AjisTokenKind kind)
   {
      if(_current.Kind != kind)
         throw new FormatException($"Expected '{kind}' at {_current.Line}:{_current.Column}.");

      Advance();
   }

   private void Advance() => _current = _lexer.NextToken();
}
