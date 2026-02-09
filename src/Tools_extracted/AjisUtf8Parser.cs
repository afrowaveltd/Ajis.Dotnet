using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Afrowave.AJIS.Legacy;

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
        return ParseInternal(utf8Json, options, null);
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
        return ParseInternal(utf8Json.AsSpan(), options, utf8Json);
    }

    private static AjisValue ParseInternal(ReadOnlySpan<byte> utf8Json, AjisParserOptions options, byte[]? backingBuffer)
    {
        Dictionary<string, string>? keyPool = options.EnableKeyPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;
        Dictionary<string, string>? stringPool = options.EnableStringPooling
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : null;

        var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            CommentHandling = JsonCommentHandling.Skip, // Always allow comments
            MaxDepth = options.MaxDepth
        });

        if (!reader.Read())
        {
            throw new AjisParseException("Empty JSON input", default);
        }

        return ParseValue(ref reader, options, keyPool, stringPool, backingBuffer);
    }

    /// <summary>
    /// Parses a string by converting to UTF-8 first using ArrayPool for efficient memory usage.
    /// For best performance, use Parse(ReadOnlySpan&lt;byte&gt;) directly.
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

        return ParseValue(ref reader, options, keyPool, stringPool, null);
    }

    private static AjisValue ParseValue(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => ParseObject(ref reader, options, keyPool, stringPool, backingBuffer),
            JsonTokenType.StartArray => ParseArray(ref reader, options, keyPool, stringPool, backingBuffer),
            JsonTokenType.String => ParseStringValue(ref reader, options, stringPool, backingBuffer),
            JsonTokenType.Number => ParseNumber(ref reader),
            JsonTokenType.True => AjisValue.Boolean(true),
            JsonTokenType.False => AjisValue.Boolean(false),
            JsonTokenType.Null => AjisValue.Null(),
            _ => throw new AjisParseException($"Unexpected token: {reader.TokenType}", default)
        };
    }

    private static AjisValue ParseObject(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer)
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

                var value = ParseValue(ref reader, options, keyPool, stringPool, backingBuffer);

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

    private static AjisValue ParseStringValue(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? stringPool, byte[]? backingBuffer)
    {
        if (options.EnableLazyStringMaterialization && backingBuffer != null && !reader.ValueIsEscaped && !reader.HasValueSequence)
        {
            var start = checked((int)reader.TokenStartIndex + 1);
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

    private static AjisValue ParseArray(ref Utf8JsonReader reader, AjisParserOptions options, Dictionary<string, string>? keyPool, Dictionary<string, string>? stringPool, byte[]? backingBuffer)
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

                items.Add(ParseValue(ref reader, options, keyPool, stringPool, backingBuffer));
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
        return ParseInternal(span, options, buffer);
    }
}

