using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Afrowave.AJIS;

/// <summary>
/// Parser that converts tokens from AjisLexer into an AST (AjisValue tree).
/// Optimized version using direct lexer access instead of pre-tokenization.
/// </summary>
internal sealed class AjisParser
{
    private const int MaxPooledStringLength = 32;
    private const int MaxStringPoolEntries = 1024;
    private const int MaxPooledCollectionSize = 128;

    private static readonly ConcurrentBag<Dictionary<string, AjisValue>> _dictPool = new();
    private static readonly ConcurrentBag<List<AjisValue>> _listPool = new();
    private static readonly ConcurrentBag<Dictionary<string, string>> _keyStringPool = new();
    private static readonly ConcurrentBag<Dictionary<string, string>> _stringPoolPool = new();
    private static readonly ConcurrentBag<System.Text.StringBuilder> _sbPool = new();

    private readonly ReadOnlyMemory<char> _input;
    private readonly AjisLexerOptions _lexerOptions;
    private readonly AjisParserOptions _parserOptions;
    private readonly Dictionary<string, string>? _keyPool;
    private readonly Dictionary<string, string>? _stringPool;
    private AjisTokenType _currentType;
    private int _currentStart;
    private int _currentLength;
    private AjisLocation _currentLocation;
    private int _depth;

    public AjisParser(string input, AjisLexerOptions? lexerOptions = null, AjisParserOptions? parserOptions = null)
        : this(input.AsMemory(), lexerOptions, parserOptions)
    {
    }

    public AjisParser(ReadOnlyMemory<char> input, AjisLexerOptions? lexerOptions = null, AjisParserOptions? parserOptions = null)
    {
        _input = input;
        _lexerOptions = lexerOptions ?? AjisLexerOptions.Json;
        _parserOptions = parserOptions ?? AjisParserOptions.Ajis;
        _keyPool = _parserOptions.EnableKeyPooling
            ? (_keyStringPool.TryTake(out var k) ? k : new Dictionary<string, string>(StringComparer.Ordinal))
            : null;
        _stringPool = _parserOptions.EnableStringPooling
            ? (_stringPoolPool.TryTake(out var s) ? s : new Dictionary<string, string>(StringComparer.Ordinal))
            : null;
        _currentType = AjisTokenType.Eof;
        _currentStart = 0;
        _currentLength = 0;
        _currentLocation = default;
        _depth = 0;
    }

    /// <summary>
    /// Parses the input and returns the root value.
    /// </summary>
    public AjisValue Parse()
    {
        // Create lexer for this parse
        var lexer = new AjisLexer(_input.Span, _lexerOptions);

        try
        {
            // Read first token
            Advance(ref lexer);

            // Parse the root value
            var value = ParseValue(ref lexer);

            // Ensure we consumed everything (only EOF should remain)
            if (_currentType != AjisTokenType.Eof)
            {
                throw new AjisParseException(
                    $"Unexpected token after root value: {_currentType}",
                    _currentLocation);
            }

            return value;
        }
        finally
        {
            // Return pooled key/string dictionaries if used
            if (_keyPool != null)
            {
                _keyPool.Clear();
                _keyStringPool.Add(_keyPool);
            }

            if (_stringPool != null)
            {
                _stringPool.Clear();
                _stringPoolPool.Add(_stringPool);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(ref AjisLexer lexer)
    {
        var token = lexer.NextToken();
        _currentType = token.Type;
        _currentStart = token.Start;
        _currentLength = token.Length;
        _currentLocation = token.Location;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDepth()
    {
        if (_depth >= _parserOptions.MaxDepth)
        {
            throw new AjisParseException(
                $"Maximum parsing depth ({_parserOptions.MaxDepth}) exceeded",
                _currentLocation);
        }
    }

    private AjisValue ParseValue(ref AjisLexer lexer)
    {
        return _currentType switch
        {
            AjisTokenType.LeftBrace => ParseObject(ref lexer),
            AjisTokenType.LeftBracket => ParseArray(ref lexer),
            AjisTokenType.String => ParseString(ref lexer),
            AjisTokenType.Number => ParseNumber(ref lexer),
            AjisTokenType.True => ParseTrue(ref lexer),
            AjisTokenType.False => ParseFalse(ref lexer),
            AjisTokenType.Null => ParseNull(ref lexer),
            AjisTokenType.Eof => throw new AjisParseException("Unexpected end of file", _currentLocation),
            _ => throw new AjisParseException($"Unexpected token: {_currentType}", _currentLocation)
        };
    }

    private AjisValue ParseObject(ref AjisLexer lexer)
    {
        CheckDepth();
        _depth++;

        Expect(AjisTokenType.LeftBrace, ref lexer);

        var properties = RentDictionary();
        var returnToPool = true;

        try
        {
            // Empty object?
            if (_currentType == AjisTokenType.RightBrace)
            {
                Advance(ref lexer);
                _depth--;
                returnToPool = false;
                return AjisValue.Object(properties);
            }

            while (true)
            {
                // Parse key (must be string)
                if (_currentType != AjisTokenType.String)
                {
                    throw new AjisParseException(
                        $"Expected string key in object, got {_currentType}",
                        _currentLocation);
                }

                // Decode the string key
                var key = DecodeStringToken(_input.Span.Slice(_currentStart, _currentLength));

                // Apply naming policy if configured
                if (_parserOptions.PropertyNamingPolicy != null)
                {
                    key = _parserOptions.PropertyNamingPolicy.ConvertName(key);
                }

                if (_keyPool != null)
                {
                    if (_keyPool.TryGetValue(key, out var pooled))
                    {
                        key = pooled;
                    }
                    else
                    {
                        _keyPool[key] = key;
                    }
                }

                Advance(ref lexer);

                // Expect colon
                Expect(AjisTokenType.Colon, ref lexer);

                // Parse value
                var value = ParseValue(ref lexer);

                if (_parserOptions.ThrowOnDuplicateKeys)
                {
                    if (!properties.TryAdd(key, value))
                    {
                        throw new AjisParseException(
                            $"Duplicate key '{key}' in object",
                            _currentLocation);
                    }
                }
                else
                {
                    // Last value wins
                    properties[key] = value;
                }

                // Check for comma or end
                if (_currentType == AjisTokenType.Comma)
                {
                    Advance(ref lexer);

                    // Allow trailing comma if configured
                    if (_currentType == AjisTokenType.RightBrace)
                    {
                        if (!_parserOptions.AllowTrailingCommas)
                        {
                            throw new AjisParseException(
                                "Trailing comma not allowed",
                                _currentLocation);
                        }
                        break;
                    }
                }
                else if (_currentType == AjisTokenType.RightBrace)
                {
                    break;
                }
                else
                {
                    throw new AjisParseException(
                        $"Expected ',' or '}}' in object, got {_currentType}",
                        _currentLocation);
                }
            }

            Expect(AjisTokenType.RightBrace, ref lexer);
            _depth--;
            returnToPool = false;
            return AjisValue.Object(properties);
        }
        finally
        {
            if (returnToPool)
            {
                ReturnDictionary(properties);
            }
        }
    }

    private AjisValue ParseArray(ref AjisLexer lexer)
    {
        CheckDepth();
        _depth++;

        Expect(AjisTokenType.LeftBracket, ref lexer);

        var items = RentList();
        var returnToPool = true;

        try
        {
            // Empty array?
            if (_currentType == AjisTokenType.RightBracket)
            {
                Advance(ref lexer);
                _depth--;
                returnToPool = false;
                return AjisValue.Array(items);
            }

            while (true)
            {
                // Parse value
                var value = ParseValue(ref lexer);
                items.Add(value);

                // Check for comma or end
                if (_currentType == AjisTokenType.Comma)
                {
                    Advance(ref lexer);

                    // Allow trailing comma if configured
                    if (_currentType == AjisTokenType.RightBracket)
                    {
                        if (!_parserOptions.AllowTrailingCommas)
                        {
                            throw new AjisParseException(
                                "Trailing comma not allowed",
                                _currentLocation);
                        }
                        break;
                    }
                }
                else if (_currentType == AjisTokenType.RightBracket)
                {
                    break;
                }
                else
                {
                    throw new AjisParseException(
                        $"Expected ',' or ']' in array, got {_currentType}",
                        _currentLocation);
                }
            }

            Expect(AjisTokenType.RightBracket, ref lexer);
            _depth--;
            returnToPool = false;
            return AjisValue.Array(items);
        }
        finally
        {
            if (returnToPool)
            {
                ReturnList(items);
            }
        }
    }

    private AjisValue ParseString(ref AjisLexer lexer)
    {
        var rawText = _input.Span.Slice(_currentStart, _currentLength);
        Advance(ref lexer);

        var decoded = DecodeStringToken(rawText);
        if (_stringPool != null)
        {
            decoded = PoolString(decoded, _stringPool);
        }
        return AjisValue.String(decoded);
    }

    private string DecodeStringToken(ReadOnlySpan<char> rawText)
    {
        // Remove surrounding quotes and decode escape sequences
        if (rawText.Length < 2 || rawText[0] != '"' || rawText[^1] != '"')
        {
            throw new AjisParseException($"Invalid string format", _currentLocation);
        }

        var content = rawText.Slice(1, rawText.Length - 2);
        return DecodeString(content);
    }

    private string DecodeString(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IndexOf('\\') < 0)
        {
            return input.ToString();
        }

        // Use pooled StringBuilder only when necessary to reduce allocations
        var sb = _sbPool.TryTake(out var rented) ? rented : new System.Text.StringBuilder(input.Length);
        try
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\\' && i + 1 < input.Length)
                {
                    i++;
                    switch (input[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'u':
                            // Unicode escape: \uXXXX
                            if (i + 4 < input.Length)
                            {
                                var hex = input.Slice(i + 1, 4);
                                if (int.TryParse(hex, NumberStyles.HexNumber, null, out var codePoint))
                                {
                                    sb.Append((char)codePoint);
                                    i += 4;
                                }
                                else
                                {
                                    sb.Append('\\').Append(input[i]);
                                }
                            }
                            else
                            {
                                sb.Append('\\').Append(input[i]);
                            }
                            break;
                        default:
                            sb.Append('\\').Append(input[i]);
                            break;
                    }
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            return sb.ToString();
        }
        finally
        {
            sb.Clear();
            if (sb.Capacity <= 4096) // avoid keeping excessively large buffers
            {
                _sbPool.Add(sb);
            }
        }
    }

    private static string PoolString(string value, Dictionary<string, string> pool)
    {
        if (value.Length > MaxPooledStringLength)
        {
            return value;
        }

        if (pool.TryGetValue(value, out var pooled))
        {
            return pooled;
        }

        if (pool.Count < MaxStringPoolEntries)
        {
            pool[value] = value;
        }

        return value;
    }

    private static Dictionary<string, AjisValue> RentDictionary()
    {
        if (_dictPool.TryTake(out var dict))
        {
            return dict;
        }

        return new Dictionary<string, AjisValue>();
    }

    private static void ReturnDictionary(Dictionary<string, AjisValue> dict)
    {
        if (dict.Count <= MaxPooledCollectionSize)
        {
            dict.Clear();
            _dictPool.Add(dict);
        }
    }

    private static List<AjisValue> RentList()
    {
        if (_listPool.TryTake(out var list))
        {
            return list;
        }

        return new List<AjisValue>();
    }

    private static void ReturnList(List<AjisValue> list)
    {
        if (list.Count <= MaxPooledCollectionSize)
        {
            list.Clear();
            _listPool.Add(list);
        }
    }

    private AjisValue ParseNumber(ref AjisLexer lexer)
    {
        var text = _input.Span.Slice(_currentStart, _currentLength);
        Advance(ref lexer);

        var hasSeparators = text.IndexOf('_') >= 0;

        // Check if we need to process separators
        if (!_parserOptions.AllowNumericSeparators && hasSeparators)
        {
            throw new AjisParseException("Numeric separators not allowed", _currentLocation);
        }

        if (!hasSeparators)
        {
            if (text.Length == 1 && text[0] == '0')
            {
                return AjisValue.Number(0);
            }

            if (text.Length == 2 && text[0] == '-' && text[1] == '0')
            {
                return AjisValue.Number(0);
            }
        }

        // Fast path: simple integer without separators or special prefix
        if (text.Length > 0 && text.IndexOf('_') < 0 &&
            text[0] != '0' && text.IndexOfAny('.', 'e', 'E') < 0)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fastInt))
            {
                return AjisValue.Number(fastInt);
            }
        }

        if (!hasSeparators)
        {
            // Check for special number formats (hex, binary, octal)
            if (!_parserOptions.AllowExtendedNumberFormats &&
                (text.Length >= 2 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X' || text[1] == 'b' || text[1] == 'B' || text[1] == 'o' || text[1] == 'O')))
            {
                throw new AjisParseException("Extended number formats not allowed", _currentLocation);
            }

            if (text.Length >= 3 && text[0] == '0')
            {
                char prefix = text[1];
                if (prefix == 'x' || prefix == 'X')
                {
                    // Hex number
                    var hexPart = text.Slice(2);
                    if (long.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                    {
                        return AjisValue.Number(hexValue);
                    }
                    throw new AjisParseException($"Invalid hex number format", _currentLocation);
                }
                else if (prefix == 'b' || prefix == 'B')
                {
                    // Binary number
                    var binaryPart = text.Slice(2);
                    try
                    {
                        var binaryValue = ParseBinarySpan(binaryPart);
                        return AjisValue.Number(binaryValue);
                    }
                    catch
                    {
                        throw new AjisParseException($"Invalid binary number format", _currentLocation);
                    }
                }
                else if (prefix == 'o' || prefix == 'O')
                {
                    // Octal number
                    var octalPart = text.Slice(2);
                    try
                    {
                        var octalValue = ParseOctalSpan(octalPart);
                        return AjisValue.Number(octalValue);
                    }
                    catch
                    {
                        throw new AjisParseException($"Invalid octal number format", _currentLocation);
                    }
                }
            }

            // Regular decimal number (no separators)
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new AjisParseException($"Invalid number format", _currentLocation);
            }

            return AjisValue.Number(value);
        }

        // Remove separators if present
        Span<char> cleanBuffer = stackalloc char[text.Length];
        int writePos = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '_')
            {
                cleanBuffer[writePos++] = c;
            }
        }
        var cleanText = cleanBuffer.Slice(0, writePos);

        // Check for special number formats (hex, binary, octal)
        if (!_parserOptions.AllowExtendedNumberFormats &&
            (cleanText.Length >= 2 && cleanText[0] == '0' && (cleanText[1] == 'x' || cleanText[1] == 'X' || cleanText[1] == 'b' || cleanText[1] == 'B' || cleanText[1] == 'o' || cleanText[1] == 'O')))
        {
            throw new AjisParseException("Extended number formats not allowed", _currentLocation);
        }

        if (cleanText.Length >= 3 && cleanText[0] == '0')
        {
            char prefix = cleanText[1];
            if (prefix == 'x' || prefix == 'X')
            {
                // Hex number
                var hexPart = cleanText.Slice(2);
                if (long.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexValue))
                {
                    return AjisValue.Number(hexValue);
                }
                throw new AjisParseException($"Invalid hex number format", _currentLocation);
            }
            else if (prefix == 'b' || prefix == 'B')
            {
                // Binary number
                var binaryPart = cleanText.Slice(2);
                try
                {
                    var binaryValue = ParseBinarySpan(binaryPart);
                    return AjisValue.Number(binaryValue);
                }
                catch
                {
                    throw new AjisParseException($"Invalid binary number format", _currentLocation);
                }
            }
            else if (prefix == 'o' || prefix == 'O')
            {
                // Octal number
                var octalPart = cleanText.Slice(2);
                try
                {
                    var octalValue = ParseOctalSpan(octalPart);
                    return AjisValue.Number(octalValue);
                }
                catch
                {
                    throw new AjisParseException($"Invalid octal number format", _currentLocation);
                }
            }
        }

        // Regular decimal number
        if (!double.TryParse(cleanText, NumberStyles.Float, CultureInfo.InvariantCulture, out var cleanedValue))
        {
            throw new AjisParseException($"Invalid number format", _currentLocation);
        }

        return AjisValue.Number(cleanedValue);
    }

    private static long ParseBinarySpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            throw new FormatException("Empty binary value");
        }

        long value = 0;
        foreach (var c in span)
        {
            if (c == '0' || c == '1')
            {
                value = checked((value << 1) + (c - '0'));
            }
            else
            {
                throw new FormatException("Invalid binary digit");
            }
        }

        return value;
    }

    private static long ParseOctalSpan(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
        {
            throw new FormatException("Empty octal value");
        }

        long value = 0;
        foreach (var c in span)
        {
            if (c >= '0' && c <= '7')
            {
                value = checked((value << 3) + (c - '0'));
            }
            else
            {
                throw new FormatException("Invalid octal digit");
            }
        }

        return value;
    }

    private AjisValue ParseTrue(ref AjisLexer lexer)
    {
        Advance(ref lexer);
        return AjisValue.Boolean(true);
    }

    private AjisValue ParseFalse(ref AjisLexer lexer)
    {
        Advance(ref lexer);
        return AjisValue.Boolean(false);
    }

    private AjisValue ParseNull(ref AjisLexer lexer)
    {
        Advance(ref lexer);
        return AjisValue.Null();
    }

    private void Expect(AjisTokenType expectedType, ref AjisLexer lexer)
    {
        if (_currentType != expectedType)
        {
            throw new AjisParseException(
                $"Expected {expectedType}, got {_currentType}",
                _currentLocation);
        }
        Advance(ref lexer);
    }
}
