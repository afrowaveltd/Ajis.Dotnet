#nullable enable

using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// ULTRA-FAST serializer using Utf8JsonWriter directly.
/// PHASE 6: JIT inlining hints, compiled getters, type specialization.
/// </summary>
/// <typeparam name="T">Source type to serialize</typeparam>
internal sealed class Utf8DirectSerializer<T> where T : notnull
{
    private readonly PropertyMapper _propertyMapper;
    private readonly PropertyGetterCompiler _getterCompiler = new();
    private readonly Dictionary<Type, PropertyMetadata[]> _propertyCache = new();
    private const int MaxDepth = 100;
    private const int InitialBufferSize = 64 * 1024; // 64KB initial buffer

    // PHASE 5: Inline type cache for fast type matching
    private static readonly Type TypeString = typeof(string);
    private static readonly Type TypeBool = typeof(bool);
    private static readonly Type TypeInt = typeof(int);
    private static readonly Type TypeLong = typeof(long);
    private static readonly Type TypeDouble = typeof(double);
    private static readonly Type TypeDecimal = typeof(decimal);
    private static readonly Type TypeFloat = typeof(float);
    private static readonly Type TypeByte = typeof(byte);
    private static readonly Type TypeShort = typeof(short);
    private static readonly Type TypeUInt = typeof(uint);
    private static readonly Type TypeULong = typeof(ulong);

    public Utf8DirectSerializer(PropertyMapper propertyMapper)
    {
        _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
    }

    /// <summary>
    /// Serialize directly to UTF8 using Utf8JsonWriter (FAST!).
    /// PHASE 6: Uses ArrayBufferWriter and aggressive inlining.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string Serialize(T value)
    {
        if (value == null)
            return "null";

        // Use ArrayBufferWriter instead of MemoryStream - much faster for UTF8
        var bufferWriter = new ArrayBufferWriter<byte>(InitialBufferSize);
        
        using (var writer = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        }))
        {
            WriteValue(writer, value, typeof(T), 0);
            writer.Flush();
        }

        // Convert buffer span directly to string (no intermediate MemoryStream)
        return Encoding.UTF8.GetString(bufferWriter.WrittenSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteValue(Utf8JsonWriter writer, object? value, Type type, int depth)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException($"Maximum depth ({MaxDepth}) exceeded");

        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var actualType = value.GetType();

        // PHASE 5: Fast-path primitives using ReferenceEquals
        if (ReferenceEquals(actualType, TypeBool))
        {
            writer.WriteBooleanValue((bool)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeString))
        {
            writer.WriteStringValue((string)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeInt))
        {
            writer.WriteNumberValue((int)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeLong))
        {
            writer.WriteNumberValue((long)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeDouble))
        {
            writer.WriteNumberValue((double)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeDecimal))
        {
            writer.WriteNumberValue((decimal)value);
            return;
        }

        if (ReferenceEquals(actualType, TypeFloat))
        {
            writer.WriteNumberValue((float)value);
            return;
        }

        // Less common types
        if (actualType == TypeByte)
        {
            writer.WriteNumberValue((byte)value);
            return;
        }

        if (actualType == TypeShort)
        {
            writer.WriteNumberValue((short)value);
            return;
        }

        if (actualType == TypeUInt)
        {
            writer.WriteNumberValue((uint)value);
            return;
        }

        if (actualType == TypeULong)
        {
            writer.WriteNumberValue((ulong)value);
            return;
        }

        // Collections (NOT string!)
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            writer.WriteStartArray();
            foreach (var item in enumerable)
            {
                WriteValue(writer, item, item?.GetType() ?? typeof(object), depth + 1);
            }
            writer.WriteEndArray();
            return;
        }

        // Objects - use cached metadata and compiled getters
        writer.WriteStartObject();

        if (!_propertyCache.TryGetValue(actualType, out var properties))
        {
            properties = _propertyMapper.GetProperties(actualType).ToArray();
            _propertyCache[actualType] = properties;
        }

        foreach (var property in properties)
        {
            if (property.IsIgnored)
                continue;

            // PHASE 5: Use compiled getter instead of reflection
            var getter = _getterCompiler.GetOrCompileGetter(property);
            var propValue = getter(value);
            
            writer.WritePropertyName(property.AjisKey);
            WriteValue(writer, propValue, property.PropertyType, depth + 1);
        }

        writer.WriteEndObject();
    }
}
