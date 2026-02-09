using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Afrowave.AJIS;

/// <summary>
/// High-performance UTF-8 based parser using Utf8JsonReader.
/// Significantly faster than string-based parsing, approaching System.Text.Json performance.
/// </summary>
public static class AjisUtf8Parser
{
    // Object pools for reducing allocations
    private static readonly ConcurrentBag<Dictionary<string, AjisValue>> _dictPool = new();
    private static readonly ConcurrentBag<List<AjisValue>> _listPool = new();
    private const int MaxPooledCollectionSize = 128;
    private const int MaxPooledStringLength = 32;
    private const int MaxStringPoolEntries = 1024;

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
    /// <summary>
    /// Parses UTF-8 encoded JSON/AJIS bytes directly.
    /// This is the fastest parsing method available.
    /// </summary>
    public static AjisValue Parse(ReadOnlySpan<byte> utf8Json, AjisParserOptions? options = null)
    {
        options ??= AjisParserOptions.Ajis;

        // If lazy string materialization is enabled and no AJIS normalization is needed,
        // create a backing byte[] so parsed string values can be represented as Utf8String
        // without allocating managed strings. We avoid renting/returning pooled arrays here
        // because the resulting AjisValue objects may reference the buffer for the lifetime
        // of the document.
        if (options.EnableLazyStringMaterialization && !options.AllowNumericSeparators && !options.AllowExtendedNumberFormats)
        {
            var arr = new byte[utf8Json.Length];
            utf8Json.CopyTo(arr);
            return ParseInternal(arr.AsSpan(0, utf8Json.Length), options, arr, 0);
        }

        return ParseInternal(utf8Json, options, null, 0);
    }

    /// <summary>
    /// Parses UTF-8 encoded JSON/AJIS bytes from a byte array.
    /// Enables lazy string materialization when configured.
    /// </summary>
    public static AjisValue Parse(byte[] utf8Json, AjisParserOptions? options = null)
    {
        if (utf8Json == null)
        {
            throw new ArgumentNullException(nameof(utf8Json));
        }

        options ??= AjisParserOptions.Ajis;
        return ParseInternal(utf8Json.AsSpan(), options, utf8Json, 0);
    }

    private static AjisValue ParseInternal(ReadOnlySpan<byte> utf8Json, AjisParserOptions options, byte[]? backingBuffer, int backingBufferBase)
    {
        // If AJIS extensions are enabled we may need to normalize the input
        // so that Utf8JsonReader can consume it (remove numeric separators,
        // convert hex/bin/oct numbers to plain decimal, etc.).
        (byte[]? normalizedBuffer, int normalizedLength, bool normalizedPooled) = (null, 0, false);
        ReadOnlySpan<byte> inputSpan = utf8Json;
        if (options.AllowNumericSeparators || options.AllowExtendedNumberFormats)
        {
            var normalizedResult = NormalizeAjisToJson(utf8Json, options);
            normalizedBuffer = normalizedResult.buffer;
            normalizedLength = normalizedResult.length;
            normalizedPooled = normalizedResult.pooled;
            inputSpan = normalizedBuffer.AsSpan(0, normalizedLength);
        }
        Dictionary<string, string>? keyPool = options.EnableKeyPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;
        Dictionary<string, string>? stringPool = options.EnableStringPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;

        var reader = new Utf8JsonReader(inputSpan, new JsonReaderOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            CommentHandling = JsonCommentHandling.Skip, // Always allow comments
            MaxDepth = options.MaxDepth
        });
        try
        {
            if (!reader.Read())
            {
                throw new AjisParseException("Empty JSON input", default);
            }

                return ParseValue(ref reader, options, keyPool, stringPool, backingBuffer, backingBufferBase);
        }
        catch (JsonException je)
        {
            // Wrap System.Text.Json exceptions into AjisParseException for callers
            throw new AjisParseException(je.Message, default, je);
        }
        finally
        {
            // If we returned a pooled buffer from NormalizeAjisToJson, return it now
            if (normalizedBuffer != null && normalizedPooled)
            {
                ArrayPool<byte>.Shared.Return(normalizedBuffer);
            }
        }
    }

    private static (byte[] buffer, int length, bool pooled) NormalizeAjisToJson(ReadOnlySpan<byte> src, AjisParserOptions options)
    {
        var pool = ArrayPool<byte>.Shared;
        var buf = pool.Rent(src.Length);
        int di = 0;

        // single stackalloc for numeric formatting to avoid repeated allocations inside loops
        Span<char> numChars = stackalloc char[32];

        bool inString = false;
        bool escape = false;

        for (int i = 0; i < src.Length; i++)
        {
            byte b = src[i];

            if (inString)
            {
                buf[di++] = b;
                if (escape)
                {
                    escape = false;
                }
                else if (b == (byte)'\\')
                {
                    escape = true;
                }
                else if (b == (byte)'"')
                {
                    inString = false;
                }
                continue;
            }

            if (b == (byte)'"')
            {
                inString = true;
                buf[di++] = b;
                continue;
            }

            // Numeric separator removal: only when '_' occurs between digits
            if (options.AllowNumericSeparators && b == (byte)'_')
            {
                // check previous written char and next source char
                byte prev = di > 0 ? buf[di - 1] : (byte)0;
                byte next = i + 1 < src.Length ? src[i + 1] : (byte)0;
                if (IsDigit(prev) && IsDigit(next))
                {
                    continue; // drop separator
                }
            }

            // Extended number formats: hex, binary, octal starting with 0x/0b/0o
            if (options.AllowExtendedNumberFormats && b == (byte)'0' && i + 1 < src.Length)
            {
                byte t = src[i + 1];
                if (t == (byte)'x' || t == (byte)'X')
                {
                    int j = i + 2;
                    ulong value = 0;
                    bool any = false;
                    while (j < src.Length)
                    {
                        byte ch = src[j];
                        if (ch == (byte)'_') { j++; continue; }
                        int digit = HexValue(ch);
                        if (digit < 0) break;
                        any = true;
                        value = (value << 4) + (ulong)digit;
                        j++;
                    }
                    if (any)
                    {
                        if (value.TryFormat(numChars, out int written, default, CultureInfo.InvariantCulture))
                        {
                            for (int k = 0; k < written; k++) buf[di++] = (byte)numChars[k];
                        }
                        else
                        {
                            var dec = value.ToString();
                            var decBytes = Encoding.UTF8.GetBytes(dec);
                            for (int k = 0; k < decBytes.Length; k++) buf[di++] = decBytes[k];
                        }
                        i = j - 1;
                        continue;
                    }
                }
                else if (t == (byte)'b' || t == (byte)'B')
                {
                    int j = i + 2;
                    ulong value = 0;
                    bool any = false;
                    while (j < src.Length)
                    {
                        byte ch = src[j];
                        if (ch == (byte)'_') { j++; continue; }
                        if (ch == (byte)'0' || ch == (byte)'1')
                        {
                            any = true;
                            value = (value << 1) + (ulong)(ch - '0');
                            j++; continue;
                        }
                        break;
                    }
                    if (any)
                    {
                        if (value.TryFormat(numChars, out int written, default, CultureInfo.InvariantCulture))
                        {
                            for (int k = 0; k < written; k++) buf[di++] = (byte)numChars[k];
                        }
                        else
                        {
                            var dec = value.ToString();
                            var decBytes = Encoding.UTF8.GetBytes(dec);
                            for (int k = 0; k < decBytes.Length; k++) buf[di++] = decBytes[k];
                        }
                        i = j - 1;
                        continue;
                    }
                }
                else if (t == (byte)'o' || t == (byte)'O')
                {
                    int j = i + 2;
                    ulong value = 0;
                    bool any = false;
                    while (j < src.Length)
                    {
                        byte ch = src[j];
                        if (ch == (byte)'_') { j++; continue; }
                        if (ch >= (byte)'0' && ch <= (byte)'7')
                        {
                            any = true;
                            value = (value << 3) + (ulong)(ch - '0');
                            j++; continue;
                        }
                        break;
                    }
                    if (any)
                    {
                        if (value.TryFormat(numChars, out int written, default, CultureInfo.InvariantCulture))
                        {
                            for (int k = 0; k < written; k++) buf[di++] = (byte)numChars[k];
                        }
                        else
                        {
                            var dec = value.ToString();
                            var decBytes = Encoding.UTF8.GetBytes(dec);
                            for (int k = 0; k < decBytes.Length; k++) buf[di++] = decBytes[k];
                        }
                        i = j - 1;
                        continue;
                    }
                }
            }

            buf[di++] = b;
        }

        // If lazy string materialization is enabled, we must return an exact-sized non-pooled array
        if (options.EnableLazyStringMaterialization)
        {
            var result = new byte[di];
            Array.Copy(buf, 0, result, 0, di);
            // Return the working buffer
            pool.Return(buf);
            return (result, di, false);
        }

        // Otherwise, return the pooled buffer and the length; caller will return it to the pool when safe
        return (buf, di, true);
    }

    private static bool IsDigit(byte b) => b >= (byte)'0' && b <= (byte)'9';

    private static int HexValue(byte b)
    {
        if (b >= (byte)'0' && b <= (byte)'9') return b - (byte)'0';
        if (b >= (byte)'a' && b <= (byte)'f') return 10 + b - (byte)'a';
        if (b >= (byte)'A' && b <= (byte)'F') return 10 + b - (byte)'A';
        return -1;
    }

                        
    /// <summary>
    /// Parses a string by converting to UTF-8 first using ArrayPool for efficient memory usage.
    /// For best performance, use Parse(ReadOnlySpan<byte>) directly.
    /// </summary>
    public static AjisValue Parse(string json, AjisParserOptions? options = null)
    {
        if (string.IsNullOrEmpty(json))
        {
            throw new AjisParseException("Empty JSON input", default);
        }

        // Calculate exact UTF-8 byte count to avoid over-allocation
        var byteCount = Encoding.UTF8.GetByteCount(json);

        // Rent buffer from pool - more efficient than allocating
        byte[]? rentedBuffer = null;
        try
        {
            // Use stack allocation for small strings (<= 256 chars)
            Span<byte> buffer = byteCount <= 512
                ? stackalloc byte[byteCount]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

            var bytesWritten = Encoding.UTF8.GetBytes(json, buffer);
            return Parse(buffer.Slice(0, bytesWritten), options);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    /// <summary>
    /// Parses from a ReadOnlySequence (useful for streaming scenarios).
    /// </summary>
    public static AjisValue Parse(ReadOnlySequence<byte> utf8Json, AjisParserOptions? options = null)
    {
        options ??= AjisParserOptions.Ajis;

        Dictionary<string, string>? keyPool = options.EnableKeyPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;
        Dictionary<string, string>? stringPool = options.EnableStringPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;

        var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = options.MaxDepth
        });

        if (!reader.Read())
        {
            throw new AjisParseException("Empty JSON input", default);
        }

        return ParseValue(ref reader, options, keyPool, stringPool, null, 0);
    }

    /// <summary>
    /// Parse a slice of a UTF-8 memory buffer without copying when possible.
    /// </summary>
    public static AjisValue Parse(ReadOnlyMemory<byte> utf8Json, int start, int length, AjisParserOptions? options = null)
    {
        if (start < 0 || length < 0 || start + length > utf8Json.Length) throw new ArgumentOutOfRangeException();
        var slice = utf8Json.Slice(start, length);

        // If the underlying memory is array-backed, pass the array and base to avoid copying
        if (MemoryMarshal.TryGetArray(utf8Json, out ArraySegment<byte> seg) && seg.Array != null)
        {
            var backing = seg.Array;
            var baseIndex = seg.Offset + start;
            return ParseInternal(slice.Span, options ?? AjisParserOptions.Ajis, backing, baseIndex);
        }

        // Fallback: parse from span (may copy for lazy strings)
        return Parse(slice.Span, options);
    }

    private static AjisValue ParseValue(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer, int backingBufferBase)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ParseObject(ref reader, options, keyPool, stringPool, backingBuffer, backingBufferBase),
            JsonTokenType.StartArray => ParseArray(ref reader, options, keyPool, stringPool, backingBuffer, backingBufferBase),
            JsonTokenType.String => ParseStringValue(ref reader, options, stringPool, backingBuffer, backingBufferBase),
            JsonTokenType.Number => ParseNumber(ref reader),
            JsonTokenType.True => AjisValue.Boolean(true),
            JsonTokenType.False => AjisValue.Boolean(false),
            JsonTokenType.Null => AjisValue.Null(),
            _ => throw new AjisParseException($"Unexpected token: {reader.TokenType}", default)
        };
    }

    private static AjisValue ParseObject(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer, int backingBufferBase)
    {
        var properties = RentDictionary();
        var returnToPool = true;

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    // Transfer ownership - don't return to pool
                    returnToPool = false;
                    return AjisValue.Object(properties);
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new AjisParseException($"Expected property name, got {reader.TokenType}", default);
                }

                var key = ReadPropertyName(ref reader, options, keyPool);

                // Read the value
                if (!reader.Read())
                {
                    throw new AjisParseException("Unexpected end of object", default);
                }

                    var value = ParseValue(ref reader, options, keyPool, stringPool, backingBuffer, backingBufferBase);

                if (options.ThrowOnDuplicateKeys)
                {
                    if (!properties.TryAdd(key, value))
                    {
                        throw new AjisParseException($"Duplicate key '{key}' in object", default);
                    }
                }
                else
                {
                    properties[key] = value;
                }
            }

            throw new AjisParseException("Unterminated object", default);
        }
        finally
        {
            if (returnToPool)
            {
                ReturnDictionary(properties);
            }
        }
    }

    private static string ReadPropertyName(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool)
    {
        var key = ReadString(ref reader, null);

        if (options.PropertyNamingPolicy != null)
        {
            key = options.PropertyNamingPolicy.ConvertName(key);
        }

        if (keyPool == null)
        {
            return key;
        }

        if (keyPool.TryGetValue(key, out var pooled))
        {
            return pooled;
        }

        keyPool[key] = key;
        return key;
    }

    private static AjisValue ParseStringValue(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? stringPool, byte[]? backingBuffer, int backingBufferBase)
    {
        if (options.EnableLazyStringMaterialization && backingBuffer != null && !reader.ValueIsEscaped && !reader.HasValueSequence)
        {
            var start = checked((int)reader.TokenStartIndex + 1 + backingBufferBase);
            var length = reader.ValueSpan.Length;

            if (start >= 0 && start + length <= backingBuffer.Length)
            {
                return AjisValue.Utf8String(backingBuffer, start, length);
            }
        }

        var value = ReadString(ref reader, stringPool);
        return AjisValue.String(value);
    }

    private static string ReadString(ref Utf8JsonReader reader, Dictionary<string, string>? stringPool)
    {
        if (!reader.ValueIsEscaped && !reader.HasValueSequence)
        {
            var raw = Encoding.UTF8.GetString(reader.ValueSpan);
            return PoolString(raw, stringPool);
        }

        var value = reader.GetString() ?? "";
        return PoolString(value, stringPool);
    }

    private static string PoolString(string value, Dictionary<string, string>? stringPool)
    {
        if (stringPool == null)
        {
            return value;
        }

        if (value.Length > MaxPooledStringLength)
        {
            return value;
        }

        if (stringPool.TryGetValue(value, out var pooled))
        {
            return pooled;
        }

        if (stringPool.Count < MaxStringPoolEntries)
        {
            stringPool[value] = value;
        }

        return value;
    }

    private static AjisValue ParseArray(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer, int backingBufferBase)
    {
        var items = RentList();
        var returnToPool = true;

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    // Transfer ownership - don't return to pool
                    returnToPool = false;
                    return AjisValue.Array(items);
                }

                items.Add(ParseValue(ref reader, options, keyPool, stringPool, backingBuffer, backingBufferBase));
            }

            throw new AjisParseException("Unterminated array", default);
        }
        finally
        {
            if (returnToPool)
            {
                ReturnList(items);
            }
        }
    }

    private static AjisValue ParseNumber(ref Utf8JsonReader reader)
    {
        // Try to get as integer first for better precision
        if (reader.TryGetInt64(out var longValue))
        {
            return AjisValue.Number(longValue);
        }

        // Fall back to double
        if (reader.TryGetDouble(out var doubleValue))
        {
            return AjisValue.Number(doubleValue);
        }

        // If both fail, throw
        throw new AjisParseException("Invalid number format", default);
    }

    /// <summary>
    /// Asynchronously parses JSON/AJIS from a stream.
    /// Reads the stream into a buffer and parses using Utf8JsonReader.
    /// </summary>
    public static async ValueTask<AjisValue> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default,
        AjisParserOptions? options = null)
    {
        // For streams, we need to read into a buffer
        // Use ArrayPool for large allocations to avoid LOH
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);

        var buffer = ms.GetBuffer();
        var span = buffer.AsSpan(0, (int)ms.Length);

        options ??= AjisParserOptions.Ajis;
        return ParseInternal(span, options, buffer, 0);
    }
}
