#nullable enable

using System.Buffers;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Reader;
using Afrowave.AJIS.Streaming.Segments;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Generic converter for mapping between .NET objects and AJIS representations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides serialization and deserialization of .NET objects to/from AJIS text,
/// with support for flexible naming policies, custom type converters, nested objects, collections, and attributes.
/// </para>
/// <para>
/// Designed for comfort comparable to Newtonsoft.Json with superior error reporting.
/// </para>
/// </remarks>
/// <typeparam name="T">The target type for conversion.</typeparam>
public class AjisConverter<T> where T : notnull
{
    private readonly INamingPolicy _namingPolicy;
    private readonly PropertyMapper _propertyMapper;
    private readonly Dictionary<Type, object> _customConverters = new();
    private const int MaxDepth = 100; // Prevent stack overflow

    /// <summary>
    /// Initializes a new instance of the <see cref="AjisConverter{T}"/> class with default settings.
    /// </summary>
    public AjisConverter() : this(PascalCaseNamingPolicy.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AjisConverter{T}"/> class with a specific naming policy.
    /// </summary>
    /// <param name="namingPolicy">The naming policy to use for property name mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown if namingPolicy is null.</exception>
    public AjisConverter(INamingPolicy namingPolicy)
    {
        _namingPolicy = namingPolicy ?? throw new ArgumentNullException(nameof(namingPolicy));
        
        // PHASE 7B: Use global singleton PropertyMapper instead of creating new instance
        // This shares type metadata cache across all AjisConverter<T> instances
        _propertyMapper = GlobalPropertyMapperFactory.GetOrCreate(namingPolicy);
    }

    /// <summary>
    /// Gets the naming policy used by this converter.
    /// </summary>
    public INamingPolicy NamingPolicy => _namingPolicy;

    /// <summary>
    /// Registers a custom converter for a specific type.
    /// </summary>
    /// <typeparam name="TTarget">The type this converter handles.</typeparam>
    /// <param name="converter">The converter instance to register.</param>
    /// <returns>This converter instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if converter is null.</exception>
    /// <remarks>
    /// Custom converters allow specialized handling for types like DateTime, Guid, or domain types.
    /// </remarks>
    public AjisConverter<T> RegisterConverter<TTarget>(ICustomAjisConverter<TTarget> converter) where TTarget : notnull
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        _customConverters[typeof(TTarget)] = converter;
        return this;
    }

    /// <summary>
    /// Serializes an object to AJIS text format.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The AJIS text representation of the object.</returns>
    /// <remarks>
    /// The returned text conforms to the AJIS or JSON specification depending on the formatter configuration.
    /// Supports nested objects, collections, and all attribute-based overrides.
    /// OPTIMIZED: Uses cached direct Utf8JsonWriter for maximum performance.
    /// </remarks>
    public string Serialize(T value)
    {
        if (value == null)
            return "null";

        // PHASE 7A: Use CACHED serializer instance (not new each time!)
        // This eliminates PropertySetterCompiler cache invalidation
        var serializer = GetCachedSerializer();
        return serializer.Serialize(value);
    }

    /// <summary>
    /// Deserializes an AJIS string to an object of type T.
    /// </summary>
    /// <param name="ajisText">The AJIS text to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="FormatException">Thrown if the AJIS text is malformed or cannot be converted to type T.</exception>
    public T? Deserialize(string ajisText)
    {
        if (string.IsNullOrEmpty(ajisText))
            throw new ArgumentException("AJIS text cannot be null or empty.", nameof(ajisText));

        // PHASE 8A: Use ArrayPool<byte> to avoid massive allocation!
        // For 1M records (65MB JSON), Encoding.UTF8.GetBytes() allocates 65MB!
        var byteCount = Encoding.UTF8.GetByteCount(ajisText);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(ajisText, buffer);
            return DeserializeFromUtf8(buffer.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Deserializes UTF8 bytes directly using Utf8JsonReader (ULTRA FAST!).
    /// Based on successful AjisUtf8Parser design from Tools_extracted.
    /// </summary>
    public T? DeserializeFromUtf8(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var deserializer = GetCachedDeserializer();
            return deserializer.Deserialize(utf8Json);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to deserialize AJIS: {ex.Message}", ex);
        }
    }

    // PHASE 7A: Cache serializer and deserializer instances
    // Static fields ensure we only create once per <T> type
    private static Utf8DirectSerializer<T>? _cachedSerializer;
    private static Utf8DirectDeserializer<T>? _cachedDeserializer;
    private static readonly object _serializerLock = new();
    private static readonly object _deserializerLock = new();

    private Utf8DirectSerializer<T> GetCachedSerializer()
    {
        if (_cachedSerializer != null)
            return _cachedSerializer;

        lock (_serializerLock)
        {
            if (_cachedSerializer != null)
                return _cachedSerializer;

            _cachedSerializer = new Utf8DirectSerializer<T>(_propertyMapper);
            return _cachedSerializer;
        }
    }

    private Utf8DirectDeserializer<T> GetCachedDeserializer()
    {
        if (_cachedDeserializer != null)
            return _cachedDeserializer;

        lock (_deserializerLock)
        {
            if (_cachedDeserializer != null)
                return _cachedDeserializer;

            _cachedDeserializer = new Utf8DirectDeserializer<T>(_propertyMapper);
            return _cachedDeserializer;
        }
    }

    /// <summary>
    /// Recursively writes a segment value to Utf8JsonWriter.
    /// </summary>
    private void WriteSegmentValue(List<AjisSegment> segments, ref int index, Utf8JsonWriter writer)
    {
        if (index >= segments.Count)
            return;

        var segment = segments[index];

        // Handle values
        if (segment.Kind == AjisSegmentKind.Value && segment.ValueKind.HasValue)
        {
            switch (segment.ValueKind.Value)
            {
                case AjisValueKind.Null:
                    writer.WriteNullValue();
                    break;

                case AjisValueKind.Boolean:
                    if (segment.Slice != null)
                    {
                        // OPTIMIZED: Direct byte comparison, no string allocation
                        writer.WriteBooleanValue(segment.Slice.Value.Bytes.Span.SequenceEqual("true"u8));
                    }
                    else
                    {
                        writer.WriteBooleanValue(false);
                    }
                    break;

                case AjisValueKind.Number:
                    if (segment.Slice != null)
                    {
                        // Write raw number bytes directly (fastest)
                        writer.WriteRawValue(segment.Slice.Value.Bytes.Span, skipInputValidation: true);
                    }
                    else
                    {
                        writer.WriteNumberValue(0);
                    }
                    break;

                case AjisValueKind.String:
                    if (segment.Slice != null)
                    {
                        writer.WriteStringValue(segment.Slice.Value.Bytes.Span);
                    }
                    else
                    {
                        writer.WriteStringValue("");
                    }
                    break;
            }
            index++;
            return;
        }

        // Handle containers
        if (segment.Kind == AjisSegmentKind.EnterContainer && segment.ContainerKind.HasValue)
        {
            if (segment.ContainerKind.Value == AjisContainerKind.Array)
            {
                writer.WriteStartArray();
                index++; // Skip EnterContainer

                while (index < segments.Count && 
                       !(segments[index].Kind == AjisSegmentKind.ExitContainer && 
                         segments[index].ContainerKind == AjisContainerKind.Array))
                {
                    if (segments[index].Kind == AjisSegmentKind.Value || 
                        segments[index].Kind == AjisSegmentKind.EnterContainer)
                    {
                        WriteSegmentValue(segments, ref index, writer);
                    }
                    else
                    {
                        index++;
                    }
                }

                writer.WriteEndArray();
                index++; // Skip ExitContainer
            }
            else if (segment.ContainerKind.Value == AjisContainerKind.Object)
            {
                writer.WriteStartObject();
                index++; // Skip EnterContainer

                while (index < segments.Count && 
                       !(segments[index].Kind == AjisSegmentKind.ExitContainer && 
                         segments[index].ContainerKind == AjisContainerKind.Object))
                {
                    if (segments[index].Kind == AjisSegmentKind.PropertyName && segments[index].Slice != null)
                    {
                        // Write property name
                        writer.WritePropertyName(segments[index].Slice.Value.Bytes.Span);
                        index++; // Move to value

                        // Write property value
                        if (index < segments.Count)
                        {
                            WriteSegmentValue(segments, ref index, writer);
                        }
                    }
                    else
                    {
                        index++;
                    }
                }

                writer.WriteEndObject();
                index++; // Skip ExitContainer
            }
        }
    }

    /// <summary>
    /// Converts a .NET object to an AjisValue for serialization, with support for nested objects and collections.
    /// </summary>
    private AjisValue ObjectToAjisValue(object? obj, int depth)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException($"Maximum nesting depth ({MaxDepth}) exceeded.");

        if (obj == null)
            return AjisValue.Null();

        var type = obj.GetType();

        // Check for custom converter
        if (_customConverters.TryGetValue(type, out var customConverter))
        {
            var method = customConverter.GetType().GetMethod("Serialize", BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                var result = method.Invoke(customConverter, new[] { obj });
                return (AjisValue)result!;
            }
        }

        // Handle primitives
        if (obj is bool boolValue)
            return AjisValue.Bool(boolValue);

        if (obj is string stringValue)
            return AjisValue.String(stringValue);

        if (obj is int intValue)
            return AjisValue.Number(intValue.ToString());

        if (obj is long longValue)
            return AjisValue.Number(longValue.ToString());

        if (obj is double doubleValue)
            return AjisValue.Number(doubleValue.ToString("G17"));

        if (obj is decimal decimalValue)
            return AjisValue.Number(decimalValue.ToString());

        // Handle nullable value types
        if (obj is System.ValueType)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return ObjectToAjisValue(obj, depth + 1);
        }

        // Handle arrays and collections
        if (obj is IEnumerable enumerable && !(obj is string))
        {
            var items = new List<AjisValue>();
            foreach (var item in enumerable)
            {
                items.Add(ObjectToAjisValue(item, depth + 1));
            }
            return AjisValue.Array(items.ToArray());
        }

        // Handle objects with property mapping
        var properties = _propertyMapper.GetProperties(type);
        var pairs = new List<KeyValuePair<string, AjisValue>>();

        foreach (var metadata in properties)
        {
            if (metadata.IsIgnored)
                continue;

            var value = _propertyMapper.GetValue(obj, metadata);
            var ajisValue = ObjectToAjisValue(value, depth + 1);

            pairs.Add(new KeyValuePair<string, AjisValue>(metadata.AjisKey, ajisValue));
        }

        return AjisValue.Object(pairs.ToArray());
    }

    /// <summary>
    /// Context for deserialization operations, tracking path and state.
    /// </summary>
    private class DeserializationContext
    {
        private readonly AjisConverter<T> _converter;
        private readonly PropertyMapper _propertyMapper;

        public DeserializationContext(AjisConverter<T> converter, PropertyMapper propertyMapper)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
        }

        public object? DeserializeValue(Type targetType, List<AjisSegment> segments, int index, string path)
        {
            if (index >= segments.Count)
                throw new FormatException($"Path '{path}': Unexpected end of segments.");

            var segment = segments[index];

            if (segment.Kind == AjisSegmentKind.Value)
            {
                return segment.ValueKind switch
                {
                    AjisValueKind.Null => null,
                    AjisValueKind.Boolean => segment.Slice != null
                        ? Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span) == "true"
                        : false,
                    AjisValueKind.String => segment.Slice != null
                        ? Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span)
                        : "",
                    AjisValueKind.Number => segment.Slice != null
                        ? Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span)
                        : "0",
                    _ => throw new FormatException($"Path '{path}': Unknown value kind.")
                };
            }

            throw new FormatException($"Path '{path}': Unexpected segment kind {segment.Kind}.");
        }
    }
}

/// <summary>
/// Interface for custom type converters.
/// </summary>
/// <remarks>
/// Implement this interface to provide custom serialization/deserialization for specific types,
/// such as DateTime, Guid, or domain-specific types.
/// </remarks>
/// <typeparam name="T">The type this converter handles.</typeparam>
public interface ICustomAjisConverter<T> where T : notnull
{
    /// <summary>
    /// Converts an object to an AjisValue.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <returns>The AjisValue representation.</returns>
    AjisValue Serialize(T value);

    /// <summary>
    /// Converts an AjisValue to an object of type T.
    /// </summary>
    /// <param name="value">The AjisValue to convert.</param>
    /// <returns>The deserialized object.</returns>
    T? Deserialize(AjisValue value);
}
