#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisLexer(
   IAjisReader reader,
   global::Afrowave.AJIS.Core.AjisNumberOptions? numberOptions = null,
   global::Afrowave.AJIS.Core.AjisStringOptions? stringOptions = null,
   global::Afrowave.AJIS.Core.AjisCommentOptions? commentOptions = null,
   global::Afrowave.AJIS.Core.AjisTextMode textMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis,
   bool allowDirectives = true,
   bool preserveStringEscapes = false,
   bool emitCommentTokens = false)
{
   private readonly IAjisReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
   private readonly global::Afrowave.AJIS.Core.AjisNumberOptions _numberOptions = numberOptions ?? new global::Afrowave.AJIS.Core.AjisNumberOptions();
   private readonly global::Afrowave.AJIS.Core.AjisStringOptions _stringOptions = stringOptions ?? new global::Afrowave.AJIS.Core.AjisStringOptions();
   private readonly global::Afrowave.AJIS.Core.AjisTextMode _textMode = textMode;
   private readonly global::Afrowave.AJIS.Core.AjisCommentOptions _commentOptions = commentOptions ?? new global::Afrowave.AJIS.Core.AjisCommentOptions();
   private readonly bool _allowDirectives = allowDirectives && textMode != global::Afrowave.AJIS.Core.AjisTextMode.Json;
   private readonly bool _preserveStringEscapes = preserveStringEscapes;
   private readonly bool _emitCommentTokens = emitCommentTokens;

   public AjisToken NextToken()
   {
      SkipWhitespace();

      if(_reader.EndOfInput)
         return new AjisToken(AjisTokenKind.End, _reader.Offset, _reader.Line, _reader.Column, null);

      long offset = _reader.Offset;
      int line = _reader.Line;
      int column = _reader.Column;
      byte current = _reader.Peek();

      if(current == '#' && IsLineStart())
         return ReadDirectiveToken(offset, line, column);

      if(current == '/' && _emitCommentTokens)
         return ReadCommentToken(offset, line, column);

      if(IsIdentifierStart(current) && AllowUnquotedPropertyNames())
         return ReadIdentifierToken(offset, line, column);

      return current switch
      {
         (byte)'{' => ReadSingle(AjisTokenKind.LeftBrace, offset, line, column),
         (byte)'}' => ReadSingle(AjisTokenKind.RightBrace, offset, line, column),
         (byte)'[' => ReadSingle(AjisTokenKind.LeftBracket, offset, line, column),
         (byte)']' => ReadSingle(AjisTokenKind.RightBracket, offset, line, column),
         (byte)':' => ReadSingle(AjisTokenKind.Colon, offset, line, column),
         (byte)',' => ReadSingle(AjisTokenKind.Comma, offset, line, column),
         (byte)'"' => ReadStringToken(offset, line, column, '"'),
         (byte)'\'' => ReadSingleQuoteString(offset, line, column),
         (byte)'t' => ReadLiteral("true", AjisTokenKind.True, offset, line, column),
         (byte)'f' => ReadLiteral("false", AjisTokenKind.False, offset, line, column),
         (byte)'n' => ReadLiteral("null", AjisTokenKind.Null, offset, line, column),
         _ when IsNumberStart(current) => ReadNumberToken(offset, line, column),
         _ => throw new FormatException($"Unexpected character '{(char)current}' at offset {offset}.")
      };
   }

   private AjisToken ReadSingle(AjisTokenKind kind, long offset, int line, int column)
   {
      _reader.Read();
      return new AjisToken(kind, offset, line, column, null);
   }

   private AjisToken ReadLiteral(string literal, AjisTokenKind kind, long offset, int line, int column)
   {
      EnsureTokenLimit(literal.Length, offset);
      for(int i = 0; i < literal.Length; i++)
      {
         if(_reader.EndOfInput)
            throw new FormatException($"Unexpected end of input while reading '{literal}'.");

         byte b = _reader.Read();
         if(b != literal[i])
            throw new FormatException($"Invalid literal at offset {offset}.");
      }

      return new AjisToken(kind, offset, line, column, literal);
   }

   private AjisToken ReadSingleQuoteString(long offset, int line, int column)
   {
      if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json)
         throw new FormatException($"Single-quoted strings are not allowed at {line}:{column}.");

      if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_stringOptions.AllowSingleQuotes)
         throw new FormatException($"Single-quoted strings are not allowed at {line}:{column}.");

      return ReadStringToken(offset, line, column, '\'');
   }

   private AjisToken ReadIdentifierToken(long offset, int line, int column)
   {
      var buffer = new List<byte>();

      while(!_reader.EndOfInput)
      {
         byte b = _reader.Peek();
         if(IsIdentifierPart(b))
         {
            buffer.Add(_reader.Read());
            continue;
         }

         break;
      }

      if(buffer.Count == 0)
         throw new FormatException($"Invalid identifier at offset {offset}.");

      EnsureTokenLimit(buffer.Count, offset);

      string text = Encoding.UTF8.GetString([.. buffer]);
      // Normalize common JS literals that may appear as identifiers in Lax mode
      // e.g. 'true' or 'false' might be returned as identifier-like tokens depending on mode
      if(_textMode != global::Afrowave.AJIS.Core.AjisTextMode.Json && IsTypedLiteralText(text))
         return new AjisToken(AjisTokenKind.Number, offset, line, column, text);

      return text switch
      {
         "true" => new AjisToken(AjisTokenKind.True, offset, line, column, text),
         "false" => new AjisToken(AjisTokenKind.False, offset, line, column, text),
         "null" => new AjisToken(AjisTokenKind.Null, offset, line, column, text),
         "NaN" when _numberOptions.AllowNaNAndInfinity => new AjisToken(AjisTokenKind.Number, offset, line, column, text),
         "Infinity" when _numberOptions.AllowNaNAndInfinity => new AjisToken(AjisTokenKind.Number, offset, line, column, text),
         _ => new AjisToken(AjisTokenKind.Identifier, offset, line, column, text)
      };
   }

   private static bool IsTypedLiteralText(string text)
   {
      if(text.Length < 2)
         return false;

      int i = 0;
      while(i < text.Length && text[i] is >= 'A' and <= 'Z')
         i++;

      int prefixEnd = i;
      if(prefixEnd == 0 || prefixEnd == text.Length)
         return false;

      while(i < text.Length && text[i] is >= '0' and <= '9')
         i++;

      if(i == prefixEnd)
         return false;

      return i == text.Length;
   }

   private AjisToken ReadStringToken(long offset, int line, int column, char delimiter)
   {
      _reader.Read();
      var builder = new StringBuilder();
      int tokenBytes = 0;
      bool escapesEnabled = _textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json || _stringOptions.EnableEscapes;

      while(true)
      {
         if(_reader.EndOfInput)
         {
            if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
               return new AjisToken(AjisTokenKind.String, offset, line, column, builder.ToString());

            throw new FormatException("Unterminated string.");
         }

         byte b = _reader.Read();
         if(b == delimiter)
            break;

         tokenBytes++;
         EnsureStringLimits(tokenBytes, offset);

         if(IsControlByte(b))
         {
            if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json)
               throw new FormatException($"Invalid control character at {line}:{column}.");

            if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_stringOptions.AllowMultiline)
               throw new FormatException($"Invalid control character at {line}:{column}.");

            if(b == '\n' || b == '\r')
            {
               if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json)
                  throw new FormatException($"Invalid newline in string at {line}:{column}.");

               if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_stringOptions.AllowMultiline)
                  throw new FormatException($"Invalid newline in string at {line}:{column}.");
            }

            if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
            {
               builder.Append((char)b);
               continue;
            }
         }

         if(b == '\\')
         {
            if(!escapesEnabled)
            {
               builder.Append('\\');
               continue;
            }

            if(_reader.EndOfInput)
            {
               if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
                  return new AjisToken(AjisTokenKind.String, offset, line, column, builder.Append('\\').ToString());

               throw new FormatException("Unterminated escape sequence.");
            }

            byte esc = _reader.Read();
            tokenBytes++;
            EnsureStringLimits(tokenBytes, offset);
            if(_preserveStringEscapes)
            {
               if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
               {
                  builder.Append('\\');
                  builder.Append((char)esc);
                  continue;
               }

               if(esc == (byte)'u')
               {
                  builder.Append("\\u");
                  builder.Append(ReadUnicodeEscapeRaw(offset));
                  continue;
               }

               if(IsSimpleEscape(esc))
               {
                  builder.Append('\\');
                  builder.Append((char)esc);
                  continue;
               }

               throw new FormatException($"Invalid escape sequence at offset {offset}.");
            }

            builder.Append(esc switch
            {
               (byte)'"' => '"',
               (byte)'\'' => '\'',
               (byte)'\\' => '\\',
               (byte)'/' => '/',
               (byte)'b' => '\b',
               (byte)'f' => '\f',
               (byte)'n' => '\n',
               (byte)'r' => '\r',
               (byte)'t' => '\t',
               (byte)'u' => ReadUnicodeEscape(offset),
               _ => _textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex
                  ? (char)esc
                  : throw new FormatException($"Invalid escape sequence at offset {offset}.")
            });

            continue;
         }

         builder.Append((char)b);
      }

      return new AjisToken(AjisTokenKind.String, offset, line, column, builder.ToString());
   }

   private string ReadUnicodeEscapeRaw(long offset)
   {
      Span<char> buffer = stackalloc char[4];
      for(int i = 0; i < 4; i++)
      {
         if(_reader.EndOfInput)
            throw new FormatException("Unterminated unicode escape sequence.");

         byte b = _reader.Read();
         bool isHex = b is >= (byte)'0' and <= (byte)'9'
            or >= (byte)'a' and <= (byte)'f'
            or >= (byte)'A' and <= (byte)'F';

         if(!isHex)
            throw new FormatException($"Invalid unicode escape sequence at offset {offset}.");

         buffer[i] = (char)b;
      }

      return buffer.ToString();
   }

   private static bool IsSimpleEscape(byte esc)
      => esc is (byte)'"' or (byte)'\'' or (byte)'\\' or (byte)'/'
         or (byte)'b' or (byte)'f' or (byte)'n' or (byte)'r' or (byte)'t';

   private AjisToken ReadNumberToken(long offset, int line, int column)
   {
      var signedPrefix = new List<byte>();
      if(_reader.Peek() == '-' || (_reader.Peek() == '+' && _numberOptions.AllowLeadingPlusOnNumbers))
         signedPrefix.Add(_reader.Read());

      if(_numberOptions.AllowNaNAndInfinity)
      {
         byte next = _reader.Peek();
         if(next == (byte)'N')
         {
            if(signedPrefix.Count > 0)
               throw new FormatException($"Invalid number at offset {offset}.");
            return ReadSpecialNumberToken(offset, line, column, signedPrefix, "NaN");
         }

         if(next == (byte)'I')
            return ReadSpecialNumberToken(offset, line, column, signedPrefix, "Infinity");
      }

      if(_numberOptions.EnableBasePrefixes && TryReadPrefixedNumber(offset, line, column, signedPrefix, out AjisToken prefixed))
         return prefixed;

      var buffer = new List<byte>();
      if(signedPrefix.Count > 0)
         buffer.AddRange(signedPrefix);
      bool hasDot = false;
      bool hasExp = false;
      bool hasDigits = false;

      if(_reader.EndOfInput)
         throw new FormatException($"Invalid number at offset {offset}.");

      string integerPart = ReadDecimalIntegerPart(offset);
      if(integerPart.Length == 0)
         throw new FormatException($"Invalid number at offset {offset}.");

      hasDigits = true;
      buffer.AddRange(Encoding.UTF8.GetBytes(integerPart));

      if(!_reader.EndOfInput && _reader.Peek() == '.')
      {
         hasDot = true;
         buffer.Add(_reader.Read());

         if(_reader.EndOfInput || !IsDigit(_reader.Peek()))
            throw new FormatException($"Invalid number at offset {offset}.");

         while(!_reader.EndOfInput && IsDigit(_reader.Peek()))
            buffer.Add(_reader.Read());
      }

      if(!_reader.EndOfInput && (_reader.Peek() == 'e' || _reader.Peek() == 'E'))
      {
         hasExp = true;
         buffer.Add(_reader.Read());

         if(!_reader.EndOfInput && (_reader.Peek() == '+' || _reader.Peek() == '-'))
            buffer.Add(_reader.Read());

         if(_reader.EndOfInput || !IsDigit(_reader.Peek()))
            throw new FormatException($"Invalid number at offset {offset}.");

         while(!_reader.EndOfInput && IsDigit(_reader.Peek()))
            buffer.Add(_reader.Read());
      }

      if(!hasDigits)
         throw new FormatException($"Invalid number at offset {offset}.");

      if(_numberOptions.EnableDigitSeparators && _numberOptions.EnforceSeparatorGroupingRules)
      {
         string integerOnly = integerPart;
         if(!ValidateGrouping(integerOnly, 3, allowHexGrouping: false))
            throw new FormatException($"Invalid digit grouping at offset {offset}.");
      }

      _ = hasDot;
      _ = hasExp;
      EnsureTokenLimit(buffer.Count, offset);
      return new AjisToken(AjisTokenKind.Number, offset, line, column, Encoding.UTF8.GetString([.. buffer]));
   }

   private bool TryReadPrefixedNumber(long offset, int line, int column, List<byte> signedPrefix, out AjisToken token)
   {
      token = default;
      if(_reader.Peek() != '0') return false;

      byte first = _reader.Read();
      if(_reader.EndOfInput)
         throw new FormatException($"Invalid number at offset {offset}.");

      byte prefix = _reader.Peek();
      int numberBase = prefix switch
      {
         (byte)'b' or (byte)'B' => 2,
         (byte)'o' or (byte)'O' => 8,
         (byte)'x' or (byte)'X' => 16,
         _ => 0
      };

      if(numberBase == 0)
      {
         if(IsDigit(prefix) || (_numberOptions.EnableDigitSeparators && prefix == '_'))
            throw new FormatException($"Invalid number at offset {offset}.");
         token = new AjisToken(AjisTokenKind.Number, offset, line, column, "0");
         return true;
      }

      _reader.Read();

      var buffer = new List<byte>();
      if(signedPrefix.Count > 0)
         buffer.AddRange(signedPrefix);
      buffer.Add(first);
      buffer.Add(prefix);
      bool hasDigit = false;

      while(!_reader.EndOfInput)
      {
         byte b = _reader.Peek();
         if(_numberOptions.EnableDigitSeparators && b == '_')
         {
            buffer.Add(_reader.Read());
            continue;
         }

         if(IsBaseDigit(b, numberBase))
         {
            hasDigit = true;
            buffer.Add(_reader.Read());
            continue;
         }

         break;
      }

      if(!hasDigit)
         throw new FormatException($"Invalid number at offset {offset}.");

      if(_numberOptions.EnableDigitSeparators && _numberOptions.EnforceSeparatorGroupingRules)
      {
         string rawDigits = Encoding.UTF8.GetString([.. buffer]);
         int start = signedPrefix.Count + 2;
         string digitsOnly = rawDigits[start..];
         if(!ValidateGrouping(digitsOnly, BaseGroupSize(numberBase), allowHexGrouping: numberBase == 16))
            throw new FormatException($"Invalid digit grouping at offset {offset}.");
      }

      EnsureTokenLimit(buffer.Count, offset);
      token = new AjisToken(AjisTokenKind.Number, offset, line, column, Encoding.UTF8.GetString([.. buffer]));
      return true;
   }

   private string ReadDecimalIntegerPart(long offset)
   {
      var buffer = new List<byte>();
      if(_reader.EndOfInput) return string.Empty;

      byte first = _reader.Peek();
      if(first == '0')
      {
         buffer.Add(_reader.Read());

         if(!_reader.EndOfInput)
         {
            byte next = _reader.Peek();
            if(IsDigit(next))
               throw new FormatException($"Invalid number at offset {offset}.");

            if(_numberOptions.EnableDigitSeparators && next == '_')
            {
               buffer.Add(_reader.Read());
               ReadDigitsWithSeparators(buffer, 10, offset);
            }
         }

         return Encoding.UTF8.GetString([.. buffer]);
      }

      if(!IsDigit(first))
         return string.Empty;

      ReadDigitsWithSeparators(buffer, 10, offset);
      return Encoding.UTF8.GetString([.. buffer]);
   }

   private void ReadDigitsWithSeparators(List<byte> buffer, int numberBase, long offset)
   {
      bool hasDigit = false;
      while(!_reader.EndOfInput)
      {
         byte b = _reader.Peek();
         if(_numberOptions.EnableDigitSeparators && b == '_')
         {
            buffer.Add(_reader.Read());
            continue;
         }

         if(IsBaseDigit(b, numberBase))
         {
            hasDigit = true;
            buffer.Add(_reader.Read());
            continue;
         }

         break;
      }

      if(!hasDigit)
         throw new FormatException($"Invalid number at offset {offset}.");
   }

   private static int BaseGroupSize(int numberBase)
      => numberBase switch
      {
         2 => 4,
         8 => 3,
         16 => 4,
         _ => 3
      };

   private static bool ValidateGrouping(string digits, int groupSize, bool allowHexGrouping)
   {
      if(!digits.Contains('_')) return true;
      if(digits.StartsWith('_') || digits.EndsWith('_') || digits.Contains("__")) return false;

      string[] groups = digits.Split('_');
      if(groups.Length == 0) return false;

      if(allowHexGrouping)
      {
         int size = groups[0].Length;
         if(size is not (2 or 4)) return false;
         for(int i = 1; i < groups.Length; i++)
         {
            if(groups[i].Length != size)
               return false;
         }

         return true;
      }

      if(groups[0].Length > groupSize) return false;
      for(int i = 1; i < groups.Length; i++)
      {
         if(groups[i].Length != groupSize)
            return false;
      }

      return true;
   }

   private char ReadUnicodeEscape(long offset)
   {
      if(_textMode != global::Afrowave.AJIS.Core.AjisTextMode.Json && !_stringOptions.EnableEscapes)
         return 'u';

      int value = 0;
      for(int i = 0; i < 4; i++)
      {
         if(_reader.EndOfInput)
            throw new FormatException("Unterminated unicode escape sequence.");

         byte b = _reader.Read();
         int hex = b switch
         {
            >= (byte)'0' and <= (byte)'9' => b - '0',
            >= (byte)'a' and <= (byte)'f' => 10 + b - 'a',
            >= (byte)'A' and <= (byte)'F' => 10 + b - 'A',
            _ => -1
         };

         if(hex < 0)
         {
            if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
               return 'u';

            throw new FormatException($"Invalid unicode escape sequence at offset {offset}.");
         }

         value = (value << 4) + hex;
      }

      return (char)value;
   }

   private void SkipWhitespace()
   {
      while(!_reader.EndOfInput)
      {
         byte b = _reader.Peek();
         if(b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
         {
            _reader.Read();
            continue;
         }

         if(b == '#' && IsLineStart())
            return;

         if(b == '/')
         {
            if(_emitCommentTokens)
               return;

            if(TryConsumeComment())
               continue;
         }
         break;
      }
   }

   private AjisToken ReadDirectiveToken(long offset, int line, int column)
   {
      if(!_allowDirectives)
         throw new FormatException($"Directives are not allowed at {line}:{column}.");

      _reader.Read();
      var buffer = new List<byte>();
      while(!_reader.EndOfInput)
      {
         byte b = _reader.Peek();
         if(b == '\n')
            break;

         buffer.Add(_reader.Read());
      }

      EnsureTokenLimit(buffer.Count, offset);
      string text = Encoding.UTF8.GetString([.. buffer]).Trim();
      return new AjisToken(AjisTokenKind.Directive, offset, line, column, text);
   }

   private bool IsLineStart()
      => _reader.Column == 1 || _reader.Column == 0;

   private bool TryConsumeComment()
   {
      if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json)
         throw new FormatException($"Comments are not allowed at {_reader.Line}:{_reader.Column}.");

      byte slash = _reader.Read();
      if(_reader.EndOfInput)
         throw new FormatException($"Invalid comment start at {_reader.Line}:{_reader.Column}.");

      byte next = _reader.Peek();
      if(next == '/')
      {
         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_commentOptions.AllowLineComments)
            throw new FormatException($"Line comments are not allowed at {_reader.Line}:{_reader.Column}.");

         _reader.Read();
         while(!_reader.EndOfInput)
         {
            byte b = _reader.Read();
            if(b == '\n')
               break;
         }

         return true;
      }

      if(next == '*')
      {
         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_commentOptions.AllowBlockComments)
            throw new FormatException($"Block comments are not allowed at {_reader.Line}:{_reader.Column}.");

         _reader.Read();
         byte prev = 0;
         while(!_reader.EndOfInput)
         {
            byte b = _reader.Read();
            if(_commentOptions.RejectNestedBlockComments && prev == '/' && b == '*')
               throw new FormatException($"Nested block comments are not allowed at {_reader.Line}:{_reader.Column}.");

            if(prev == '*' && b == '/')
               return true;

            prev = b;
         }

         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
            return true;

         throw new FormatException("Unterminated block comment.");
      }

      throw new FormatException($"Invalid comment start at {_reader.Line}:{_reader.Column}.");
   }

   private AjisToken ReadCommentToken(long offset, int line, int column)
   {
      if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Json)
         throw new FormatException($"Comments are not allowed at {line}:{column}.");

      _reader.Read();
      if(_reader.EndOfInput)
         throw new FormatException($"Invalid comment start at {line}:{column}.");

      byte next = _reader.Peek();
      if(next == '/')
      {
         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_commentOptions.AllowLineComments)
            throw new FormatException($"Line comments are not allowed at {line}:{column}.");

         _reader.Read();
         var buffer = new List<byte>();
         while(!_reader.EndOfInput)
         {
            byte b = _reader.Read();
            if(b == '\n')
               break;

            buffer.Add(b);
         }

         EnsureTokenLimit(buffer.Count, offset);
         string text = Encoding.UTF8.GetString([.. buffer]).TrimEnd();
         return new AjisToken(AjisTokenKind.Comment, offset, line, column, text);
      }

      if(next == '*')
      {
         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && !_commentOptions.AllowBlockComments)
            throw new FormatException($"Block comments are not allowed at {line}:{column}.");

         _reader.Read();
         var buffer = new List<byte>();
         byte prev = 0;
         while(!_reader.EndOfInput)
         {
            byte b = _reader.Read();
            if(_commentOptions.RejectNestedBlockComments && prev == '/' && b == '*')
               throw new FormatException($"Nested block comments are not allowed at {_reader.Line}:{_reader.Column}.");

            if(prev == '*' && b == '/')
            {
               if(buffer.Count > 0)
                  buffer.RemoveAt(buffer.Count - 1);
               EnsureTokenLimit(buffer.Count, offset);
               string text = Encoding.UTF8.GetString([.. buffer]).TrimEnd();
               return new AjisToken(AjisTokenKind.Comment, offset, line, column, text);
            }

            buffer.Add(b);
            prev = b;
         }

         if(_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex)
         {
            EnsureTokenLimit(buffer.Count, offset);
            string text = Encoding.UTF8.GetString([.. buffer]).TrimEnd();
            return new AjisToken(AjisTokenKind.Comment, offset, line, column, text);
         }

         throw new FormatException("Unterminated block comment.");
      }

      throw new FormatException($"Invalid comment start at {line}:{column}.");
   }

   private bool IsNumberStart(byte b)
      => b == '-'
         || (b == '+' && _numberOptions.AllowLeadingPlusOnNumbers)
         || IsDigit(b)
         || (_numberOptions.AllowNaNAndInfinity && (b == (byte)'N' || b == (byte)'I'));

   private static bool IsDigit(byte b)
      => b >= '0' && b <= '9';

   private static bool IsBaseDigit(byte b, int numberBase)
      => numberBase switch
      {
         2 => b is (byte)'0' or (byte)'1',
         8 => b >= '0' && b <= '7',
         16 => IsDigit(b) || (b >= 'a' && b <= 'f') || (b >= 'A' && b <= 'F'),
         _ => IsDigit(b)
      };

   private static bool IsControlByte(byte b)
      => b < 0x20;

   private void EnsureTokenLimit(int tokenBytes, long offset)
   {
      if(_numberOptions.MaxTokenBytes > 0 && tokenBytes > _numberOptions.MaxTokenBytes)
         throw new FormatException($"Token exceeds maximum size at offset {offset}.");
   }

   private void EnsureStringLimits(int tokenBytes, long offset)
   {
      EnsureTokenLimit(tokenBytes, offset);
      if(_stringOptions.MaxStringBytes is int max && max > 0 && tokenBytes > max)
         throw new FormatException($"String exceeds maximum size at offset {offset}.");
   }

   private AjisToken ReadSpecialNumberToken(long offset, int line, int column, List<byte> signedPrefix, string literal)
   {
      for(int i = 0; i < literal.Length; i++)
      {
         if(_reader.EndOfInput)
            throw new FormatException($"Unexpected end of input while reading '{literal}'.");

         byte b = _reader.Read();
         if(b != literal[i])
            throw new FormatException($"Invalid number at offset {offset}.");
      }

      var buffer = new List<byte>(signedPrefix.Count + literal.Length);
      if(signedPrefix.Count > 0)
         buffer.AddRange(signedPrefix);
      buffer.AddRange(Encoding.UTF8.GetBytes(literal));
      EnsureTokenLimit(buffer.Count, offset);
      return new AjisToken(AjisTokenKind.Number, offset, line, column, Encoding.UTF8.GetString([.. buffer]));
   }

   private bool AllowUnquotedPropertyNames()
      => _textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lex
         || _textMode == global::Afrowave.AJIS.Core.AjisTextMode.Lax
         || (_textMode == global::Afrowave.AJIS.Core.AjisTextMode.Ajis && _stringOptions.AllowUnquotedPropertyNames);

   private static bool IsIdentifierStart(byte b)
      => b is >= (byte)'A' and <= (byte)'Z'
         or >= (byte)'a' and <= (byte)'z'
         or (byte)'$';

   private static bool IsIdentifierPart(byte b)
      => IsIdentifierStart(b) || (b >= '0' && b <= '9');
}
