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
   private sealed class SetterCacheEntry(Action<object, object?> setter)
   {
      public Action<object, object?> Setter { get; } = setter;
      public int HitCount { get; set; } = 0;
   }

   private readonly Dictionary<(Type, string), SetterCacheEntry> _setterCache = [];
   private readonly Lock _lock = new();

   /// <summary>
   /// Gets or compiles a fast property setter.
   /// Compiled setters are permanently cached and reused.
   /// </summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public Action<object, object?> GetOrCompileSetter(PropertyMetadata property)
   {
      (Type, string Name) key = (property.Member.DeclaringType!, property.Member.Name);

      // Fast path - check cache without lock first
      if(_setterCache.TryGetValue(key, out SetterCacheEntry? entry))
      {
         entry.HitCount++;
         return entry.Setter;
      }

      // Slow path - compile and cache
      lock(_lock)
      {
         if(_setterCache.TryGetValue(key, out entry))
         {
            entry.HitCount++;
            return entry.Setter;
         }

         Action<object, object?> setter = CompileSetter(property);
         _setterCache[key] = new SetterCacheEntry(setter);
         return setter;
      }
   }

   private Action<object, object?> CompileSetter(PropertyMetadata property)
   {
      if(property.Member is PropertyInfo propInfo)
      {
         return CompilePropertySetter(propInfo);
      }
      else if(property.Member is FieldInfo fieldInfo)
      {
         return CompileFieldSetter(fieldInfo);
      }

      throw new InvalidOperationException($"Unsupported member type: {property.Member.GetType()}");
   }

   private Action<object, object?> CompilePropertySetter(PropertyInfo propInfo)
   {
      Type declaringType = propInfo.DeclaringType!;
      Type propertyType = propInfo.PropertyType;

      ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
      ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

      // Convert object to declaring type
      UnaryExpression objCast = Expression.Convert(objParam, declaringType);

      // Convert value to property type
      UnaryExpression valueCast = Expression.Convert(valueParam, propertyType);

      // Property access and assignment
      MemberExpression propertyAccess = Expression.Property(objCast, propInfo);
      BinaryExpression assignment = Expression.Assign(propertyAccess, valueCast);

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
      Type declaringType = fieldInfo.DeclaringType!;
      Type fieldType = fieldInfo.FieldType;

      ParameterExpression objParam = Expression.Parameter(typeof(object), "obj");
      ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

      UnaryExpression objCast = Expression.Convert(objParam, declaringType);
      UnaryExpression valueCast = Expression.Convert(valueParam, fieldType);
      MemberExpression fieldAccess = Expression.Field(objCast, fieldInfo);
      BinaryExpression assignment = Expression.Assign(fieldAccess, valueCast);

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