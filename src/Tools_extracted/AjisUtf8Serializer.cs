using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Afrowave.AJIS.Legacy;

/// <summary>
/// High-performance UTF-8 based serializer using Utf8JsonWriter.
/// This provides significant performance improvements over StringBuilder-based serialization.
/// </summary>
public static class AjisUtf8Serializer
{
    // Pool of ArrayBufferWriter instances to reduce allocations
    private static readonly ConcurrentBag<ArrayBufferWriter<byte>> _bufferPool = new();
    private const int MaxPooledBufferSize = 1024 * 1024; // 1MB

    // Cache converted property names per naming policy instance to avoid repeated string allocations
    private static readonly ConcurrentDictionary<AjisNamingPolicy, ConcurrentDictionary<string, string>> _namingPolicyCache = new();

    // Cache converted property names as UTF-8 bytes to avoid per-serialization string -> utf8 conversions
    private static readonly ConcurrentDictionary<AjisNamingPolicy, ConcurrentDictionary<string, byte[]>> _namingPolicyUtf8Cache = new();

    private static ArrayBufferWriter<byte> RentBuffer()
    {
        if (_bufferPool.TryTake(out var buffer))
        {
            buffer.Clear();
            return buffer;
        }
        return new ArrayBufferWriter<byte>();
    }

    private static void ReturnBuffer(ArrayBufferWriter<byte> buffer)
    {
        // Only pool buffers that aren't too large
        if (buffer.Capacity <= MaxPooledBufferSize)
        {
            _bufferPool.Add(buffer);
        }
    }
    /// <summary>
    /// Serializes to a pooled ArrayBufferWriter so caller can avoid copying.
    /// Caller MUST call <see cref="ReturnBufferWriter(ArrayBufferWriter{byte})"/> when done.
    /// </summary>
    public static ArrayBufferWriter<byte> SerializeToBufferWriter(AjisValue value, AjisSerializerOptions? options = null)
    {
        options ??= AjisSerializerOptions.Ajis;

        var buffer = RentBuffer();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = options.WriteIndented,
            SkipValidation = false,
            Encoder = options.EscapeNonAscii
                ? System.Text.Encodings.Web.JavaScriptEncoder.Default
                : System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }))
        {
            WriteValue(writer, value, options);
            writer.Flush();
        }

        return buffer;
    }

    /// <summary>
    /// Returns a buffer previously obtained from <see cref="SerializeToBufferWriter"/>.
    /// </summary>
    public static void ReturnBufferWriter(ArrayBufferWriter<byte> buffer)
    {
        if (buffer == null) return;
        ReturnBuffer(buffer);
    }
    /// <summary>
    /// Serializes an AjisValue to UTF-8 bytes.
    /// This is the most efficient serialization method.
    /// </summary>
    public static byte[] SerializeToUtf8Bytes(AjisValue value, AjisSerializerOptions? options = null)
    {
        options ??= AjisSerializerOptions.Ajis;

        var buffer = RentBuffer();
        try
        {
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
            {
                Indented = options.WriteIndented,
                SkipValidation = false,
                Encoder = options.EscapeNonAscii
                    ? System.Text.Encodings.Web.JavaScriptEncoder.Default
                    : System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }))
            {
                WriteValue(writer, value, options);
                writer.Flush();
            }

            return buffer.WrittenSpan.ToArray();
        }
        finally
        {
            ReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Serializes an AjisValue to a string.
    /// Note: For best performance, use SerializeToUtf8Bytes and work with UTF-8 directly.
    /// </summary>
    public static string Serialize(AjisValue value, AjisSerializerOptions? options = null)
    {
        var utf8Bytes = SerializeToUtf8Bytes(value, options);
        return Encoding.UTF8.GetString(utf8Bytes);
    }

    /// <summary>
    /// Serializes an AjisValue directly to a stream.
    /// </summary>
    public static void SerializeToStream(AjisValue value, Stream stream, AjisSerializerOptions? options = null)
    {
        options ??= AjisSerializerOptions.Ajis;

        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Indented = options.WriteIndented,
            SkipValidation = false
        });

        WriteValue(writer, value, options);
        writer.Flush();
    }

    /// <summary>
    /// Asynchronously serializes an AjisValue directly to a stream using a pooled buffer to avoid blocking I/O.
    /// </summary>
    public static async System.Threading.Tasks.Task SerializeToStreamAsync(AjisValue value, Stream stream, AjisSerializerOptions? options = null, System.Threading.CancellationToken cancellationToken = default)
    {
        options ??= AjisSerializerOptions.Ajis;

        var buffer = RentBuffer();
        try
        {
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
            {
                Indented = options.WriteIndented,
                SkipValidation = false,
                Encoder = options.EscapeNonAscii
                    ? System.Text.Encodings.Web.JavaScriptEncoder.Default
                    : System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }))
            {
                WriteValue(writer, value, options);
                writer.Flush();
            }

            var mem = buffer.WrittenMemory;
            await stream.WriteAsync(mem, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ReturnBuffer(buffer);
        }
    }

    private static void WriteValue(Utf8JsonWriter writer, AjisValue value, AjisSerializerOptions options)
    {
        switch (value.Type)
        {
            case AjisValueType.Null:
                writer.WriteNullValue();
                break;

            case AjisValueType.Boolean:
                writer.WriteBooleanValue(value.AsBoolean());
                break;

            case AjisValueType.Number:
                WriteNumber(writer, value.AsNumber(), options);
                break;

            case AjisValueType.String:
                // Try to use the Utf8-backed representation to avoid allocations
                if (value.TryGetUtf8String(out var utf8Value))
                {
                    // If the UTF-8 bytes contain no characters that require escaping
                    // and EscapeNonAscii is not requested, we can write raw: "<bytes>"
                    bool escapeNonAscii = options.EscapeNonAscii;
                    if (IsSafeUtf8String(utf8Value, escapeNonAscii) && utf8Value.Length <= 1024)
                    {
                        Span<byte> buf = stackalloc byte[utf8Value.Length + 2];
                        buf[0] = (byte)'"';
                        utf8Value.CopyTo(buf.Slice(1));
                        buf[utf8Value.Length + 1] = (byte)'"';
                        writer.WriteRawValue(buf, skipInputValidation: true);
                    }
                    else
                    {
                        writer.WriteStringValue(utf8Value);
                    }
                }
                else
                {
                    var s = value.AsString();
                    if (s.Length == 0)
                    {
                        writer.WriteStringValue(s);
                    }
                    else
                    {
                        // Try stackalloc encode for small strings to avoid allocations
                        int maxBytes = s.Length * 4; // worst-case UTF-8
                        if (maxBytes <= 1024)
                        {
                            Span<byte> buf = stackalloc byte[maxBytes + 2];
                            // Reserve first byte for opening quote
                            var dest = buf.Slice(1, maxBytes);
                            int written;
                            try
                            {
                                written = System.Text.Encoding.UTF8.GetBytes(s.AsSpan(), dest);
                            }
                            catch
                            {
                                writer.WriteStringValue(s);
                                break;
                            }

                            var actual = dest.Slice(0, written);
                            if (IsSafeUtf8String(actual, options.EscapeNonAscii))
                            {
                                buf[0] = (byte)'"';
                                buf[written + 1] = (byte)'"';
                                writer.WriteRawValue(buf.Slice(0, written + 2), skipInputValidation: true);
                            }
                            else
                            {
                                writer.WriteStringValue(s);
                            }
                        }
                        else
                        {
                            // For larger strings, rent a temporary buffer to avoid large heap allocations
                            int needed = maxBytes + 2;
                            var pool = ArrayPool<byte>.Shared;
                            var rented = pool.Rent(needed);
                            try
                            {
                                var dest = new Span<byte>(rented, 1, maxBytes);
                                int written;
                                try
                                {
                                    written = System.Text.Encoding.UTF8.GetBytes(s.AsSpan(), dest);
                                }
                                catch
                                {
                                    writer.WriteStringValue(s);
                                    break;
                                }

                                var actual = dest.Slice(0, written);
                                if (IsSafeUtf8String(actual, options.EscapeNonAscii))
                                {
                                    rented[0] = (byte)'"';
                                    rented[written + 1] = (byte)'"';
                                    writer.WriteRawValue(new ReadOnlySpan<byte>(rented, 0, written + 2), skipInputValidation: true);
                                }
                                else
                                {
                                    writer.WriteStringValue(s);
                                }
                            }
                            finally
                            {
                                pool.Return(rented);
                            }
                        }
                    }
                }
                break;

            case AjisValueType.Array:
                WriteArray(writer, value, options);
                break;

            case AjisValueType.Object:
                WriteObject(writer, value, options);
                break;

            default:
                throw new InvalidOperationException($"Unknown value type: {value.Type}");
        }
    }

    private static void WriteNumber(Utf8JsonWriter writer, double number, AjisSerializerOptions options)
    {
        // Check if it's an integer
        if (number % 1 == 0 && number >= long.MinValue && number <= long.MaxValue)
        {
            long intValue = (long)number;

            // Use AJIS extensions if enabled (custom formatting)
            if (options.UseAjisExtensions)
            {
                // For hex values, we need to write as string since Utf8JsonWriter doesn't support custom number formats
                if (intValue >= 0 && intValue % 16 == 0 && intValue >= 256)
                {
                    writer.WriteStringValue($"0x{intValue:X}");
                    return;
                }
            }

            // Format integer into UTF-8 bytes and write raw to avoid intermediate string allocations
            Span<byte> buf = stackalloc byte[32];
            if (Utf8Formatter.TryFormat(intValue, buf, out var bytesWritten))
            {
                writer.WriteRawValue(buf.Slice(0, bytesWritten), skipInputValidation: true);
            }
            else
            {
                writer.WriteNumberValue(intValue);
            }
        }
        else
        {
            // Format floating point into UTF-8 bytes and write raw to avoid intermediate string allocations
            Span<byte> buf = stackalloc byte[64];
            if (Utf8Formatter.TryFormat(number, buf, out var bytesWritten))
            {
                writer.WriteRawValue(buf.Slice(0, bytesWritten), skipInputValidation: true);
            }
            else
            {
                writer.WriteNumberValue(number);
            }
        }
    }

    private static void WriteArray(Utf8JsonWriter writer, AjisValue array, AjisSerializerOptions options)
    {
        writer.WriteStartArray();

        var items = array.AsArray();
        foreach (var item in items)
        {
            WriteValue(writer, item, options);
        }

        writer.WriteEndArray();
    }

    private static void WriteObject(Utf8JsonWriter writer, AjisValue obj, AjisSerializerOptions options)
    {
        writer.WriteStartObject();

        var properties = obj.AsObject();
        foreach (var kvp in properties)
        {
            // Skip null values if configured
            if (!options.WriteNullValues && kvp.Value.Type == AjisValueType.Null)
                continue;

            // Apply naming policy to key with caching to avoid repeated conversions
            string key = kvp.Key;
            if (options.PropertyNamingPolicy != null)
            {
                var policy = options.PropertyNamingPolicy;

                // Try UTF8 cache first to avoid repeated Encoding conversions
                var utf8Cache = _namingPolicyUtf8Cache.GetOrAdd(policy, _ => new ConcurrentDictionary<string, byte[]>());
                if (utf8Cache.TryGetValue(key, out var utf8Bytes))
                {
                    writer.WritePropertyName(utf8Bytes);
                }
                else
                {
                    // Fallback: convert name and store both string and utf8 bytes
                    var strCache = _namingPolicyCache.GetOrAdd(policy, _ => new ConcurrentDictionary<string, string>());
                    var converted = strCache.GetOrAdd(key, k => policy.ConvertName(k));
                    utf8Bytes = Encoding.UTF8.GetBytes(converted);
                    utf8Cache.TryAdd(key, utf8Bytes);
                    writer.WritePropertyName(utf8Bytes);
                }
            }
            else
            {
                writer.WritePropertyName(key);
            }
            WriteValue(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }

    private static bool IsSafeUtf8String(ReadOnlySpan<byte> utf8, bool escapeNonAscii)
    {
        // Safe if no control chars (< 0x20), no double-quote ("), no backslash (\)
        // and if escapeNonAscii==true then also require all bytes to be ASCII (<= 0x7F)
        foreach (var b in utf8)
        {
            if (b == (byte)'"' || b == (byte)'\\') return false;
            if (b < 0x20) return false;
            if (escapeNonAscii && b > 0x7F) return false;
        }
        return true;
    }
}
