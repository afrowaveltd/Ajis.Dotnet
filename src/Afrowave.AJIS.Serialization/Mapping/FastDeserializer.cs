#nullable enable

using System.Buffers;
using System.Buffers.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Afrowave.AJIS.Streaming.Segments;
using System.Collections.Concurrent;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Fast deserializer that converts AJIS segments directly to objects.
/// PHASE 3: Compiled delegates + Span optimizations for maximum performance.
/// PHASE 9: Global frozen caches + pooled buffers for zero-allocation performance.
/// </summary>
/// <typeparam name="T">Target type to deserialize to</typeparam>
internal sealed class FastDeserializer<T> where T : notnull
{
    private readonly PropertyMapper _propertyMapper;
    private readonly PropertySetterCompiler _setterCompiler = new();
    // PHASE 9: Global constructor cache (ConcurrentDictionary for thread safety)
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> s_constructorCache = new();
    
    // PHASE 9: Object pools for instance reuse
    private static readonly ConcurrentDictionary<Type, object> s_objectPools = new();

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
                    // PHASE 9: Parse directly from Span without string allocation for known types
                    var valueSpan = segment.Slice.Value.Bytes.Span;
                    
                    if (targetType == typeof(Guid))
                    {
                        if (Guid.TryParse(valueSpan, out var guid))
                            return guid;
                        // Fallback
                        var str = Encoding.UTF8.GetString(valueSpan);
                        return Guid.Parse(str);
                    }
                    
                    if (targetType == typeof(DateTime))
                    {
                        var str = Encoding.UTF8.GetString(valueSpan);
                        if (DateTime.TryParse(str, out var dt))
                            return dt;
                        return DateTime.MinValue;
                    }
                    
                    if (targetType == typeof(DateTimeOffset))
                    {
                        var str = Encoding.UTF8.GetString(valueSpan);
                        if (DateTimeOffset.TryParse(str, out var dto))
                            return dto;
                        return DateTimeOffset.MinValue;
                    }
                    
                    if (targetType == typeof(TimeSpan))
                    {
                        var str = Encoding.UTF8.GetString(valueSpan);
                        if (TimeSpan.TryParse(str, out var ts))
                            return ts;
                        return TimeSpan.Zero;
                    }
                    
                    // For string target type, only allocate if necessary
                    if (targetType == typeof(string))
                    {
                        return Encoding.UTF8.GetString(valueSpan);
                    }
                    
                    // For other types, parse from span
                    var stringValue = Encoding.UTF8.GetString(valueSpan);
                    return Convert.ChangeType(stringValue, targetType);
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

        // PHASE 9: Type-specific paths to avoid boxing
        if (elementType == typeof(int))
            return DeserializeArrayTyped<int>(segments, ref index, targetType);
        if (elementType == typeof(string))
            return DeserializeArrayTyped<string>(segments, ref index, targetType);
        if (elementType == typeof(long))
            return DeserializeArrayTyped<long>(segments, ref index, targetType);
        if (elementType == typeof(double))
            return DeserializeArrayTyped<double>(segments, ref index, targetType);
        if (elementType == typeof(bool))
            return DeserializeArrayTyped<bool>(segments, ref index, targetType);

        // Fallback: Generic path with boxing
        return DeserializeArrayGeneric(segments, ref index, elementType, targetType);
    }

    private object? DeserializeObject(Type targetType, List<AjisSegment> segments, ref int index)
    {
        index++; // Skip EnterContainer

        // Create instance
        var ctor = s_constructorCache.GetOrAdd(targetType, t =>
        {
            var c = t.GetConstructor(Type.EmptyTypes);
            if (c == null)
                throw new InvalidOperationException($"Type {t} must have a parameterless constructor");
            return c;
        });
        
        // Try to get from pool first
        object instance;
        var pool = GetObjectPool(targetType);
        if (pool != null)
        {
            instance = ((dynamic)pool).Get();
        }
        else
        {
            instance = ctor.Invoke(null);
        }

        // Get properties and matcher
        var properties = GlobalPropertyCache.GetProperties(targetType, _propertyMapper);
        var matcher = new SpanPropertyMatcher(properties);

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

    private object? DeserializeArrayGeneric(List<AjisSegment> segments, ref int index, Type elementType, Type targetType)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? DeserializeArrayTyped<T>(List<AjisSegment> segments, ref int index, Type targetType)
    {
        var list = new List<T>(capacity: 16);

        while (index < segments.Count && 
               !(segments[index].Kind == AjisSegmentKind.ExitContainer && 
                 segments[index].ContainerKind == AjisContainerKind.Array))
        {
            if (segments[index].Kind == AjisSegmentKind.Value || 
                segments[index].Kind == AjisSegmentKind.EnterContainer)
            {
                var item = DeserializeValue(typeof(T), segments, ref index);
                list.Add((T)item!);
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
            return list.ToArray();
        }
        else
        {
            // Create List<T>
            return list;
        }
    }

    private static object? GetObjectPool(Type type)
    {
        return s_objectPools.GetOrAdd(type, t =>
        {
            try
            {
                var poolType = typeof(SimpleObjectPool<>).MakeGenericType(t);
                return Activator.CreateInstance(poolType);
            }
            catch
            {
                return null; // Can't create pool for this type
            }
        });
    }
}

/// <summary>
/// Simple object pool for instance reuse.
/// </summary>
internal class SimpleObjectPool<T> where T : class, new()
{
    private readonly object _lock = new();
    private readonly Stack<T> _pool = new();

    public T Get()
    {
        lock (_lock)
        {
            return _pool.Count > 0 ? _pool.Pop() : new T();
        }
    }

    public void Return(T obj)
    {
        if (obj == null) return;
        lock (_lock)
        {
            if (_pool.Count < 10) // Limit pool size
                _pool.Push(obj);
        }
    }
}
