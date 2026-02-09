using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Afrowave.AJIS;

/// <summary>
/// High-performance UTF-8 based serializer using Utf8JsonWriter.
/// This provides significant performance improvements over StringBuilder-based serialization.
/// </summary>
public static class AjisUtf8Serializer
{
    // Pool of ArrayBufferWriter instances to reduce allocations
    private static readonly ConcurrentBag<ArrayBufferWriter<byte>> _bufferPool = new();
    private const int MaxPooledBufferSize = 1024 * 1024; // 1MB

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
                if (value.TryGetUtf8String(out var utf8Value))
                {
                    writer.WriteStringValue(utf8Value);
                }
                else
                {
                    writer.WriteStringValue(value.AsString());
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

            writer.WriteNumberValue(intValue);
        }
        else
        {
            writer.WriteNumberValue(number);
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

            // Apply naming policy to key
            string key = kvp.Key;
            if (options.PropertyNamingPolicy != null)
            {
                key = options.PropertyNamingPolicy.ConvertName(key);
            }

            writer.WritePropertyName(key);
            WriteValue(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}
