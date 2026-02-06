#nullable enable

using Afrowave.AJIS.Streaming.Segments;

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisLexerParser
{
   private readonly AjisLexer _lexer;
   private AjisToken _current;
   private readonly List<AjisSegment> _segments = [];
   private int _depth;
   private readonly bool _allowTrailingCommas;

   public AjisLexerParser(
      IAjisReader reader,
      global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
      global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
      global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
      global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
      bool allowTrailingCommas = false,
      bool allowDirectives = true)
   {
      _lexer = new AjisLexer(reader, numberOptions, stringOptions, commentOptions, textMode, allowDirectives);
      _current = _lexer.NextToken();
      _allowTrailingCommas = allowTrailingCommas || textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex;
   }

   public IReadOnlyList<AjisSegment> Parse()
   {
      SkipDirectivesBeforeRoot();
      ParseValue();
      SkipDirectivesAfterRoot();
      Expect(AjisTokenKind.End);
      return _segments;
   }

   private void SkipDirectivesBeforeRoot()
   {
      while(_current.Kind == AjisTokenKind.Directive)
         Advance();
   }

   private void SkipDirectivesAfterRoot()
   {
      while(_current.Kind == AjisTokenKind.Directive)
         Advance();
   }

   private void ParseValue()
   {
      while(_current.Kind == AjisTokenKind.Directive)
         Advance();

      switch(_current.Kind)
      {
         case AjisTokenKind.LeftBrace:
            ParseObject();
            break;
         case AjisTokenKind.LeftBracket:
            ParseArray();
            break;
         case AjisTokenKind.String:
            EmitValue(AjisValueKind.String, _current.Text);
            Advance();
            break;
         case AjisTokenKind.Number:
            EmitValue(AjisValueKind.Number, _current.Text);
            Advance();
            break;
         case AjisTokenKind.True:
            EmitValue(AjisValueKind.Boolean, "true");
            Advance();
            break;
         case AjisTokenKind.False:
            EmitValue(AjisValueKind.Boolean, "false");
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
         if(_current.Kind != AjisTokenKind.String && _current.Kind != AjisTokenKind.Identifier)
            throw new FormatException($"Expected property name at {_current.Line}:{_current.Column}.");

         _segments.Add(AjisSegment.Name(_current.Offset, _depth, _current.Text ?? string.Empty));
         Advance();
         Expect(AjisTokenKind.Colon);
         ParseValue();

         if(_current.Kind == AjisTokenKind.Comma)
         {
            Advance();
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
         ParseValue();

         if(_current.Kind == AjisTokenKind.Comma)
         {
            Advance();
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

   private void EmitValue(AjisValueKind kind, string? text)
      => _segments.Add(AjisSegment.Value(_current.Offset, _depth, kind, text));

   private void Expect(AjisTokenKind kind)
   {
      if(_current.Kind != kind)
         throw new FormatException($"Expected '{kind}' at {_current.Line}:{_current.Column}.");

      Advance();
   }

   private void Advance() => _current = _lexer.NextToken();
}
