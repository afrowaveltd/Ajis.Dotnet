#nullable enable

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Compiles property getters into fast delegates.
/// PHASE 6: JIT inlining for hot-path cache lookups.
/// </summary>
internal sealed class PropertyGetterCompiler
{
    private sealed class GetterCacheEntry
    {
        public Func<object, object?> Getter { get; }

        public GetterCacheEntry(Func<object, object?> getter)
        {
            Getter = getter;
        }
    }

    private readonly Dictionary<(Type, string), GetterCacheEntry> _getterCache = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets or compiles a fast property getter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Func<object, object?> GetOrCompileGetter(PropertyMetadata property)
    {
        var key = (property.Member.DeclaringType!, property.Member.Name);
        
        if (_getterCache.TryGetValue(key, out var cached))
            return cached.Getter;

        lock (_lock)
        {
            if (_getterCache.TryGetValue(key, out cached))
                return cached.Getter;

            var compiled = property.Member switch
            {
                PropertyInfo prop => CompilePropertyGetter(prop),
                FieldInfo field => CompileFieldGetter(field),
                _ => throw new InvalidOperationException($"Unsupported member type: {property.Member.GetType()}")
            };
            
            _getterCache[key] = new GetterCacheEntry(compiled);
            return compiled;
        }
    }

    private Func<object, object?> CompilePropertyGetter(PropertyInfo propertyInfo)
    {
        // (object instance) => (object)((TInstance)instance).Property
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instanceParam, propertyInfo.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, propertyInfo);
        var boxed = Expression.Convert(propertyAccess, typeof(object));
        
        return Expression.Lambda<Func<object, object?>>(boxed, instanceParam).Compile();
    }
    
    private Func<object, object?> CompileFieldGetter(FieldInfo fieldInfo)
    {
        // (object instance) => (object)((TInstance)instance).Field
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instanceParam, fieldInfo.DeclaringType!);
        var fieldAccess = Expression.Field(typedInstance, fieldInfo);
        var boxed = Expression.Convert(fieldAccess, typeof(object));
        
        return Expression.Lambda<Func<object, object?>>(boxed, instanceParam).Compile();
    }
}
