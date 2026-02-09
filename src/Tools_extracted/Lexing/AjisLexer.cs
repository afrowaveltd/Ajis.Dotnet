using System;
using System.Runtime.CompilerServices;

namespace Afrowave.AJIS;

/// <summary>
/// High-performance lexer for AJIS/JSON using Span<char> for zero allocations.
/// </summary>
/// <remarks>
/// This is a ref struct to ensure stack-only allocation and prevent heap allocations.
/// Use foreach or while loop to iterate through tokens.
/// </remarks>
public ref struct AjisLexer
{
    private readonly ReadOnlySpan<char> _input;
    private readonly AjisLexerOptions _options;
    private int _position;
    private int _line;
    private int _column;

    /// <summary>
    /// Initializes a new lexer with the given input and options.
    /// </summary>
    /// <param name="input">The input text to tokenize.</param>
    /// <param name="options">Lexer options (null for JSON-compatible mode).</param>
    public AjisLexer(ReadOnlySpan<char> input, AjisLexerOptions? options = null)
    {
        _input = input;
        _options = options ?? AjisLexerOptions.Json;
        _position = 0;
        _line = 1;
        _column = 1;
    }

    /// <summary>
    /// Returns the next token from the input.
    /// </summary>
    /// <returns>The next token, or EOF token if at end of input.</returns>
    /// <exception cref="AjisLexerException">Thrown when invalid syntax is encountered.</exception>
    public AjisToken NextToken()
    {
        // Skip whitespace and comments
        SkipIgnored();

        if (IsAtEnd())
        {
            return CreateToken(AjisTokenType.Eof, 0);
        }

        var location = CurrentLocation();
        char c = Peek();

        return c switch
        {
            '{' => ConsumeSimple(AjisTokenType.LeftBrace),
            '}' => ConsumeSimple(AjisTokenType.RightBrace),
            '[' => ConsumeSimple(AjisTokenType.LeftBracket),
            ']' => ConsumeSimple(AjisTokenType.RightBracket),
            ':' => ConsumeSimple(AjisTokenType.Colon),
            ',' => ConsumeSimple(AjisTokenType.Comma),
            '"' => LexString(),
            't' => LexKeyword("true", AjisTokenType.True),
            'f' => LexKeyword("false", AjisTokenType.False),
            'n' => LexKeyword("null", AjisTokenType.Null),
            'b' => LexBinaryOrBase64(),
            'h' => LexHexBinary(),
            '-' or >= '0' and <= '9' => LexNumber(),
            _ => throw new AjisLexerException($"Unexpected character '{c}'", location)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AjisToken ConsumeSimple(AjisTokenType type)
    {
        var token = CreateToken(type, 1);
        Advance();
        return token;
    }

    private void SkipIgnored()
    {
        while (!IsAtEnd())
        {
            char c = Peek();

            // Whitespace
            if (IsWhitespace(c))
            {
                Advance();
                continue;
            }

            // Comments (if enabled)
            if (_options.AllowComments && c == '/')
            {
                if (PeekAhead(1) == '/')
                {
                    SkipLineComment();
                    continue;
                }
                if (PeekAhead(1) == '*')
                {
                    SkipBlockComment();
                    continue;
                }
            }

            break;
        }
    }

    private void SkipLineComment()
    {
        // Skip //
        Advance();
        Advance();

        while (!IsAtEnd() && Peek() != '\n')
        {
            Advance();
        }
    }

    private void SkipBlockComment()
    {
        var location = CurrentLocation();

        // Skip /*
        Advance();
        Advance();

        while (!IsAtEnd())
        {
            if (Peek() == '*' && PeekAhead(1) == '/')
            {
                Advance(); // *
                Advance(); // /
                return;
            }
            Advance();
        }

        throw new AjisLexerException("Unterminated block comment", location);
    }

    private AjisToken LexString()
    {
        var start = _position;
        var location = CurrentLocation();

        Advance(); // Opening "

        while (!IsAtEnd())
        {
            var span = _input.Slice(_position);
            var nextSpecial = span.IndexOfAny('"', '\\', '\n');

            if (nextSpecial < 0)
            {
                break;
            }

            if (nextSpecial > 0)
            {
                _position += nextSpecial;
                _column += nextSpecial;
            }

            char c = Peek();

            if (c == '"')
            {
                Advance(); // Closing "
                var length = _position - start;
                return new AjisToken(AjisTokenType.String, _input.Slice(start, length), location, start, length);
            }

            if (c == '\\')
            {
                Advance(); // \
                if (IsAtEnd())
                {
                    throw new AjisLexerException("Unexpected end of file in string", CurrentLocation());
                }
                Advance(); // Escape sequence
            }
            else if (c == '\n')
            {
                if (!_options.AllowMultilineStrings)
                {
                    throw new AjisLexerException("Newline in string (use AllowMultilineStrings option)", location);
                }
                Advance();
            }
        }

        throw new AjisLexerException("Unterminated string", location);
    }

    private AjisToken LexNumber()
    {
        var start = _position;
        var location = CurrentLocation();

        // Check for hex (0x), binary (0b), octal (0o)
        if (Peek() == '0' && !IsAtEnd())
        {
            char next = PeekAhead(1);

            if ((next == 'x' || next == 'X') && _options.AllowHexLiterals)
            {
                return LexHexNumber(start, location);
            }
            if ((next == 'b' || next == 'B') && _options.AllowBinaryLiterals)
            {
                return LexBinaryNumber(start, location);
            }
            if ((next == 'o' || next == 'O') && _options.AllowOctalLiterals)
            {
                return LexOctalNumber(start, location);
            }
        }

        // Standard decimal number
        if (Peek() == '-')
        {
            Advance();
        }

        // Integer part
        if (!IsDigit(Peek()))
        {
            throw new AjisLexerException("Expected digit after '-'", CurrentLocation());
        }

        // Handle leading zero
        if (Peek() == '0')
        {
            Advance();
            // JSON doesn't allow leading zeros like 007
            if (!IsAtEnd() && IsDigit(Peek()))
            {
                throw new AjisLexerException("Leading zeros not allowed", CurrentLocation());
            }
        }
        else
        {
            if (!_options.AllowNumberSeparators)
            {
                // Fast path: digits only
                while (!IsAtEnd() && IsDigit(Peek()))
                {
                    Advance();
                }
            }
            else
            {
                // Consume digits (with optional separators)
                while (!IsAtEnd())
                {
                    char c = Peek();
                    if (IsDigit(c))
                    {
                        Advance();
                    }
                    else if (IsNumberSeparator(c))
                    {
                        Advance();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Fractional part
        if (!IsAtEnd() && Peek() == '.')
        {
            Advance();
            if (IsAtEnd() || !IsDigit(Peek()))
            {
                throw new AjisLexerException("Expected digit after '.'", CurrentLocation());
            }

            while (!IsAtEnd() && IsDigit(Peek()))
            {
                Advance();
            }
        }

        // Exponent part
        if (!IsAtEnd() && (Peek() == 'e' || Peek() == 'E'))
        {
            Advance();

            if (!IsAtEnd() && (Peek() == '+' || Peek() == '-'))
            {
                Advance();
            }

            if (IsAtEnd() || !IsDigit(Peek()))
            {
                throw new AjisLexerException("Expected digit in exponent", CurrentLocation());
            }

            while (!IsAtEnd() && IsDigit(Peek()))
            {
                Advance();
            }
        }

        var length = _position - start;
        return new AjisToken(AjisTokenType.Number, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexHexNumber(int start, AjisLocation location)
    {
        Advance(); // 0
        Advance(); // x or X

        if (IsAtEnd() || !IsHexDigit(Peek()))
        {
            throw new AjisLexerException("Expected hex digit after '0x'", CurrentLocation());
        }

        if (!_options.AllowNumberSeparators)
        {
            while (!IsAtEnd() && IsHexDigit(Peek()))
            {
                Advance();
            }
        }
        else
        {
            while (!IsAtEnd())
            {
                char c = Peek();
                if (IsHexDigit(c))
                {
                    Advance();
                }
                else if (IsNumberSeparator(c))
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        var length = _position - start;
        return new AjisToken(AjisTokenType.Number, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexBinaryNumber(int start, AjisLocation location)
    {
        Advance(); // 0
        Advance(); // b or B

        if (IsAtEnd() || !IsBinaryDigit(Peek()))
        {
            throw new AjisLexerException("Expected binary digit after '0b'", CurrentLocation());
        }

        if (!_options.AllowNumberSeparators)
        {
            while (!IsAtEnd() && IsBinaryDigit(Peek()))
            {
                Advance();
            }
        }
        else
        {
            while (!IsAtEnd())
            {
                char c = Peek();
                if (IsBinaryDigit(c))
                {
                    Advance();
                }
                else if (IsNumberSeparator(c))
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        var length = _position - start;
        return new AjisToken(AjisTokenType.Number, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexOctalNumber(int start, AjisLocation location)
    {
        Advance(); // 0
        Advance(); // o or O

        if (IsAtEnd() || !IsOctalDigit(Peek()))
        {
            throw new AjisLexerException("Expected octal digit after '0o'", CurrentLocation());
        }

        if (!_options.AllowNumberSeparators)
        {
            while (!IsAtEnd() && IsOctalDigit(Peek()))
            {
                Advance();
            }
        }
        else
        {
            while (!IsAtEnd())
            {
                char c = Peek();
                if (IsOctalDigit(c))
                {
                    Advance();
                }
                else if (IsNumberSeparator(c))
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }
        }

        var length = _position - start;
        return new AjisToken(AjisTokenType.Number, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexKeyword(string keyword, AjisTokenType type)
    {
        var start = _position;
        var location = CurrentLocation();

        foreach (char expected in keyword)
        {
            if (IsAtEnd() || Peek() != expected)
            {
                throw new AjisLexerException($"Invalid keyword", location);
            }
            Advance();
        }

        // Make sure keyword is not followed by alphanumeric
        if (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            throw new AjisLexerException($"Invalid keyword", location);
        }

        var length = _position - start;
        return new AjisToken(type, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexBinaryOrBase64()
    {
        // Could be "bin" or "b64"
        var location = CurrentLocation();

        if (MatchKeyword("bin\""))
        {
            return LexBinaryAttachment(location);
        }

        if (MatchKeyword("b64\""))
        {
            return LexBase64Binary(location);
        }

        throw new AjisLexerException("Expected 'bin\"' or 'b64\"'", location);
    }

    private AjisToken LexHexBinary()
    {
        var location = CurrentLocation();

        if (!MatchKeyword("hex\""))
        {
            throw new AjisLexerException("Expected 'hex\"'", location);
        }

        var start = _position - 4; // Include "hex"

        while (!IsAtEnd() && Peek() != '"')
        {
            if (!IsHexDigit(Peek()) && !IsWhitespace(Peek()))
            {
                throw new AjisLexerException("Invalid hex digit in hex binary", CurrentLocation());
            }
            Advance();
        }

        if (IsAtEnd())
        {
            throw new AjisLexerException("Unterminated hex binary", location);
        }

        Advance(); // Closing "

        var length = _position - start;
        return new AjisToken(AjisTokenType.HexBinary, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexBinaryAttachment(AjisLocation location)
    {
        var start = _position - 4; // Include "bin"

        // For now, just consume until closing quote
        // Binary Wire v1 format handling comes later
        while (!IsAtEnd() && Peek() != '"')
        {
            Advance();
        }

        if (IsAtEnd())
        {
            throw new AjisLexerException("Unterminated binary attachment", location);
        }

        Advance(); // Closing "

        var length = _position - start;
        return new AjisToken(AjisTokenType.BinaryAttachment, _input.Slice(start, length), location, start, length);
    }

    private AjisToken LexBase64Binary(AjisLocation location)
    {
        var start = _position - 4; // Include "b64"

        while (!IsAtEnd() && Peek() != '"')
        {
            char c = Peek();
            if (!IsBase64Char(c) && !IsWhitespace(c))
            {
                throw new AjisLexerException("Invalid base64 character", CurrentLocation());
            }
            Advance();
        }

        if (IsAtEnd())
        {
            throw new AjisLexerException("Unterminated base64 binary", location);
        }

        Advance(); // Closing "

        var length = _position - start;
        return new AjisToken(AjisTokenType.Base64Binary, _input.Slice(start, length), location, start, length);
    }

    private bool MatchKeyword(string keyword)
    {
        int saved = _position;

        foreach (char c in keyword)
        {
            if (IsAtEnd() || Peek() != c)
            {
                _position = saved;
                return false;
            }
            Advance();
        }

        return true;
    }

    // ===== Helper methods =====

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsAtEnd() => _position >= _input.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char Peek() => _input[_position];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private char PeekAhead(int offset)
    {
        int pos = _position + offset;
        return pos < _input.Length ? _input[pos] : '\0';
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance()
    {
        if (_input[_position] == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        _position++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AjisLocation CurrentLocation() => new(_position, _line, _column);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AjisToken CreateToken(AjisTokenType type, int length)
    {
        return new AjisToken(type, _input.Slice(_position, length), CurrentLocation(), _position, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBinaryDigit(char c) => c == '0' || c == '1';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNumberSeparator(char c) => c == '_' || c == ' '; // Comma is a token, not a separator!

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBase64Char(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
        c == '+' || c == '/' || c == '=';
}
