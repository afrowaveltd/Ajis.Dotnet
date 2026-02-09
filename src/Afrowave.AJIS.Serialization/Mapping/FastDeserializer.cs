#nullable enable

using System.Buffers;
using System.Buffers.Text;
using System.Reflection;
using System.Text;
using Afrowave.AJIS.Streaming.Segments;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Fast deserializer that converts AJIS segments directly to objects.
/// PHASE 3: Compiled delegates + Span optimizations for maximum performance.
/// </summary>
/// <typeparam name="T">Target type to deserialize to</typeparam>
internal sealed class FastDeserializer<T> where T : notnull
{
    private readonly PropertyMapper _propertyMapper;
    private readonly Dictionary<Type, PropertyMetadata[]> _propertyCache = new();
    private readonly Dictionary<Type, ConstructorInfo> _constructorCache = new();
    private readonly Dictionary<Type, SpanPropertyMatcher> _matcherCache = new();
    private readonly PropertySetterCompiler _setterCompiler = new();

    public FastDeserializer(PropertyMapper propertyMapper)
    {
        _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
    }

    /// <summary>
    /// Deserializes segments directly to object (no JSON intermediate).
    /// </summary>
    public T? Deserialize(List<AjisSegment> segments)
    {
        if (segments.Count == 0)
            return default;

        int index = 0;
        return (T?)DeserializeValue(typeof(T), segments, ref index);
    }

    private object? DeserializeValue(Type targetType, List<AjisSegment> segments, ref int index)
    {
        if (index >= segments.Count)
            return null;

        var segment = segments[index];

        // Handle primitive values
        if (segment.Kind == AjisSegmentKind.Value && segment.ValueKind.HasValue)
        {
            index++;
            return ConvertPrimitiveValue(segment, targetType);
        }

        // Handle containers
        if (segment.Kind == AjisSegmentKind.EnterContainer && segment.ContainerKind.HasValue)
        {
            if (segment.ContainerKind.Value == AjisContainerKind.Array)
            {
                return DeserializeArray(targetType, segments, ref index);
            }
            else if (segment.ContainerKind.Value == AjisContainerKind.Object)
            {
                return DeserializeObject(targetType, segments, ref index);
            }
        }

        index++;
        return null;
    }

    private object? ConvertPrimitiveValue(AjisSegment segment, Type targetType)
    {
        if (!segment.ValueKind.HasValue)
            return null;

        switch (segment.ValueKind.Value)
        {
            case AjisValueKind.Null:
                return null;

            case AjisValueKind.Boolean:
                if (segment.Slice != null)
                {
                    // PHASE 3: Zero-allocation boolean parsing
                    var isTrue = segment.Slice.Value.Bytes.Span.SequenceEqual("true"u8);
                    return ConvertBoolean(isTrue, targetType);
                }
                return false;

            case AjisValueKind.Number:
                if (segment.Slice != null)
                {
                    // PHASE 3: Span-based number parsing (zero allocation where possible)
                    return ParseNumber(segment.Slice.Value.Bytes.Span, targetType);
                }
                return GetDefaultValue(targetType);

            case AjisValueKind.String:
                if (segment.Slice != null)
                {
                    // PHASE 3: Only allocate string if target type is string
                    if (targetType == typeof(string))
                    {
                        return Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);
                    }
                    // For other types, parse from span
                    var str = Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);
                    return Convert.ChangeType(str, targetType);
                }
                return targetType == typeof(string) ? "" : null;

            default:
                return null;
        }
    }

    private object? ConvertBoolean(bool value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        if (underlyingType == typeof(bool))
            return value;
        
        return Convert.ChangeType(value, underlyingType);
    }

    private object? ParseNumber(ReadOnlySpan<byte> numberBytes, Type targetType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            targetType = underlyingType;

        // PHASE 3: Use Utf8Parser for zero-allocation parsing where possible
        // For .NET 8+, many types support Span<byte> parsing directly
        
        // Fast path for common integer types
        if (targetType == typeof(int))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out int result, out _))
                return result;
        }
        else if (targetType == typeof(long))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out long result, out _))
                return result;
        }
        else if (targetType == typeof(double))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out double result, out _))
                return result;
        }
        else if (targetType == typeof(decimal))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out decimal result, out _))
                return result;
        }
        else if (targetType == typeof(float))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out float result, out _))
                return result;
        }
        else if (targetType == typeof(byte))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out byte result, out _))
                return result;
        }
        else if (targetType == typeof(short))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out short result, out _))
                return result;
        }
        else if (targetType == typeof(uint))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out uint result, out _))
                return result;
        }
        else if (targetType == typeof(ulong))
        {
            if (System.Buffers.Text.Utf8Parser.TryParse(numberBytes, out ulong result, out _))
                return result;
        }

        // Fallback: allocate string and parse
        var numberStr = Encoding.UTF8.GetString(numberBytes);
        return Convert.ChangeType(numberStr, targetType);
    }

    private object? DeserializeArray(Type targetType, List<AjisSegment> segments, ref int index)
    {
        index++; // Skip EnterContainer

        // Determine element type
        Type? elementType = null;
        if (targetType.IsArray)
        {
            elementType = targetType.GetElementType()!;
        }
        else if (targetType.IsGenericType)
        {
            var genericArgs = targetType.GetGenericArguments();
            if (genericArgs.Length == 1)
                elementType = genericArgs[0];
        }

        if (elementType == null)
            throw new InvalidOperationException($"Cannot determine element type for {targetType}");

        var items = new List<object?>();

        while (index < segments.Count && 
               !(segments[index].Kind == AjisSegmentKind.ExitContainer && 
                 segments[index].ContainerKind == AjisContainerKind.Array))
        {
            if (segments[index].Kind == AjisSegmentKind.Value || 
                segments[index].Kind == AjisSegmentKind.EnterContainer)
            {
                var item = DeserializeValue(elementType, segments, ref index);
                items.Add(item);
            }
            else
            {
                index++;
            }
        }

        index++; // Skip ExitContainer

        // Convert to target type
        if (targetType.IsArray)
        {
            var array = Array.CreateInstance(elementType, items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                array.SetValue(items[i], i);
            }
            return array;
        }
        else
        {
            // Create List<T>
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (var item in items)
            {
                list.Add(item);
            }
            return list;
        }
    }

    private object? DeserializeObject(Type targetType, List<AjisSegment> segments, ref int index)
    {
        index++; // Skip EnterContainer

        // Get or create constructor
        if (!_constructorCache.TryGetValue(targetType, out var ctor))
        {
            ctor = targetType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"Type {targetType} must have a parameterless constructor");
            _constructorCache[targetType] = ctor;
        }

        // Create instance
        var instance = ctor.Invoke(null);

        // Get properties and matcher
        if (!_propertyCache.TryGetValue(targetType, out var properties))
        {
            properties = _propertyMapper.GetProperties(targetType).ToArray();
            _propertyCache[targetType] = properties;
        }

        if (!_matcherCache.TryGetValue(targetType, out var matcher))
        {
            matcher = new SpanPropertyMatcher(properties);
            _matcherCache[targetType] = matcher;
        }

        // Read properties
        while (index < segments.Count && 
               !(segments[index].Kind == AjisSegmentKind.ExitContainer && 
                 segments[index].ContainerKind == AjisContainerKind.Object))
        {
            if (segments[index].Kind == AjisSegmentKind.PropertyName && segments[index].Slice != null)
            {
                // PHASE 3: Zero-allocation property lookup using Span
                var propertyNameBytes = segments[index].Slice.Value.Bytes.Span;
                var property = matcher.FindProperty(propertyNameBytes);
                
                index++; // Move to value

                if (property != null && index < segments.Count)
                {
                    var value = DeserializeValue(property.PropertyType, segments, ref index);
                    
                    // PHASE 3: Use compiled setter (no reflection!)
                    var setter = _setterCompiler.GetOrCompileSetter(property);
                    setter(instance, value);
                }
                else
                {
                    // Skip unknown property
                    SkipValue(segments, ref index);
                }
            }
            else
            {
                index++;
            }
        }

        index++; // Skip ExitContainer
        return instance;
    }

    private void SkipValue(List<AjisSegment> segments, ref int index)
    {
        if (index >= segments.Count)
            return;

        var segment = segments[index];

        if (segment.Kind == AjisSegmentKind.Value)
        {
            index++;
            return;
        }

        if (segment.Kind == AjisSegmentKind.EnterContainer)
        {
            int depth = 1;
            index++;
            while (index < segments.Count && depth > 0)
            {
                if (segments[index].Kind == AjisSegmentKind.EnterContainer)
                    depth++;
                else if (segments[index].Kind == AjisSegmentKind.ExitContainer)
                    depth--;
                index++;
            }
            return;
        }

        index++;
    }

    private object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }
}
