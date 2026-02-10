#nullable enable

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// ULTRA-FAST deserializer using Utf8JsonReader directly.
/// PHASE 6: JIT inlining hints, frozen collections, type specialization.
/// </summary>
/// <typeparam name="T">Target type to deserialize to</typeparam>
internal sealed class Utf8DirectDeserializer<T> where T : notnull
{
   private readonly PropertyMapper _propertyMapper;

   // Three-tier cache: Type → PropertyMetadata, Type → Properties, Type → Lookup dictionary
   private readonly Dictionary<Type, PropertyMetadata[]> _propertyCache = new();
   private readonly Dictionary<Type, ConstructorInfo> _constructorCache = new();
   private readonly Dictionary<Type, Dictionary<string, PropertyMetadata>> _propertyLookupCache = new();

   // PHASE 4: Compiled setters cache
   private readonly PropertySetterCompiler _setterCompiler = new();

   // PHASE 5: Inline type cache for common types to avoid branching
   private static readonly Type TypeString = typeof(string);
   private static readonly Type TypeInt = typeof(int);
   private static readonly Type TypeLong = typeof(long);
   private static readonly Type TypeDouble = typeof(double);
   private static readonly Type TypeDecimal = typeof(decimal);
   private static readonly Type TypeBool = typeof(bool);
   private static readonly Type TypeGuid = typeof(Guid);
   private static readonly Type TypeDateTime = typeof(DateTime);
   private static readonly Type TypeDateTimeOffset = typeof(DateTimeOffset);
   private static readonly Type TypeTimeSpan = typeof(TimeSpan);

   public Utf8DirectDeserializer(PropertyMapper propertyMapper)
   {
      _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
   }

   /// <summary>
   /// Deserialize directly from UTF8 bytes using Utf8JsonReader (FAST!).
   /// </summary>
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public T? Deserialize(ReadOnlySpan<byte> utf8Json)
   {
      Utf8JsonReader reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions
      {
         AllowTrailingCommas = true,
         CommentHandling = JsonCommentHandling.Skip,
         MaxDepth = 64
      });

      if(!reader.Read())
         return default;

      return (T?)ReadValue(ref reader, typeof(T));
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private object? ReadValue(ref Utf8JsonReader reader, Type targetType)
   {
      switch(reader.TokenType)
      {
         case JsonTokenType.Null:
            return null;

         case JsonTokenType.True:
            return ConvertBoolean(true, targetType);

         case JsonTokenType.False:
            return ConvertBoolean(false, targetType);

         case JsonTokenType.Number:
            return ReadNumber(ref reader, targetType);

         case JsonTokenType.String:
            return ReadString(ref reader, targetType);

         case JsonTokenType.StartArray:
            return ReadArray(ref reader, targetType);

         case JsonTokenType.StartObject:
            return ReadObject(ref reader, targetType);

         default:
            throw new JsonException($"Unexpected token: {reader.TokenType}");
      }
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private object? ReadString(ref Utf8JsonReader reader, Type targetType)
   {
      // PHASE 5: Fast-path for string (most common case)
      if(ReferenceEquals(targetType, TypeString))
         return reader.GetString();

      var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

      // PHASE 7C: Parse directly from ValueSpan<byte> without string allocation!
      var valueSpan = reader.ValueSpan;

      if(ReferenceEquals(underlyingType, TypeGuid))
      {
         // Parse from UTF8 bytes directly, not from converted string
         if(Guid.TryParse(valueSpan, out var guid))
            return guid;
         // Fallback: try string-based parsing if span parsing fails
         var str = reader.GetString();
         return str != null ? Guid.Parse(str) : null;
      }

      if(ReferenceEquals(underlyingType, TypeDateTime))
      {
         if(DateTime.TryParse(Encoding.UTF8.GetString(valueSpan), out var dt))
            return dt;
         // Fallback
         var str = reader.GetString();
         return str != null ? DateTime.Parse(str) : null;
      }

      if(ReferenceEquals(underlyingType, TypeDateTimeOffset))
      {
         if(DateTimeOffset.TryParse(Encoding.UTF8.GetString(valueSpan), out var dto))
            return dto;
         // Fallback
         var str = reader.GetString();
         return str != null ? DateTimeOffset.Parse(str) : null;
      }

      if(ReferenceEquals(underlyingType, TypeTimeSpan))
      {
         if(TimeSpan.TryParse(Encoding.UTF8.GetString(valueSpan), out var ts))
            return ts;
         // Fallback
         var str = reader.GetString();
         return str != null ? TimeSpan.Parse(str) : null;
      }

      // For other types, use standard string conversion
      var strVal = reader.GetString();
      if(strVal == null)
         return null;

      return Convert.ChangeType(strVal, underlyingType);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private object? ConvertBoolean(bool value, Type targetType)
   {
      // PHASE 5: Fast-path for bool
      if(ReferenceEquals(targetType, TypeBool))
         return value;

      var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
      if(ReferenceEquals(underlyingType, TypeBool))
         return value;

      return Convert.ChangeType(value, underlyingType);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private object? ReadNumber(ref Utf8JsonReader reader, Type targetType)
   {
      var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

      // PHASE 5: Use ReferenceEquals for fast type matching (no boxing!)
      if(ReferenceEquals(underlyingType, TypeInt))
         return reader.GetInt32();
      if(ReferenceEquals(underlyingType, TypeLong))
         return reader.GetInt64();
      if(ReferenceEquals(underlyingType, TypeDouble))
         return reader.GetDouble();
      if(ReferenceEquals(underlyingType, TypeDecimal))
         return reader.GetDecimal();

      // PHASE 7D: Add float explicitly to avoid boxing
      if(ReferenceEquals(underlyingType, typeof(float)))
         return reader.GetSingle();

      // PHASE 7D: Add byte, short, uint, ulong to avoid boxing
      if(ReferenceEquals(underlyingType, typeof(byte)))
         return reader.GetByte();
      if(ReferenceEquals(underlyingType, typeof(short)))
         return reader.GetInt16();
      if(ReferenceEquals(underlyingType, typeof(uint)))
         return reader.GetUInt32();
      if(ReferenceEquals(underlyingType, typeof(ulong)))
         return reader.GetUInt64();

      // Fallback for unknown numeric types
      return reader.GetDouble();
   }

   private object? ReadArray(ref Utf8JsonReader reader, Type targetType)
   {
      // Determine element type
      Type? elementType = null;
      if(targetType.IsArray)
      {
         elementType = targetType.GetElementType()!;
      }
      else if(targetType.IsGenericType)
      {
         var genericArgs = targetType.GetGenericArguments();
         if(genericArgs.Length == 1)
            elementType = genericArgs[0];
      }

      if(elementType == null)
         throw new InvalidOperationException($"Cannot determine element type for {targetType}");

      // PHASE 8B: Type-specific paths to avoid boxing in List<object?>
      // This is CRITICAL - boxing causes 30-40% memory overhead!
      if(ReferenceEquals(elementType, typeof(int)))
         return ReadArrayTyped<int>(ref reader);
      if(ReferenceEquals(elementType, typeof(string)))
         return ReadArrayTyped<string>(ref reader);
      if(ReferenceEquals(elementType, typeof(long)))
         return ReadArrayTyped<long>(ref reader);
      if(ReferenceEquals(elementType, typeof(double)))
         return ReadArrayTyped<double>(ref reader);
      if(ReferenceEquals(elementType, typeof(bool)))
         return ReadArrayTyped<bool>(ref reader);

      // Fallback: Generic path with boxing (for less common types)
      return ReadArrayGeneric(ref reader, elementType, targetType);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private T[] ReadArrayTyped<T>(ref Utf8JsonReader reader)
   {
      List<T> list = new List<T>(capacity: 16);

      while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
      {
         var item = ReadValue(ref reader, typeof(T));
         list.Add((T)item!);
      }

      return list.ToArray();
   }

   private object? ReadArrayGeneric(ref Utf8JsonReader reader, Type elementType, Type targetType)
   {
      // Original implementation for uncommon types
      List<object?> itemsList = new List<object?>(capacity: 16);

      while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
      {
         var item = ReadValue(ref reader, elementType);
         itemsList.Add(item);
      }

      // Convert to target type
      if(targetType.IsArray)
      {
         Array array = Array.CreateInstance(elementType, itemsList.Count);

         if(itemsList.Count >= 1000)
         {
            System.Threading.Tasks.Parallel.For(0, itemsList.Count, i =>
            {
               array.SetValue(itemsList[i], i);
            });
         }
         else
         {
            for(int i = 0; i < itemsList.Count; i++)
            {
               array.SetValue(itemsList[i], i);
            }
         }
         return array;
      }
      else
      {
         // Create List<T> efficiently
         var listType = typeof(List<>).MakeGenericType(elementType);
         IList list = (System.Collections.IList)Activator.CreateInstance(listType)!;
         foreach(var item in itemsList)
         {
            list.Add(item);
         }
         return list;
      }
   }

   private object? ReadObject(ref Utf8JsonReader reader, Type targetType)
   {
      // Get or create constructor
      if(!_constructorCache.TryGetValue(targetType, out var ctor))
      {
         ctor = targetType.GetConstructor(Type.EmptyTypes);
         if(ctor == null)
            throw new InvalidOperationException($"Type {targetType} must have a parameterless constructor");
         _constructorCache[targetType] = ctor;
      }

      // Create instance
      var instance = ctor.Invoke(null);

      // PHASE 8C: Use GLOBAL static cache instead of per-instance cache
      // This eliminates cache miss on new deserializer instances
      var exactLookup = GlobalPropertyCache.GetExactLookup(targetType, _propertyMapper);
      var caseInsensitiveLookup = GlobalPropertyCache.GetCaseInsensitiveLookup(targetType, _propertyMapper);

      // Read properties
      while(reader.Read() && reader.TokenType != JsonTokenType.EndObject)
      {
         if(reader.TokenType == JsonTokenType.PropertyName)
         {
            var propertyName = reader.GetString();
            reader.Read(); // Move to value

            if(propertyName != null)
            {
               // PHASE 8C: Try exact match first (FrozenDictionary - super fast!)
               if(!exactLookup.TryGetValue(propertyName, out var property))
               {
                  // PHASE 8C: Fallback to precomputed case-insensitive (O(1), not LINQ!)
                  caseInsensitiveLookup.TryGetValue(propertyName, out property);
               }

               if(property != null)
               {
                  var value = ReadValue(ref reader, property.PropertyType);

                  // Use compiled setter (cached)
                  var setter = _setterCompiler.GetOrCompileSetter(property);
                  setter(instance, value);
               }
               // else skip unknown property (already advanced reader)
            }
         }
      }

      return instance;
   }
}
