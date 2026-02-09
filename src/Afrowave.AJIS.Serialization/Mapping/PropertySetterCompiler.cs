#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Optimized property setter compiler using expression trees with aggressive caching.
/// PHASE 6: JIT inlining hints for hot-path methods.
/// </summary>
internal sealed class PropertySetterCompiler
{
    // Highly optimized cache structure
    private sealed class SetterCacheEntry
    {
        public Action<object, object?> Setter { get; }
        public int HitCount { get; set; }

        public SetterCacheEntry(Action<object, object?> setter)
        {
            Setter = setter;
            HitCount = 0;
        }
    }

    private readonly Dictionary<(Type, string), SetterCacheEntry> _setterCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets or compiles a fast property setter.
    /// Compiled setters are permanently cached and reused.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<object, object?> GetOrCompileSetter(PropertyMetadata property)
    {
        var key = (property.Member.DeclaringType!, property.Member.Name);
        
        // Fast path - check cache without lock first
        if (_setterCache.TryGetValue(key, out var entry))
        {
            entry.HitCount++;
            return entry.Setter;
        }

        // Slow path - compile and cache
        lock (_lock)
        {
            if (_setterCache.TryGetValue(key, out entry))
            {
                entry.HitCount++;
                return entry.Setter;
            }

            var setter = CompileSetter(property);
            _setterCache[key] = new SetterCacheEntry(setter);
            return setter;
        }
    }

    private Action<object, object?> CompileSetter(PropertyMetadata property)
    {
        if (property.Member is PropertyInfo propInfo)
        {
            return CompilePropertySetter(propInfo);
        }
        else if (property.Member is FieldInfo fieldInfo)
        {
            return CompileFieldSetter(fieldInfo);
        }

        throw new InvalidOperationException($"Unsupported member type: {property.Member.GetType()}");
    }

    private Action<object, object?> CompilePropertySetter(PropertyInfo propInfo)
    {
        var declaringType = propInfo.DeclaringType!;
        var propertyType = propInfo.PropertyType;
        
        var objParam = Expression.Parameter(typeof(object), "obj");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // Convert object to declaring type
        var objCast = Expression.Convert(objParam, declaringType);
        
        // Convert value to property type
        var valueCast = Expression.Convert(valueParam, propertyType);
        
        // Property access and assignment
        var propertyAccess = Expression.Property(objCast, propInfo);
        var assignment = Expression.Assign(propertyAccess, valueCast);

        // Build lambda and compile
        var lambda = Expression.Lambda<Action<object, object?>>(
            assignment,
            objParam,
            valueParam
        );

        return lambda.Compile();
    }

    private Action<object, object?> CompileFieldSetter(FieldInfo fieldInfo)
    {
        var declaringType = fieldInfo.DeclaringType!;
        var fieldType = fieldInfo.FieldType;

        var objParam = Expression.Parameter(typeof(object), "obj");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var objCast = Expression.Convert(objParam, declaringType);
        var valueCast = Expression.Convert(valueParam, fieldType);
        var fieldAccess = Expression.Field(objCast, fieldInfo);
        var assignment = Expression.Assign(fieldAccess, valueCast);

        var lambda = Expression.Lambda<Action<object, object?>>(
            assignment,
            objParam,
            valueParam
        );

        return lambda.Compile();
    }

    /// <summary>
    /// Gets cached statistics (for profiling purposes only).
    /// </summary>
    public int GetCachedSetterCount()
    {
        return _setterCache.Count;
    }
}
