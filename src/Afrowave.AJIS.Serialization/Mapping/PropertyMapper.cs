#nullable enable

using System.Collections.Concurrent;
using System.Reflection;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Metadata for a single property including reflection info and attribute overrides.
/// </summary>
/// <remarks>
/// Cached metadata for efficient property access during serialization/deserialization.
/// </remarks>
/// <remarks>
/// Initializes metadata for a property member.
/// </remarks>
internal sealed class PropertyMetadata(
    MemberInfo member,
    string ajisKey,
    bool isIgnored,
    bool isRequired,
    AjisNumberStyle? numberStyle,
    Type propertyType)
{
   /// <summary>
   /// Gets the .NET property or field information.
   /// </summary>
   public MemberInfo Member { get; } = member ?? throw new ArgumentNullException(nameof(member));

   /// <summary>
   /// Gets the AJIS key name (after applying naming policy or [AjisPropertyName] override).
   /// </summary>
   public string AjisKey { get; } = ajisKey ?? throw new ArgumentNullException(nameof(ajisKey));

   /// <summary>
   /// Gets whether this property should be ignored during mapping.
   /// </summary>
   public bool IsIgnored { get; } = isIgnored;

   /// <summary>
   /// Gets whether this property is required (must not be null).
   /// </summary>
   public bool IsRequired { get; } = isRequired;

   /// <summary>
   /// Gets the custom number format, if any.
   /// </summary>
   public AjisNumberStyle? NumberStyle { get; } = numberStyle;

   /// <summary>
   /// Gets the property type.
   /// </summary>
   public Type PropertyType { get; } = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
}

/// <summary>
/// Efficiently discovers and caches property metadata for types.
/// </summary>
/// <remarks>
/// <para>
/// PropertyMapper uses reflection with caching to discover properties and apply
/// attribute-based overrides (like [AjisPropertyName], [AjisIgnore], etc.).
/// </para>
/// <para>
/// Metadata is cached per type to avoid repeated reflection work during
/// serialization/deserialization of multiple objects.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new PropertyMapper with a naming policy.
/// </remarks>
/// <param name="namingPolicy">The naming policy for property name conversion.</param>
internal sealed class PropertyMapper(INamingPolicy namingPolicy)
{
   private readonly INamingPolicy _namingPolicy = namingPolicy ?? throw new ArgumentNullException(nameof(namingPolicy));
   private readonly ConcurrentDictionary<Type, PropertyMetadata[]> _cache = new();

   /// <summary>
   /// Gets all mapped properties for a type, with attribute-based overrides applied.
   /// </summary>
   /// <param name="type">The type to inspect.</param>
   /// <returns>Array of property metadata.</returns>
   public PropertyMetadata[] GetProperties(Type type)
   {
      if(type == null)
         throw new ArgumentNullException(nameof(type));

      // Try cache first
      if(_cache.TryGetValue(type, out var cached))
         return cached;

      // Discover and cache properties
      var properties = DiscoverProperties(type);
      _cache[type] = properties;
      return properties;
   }

   /// <summary>
   /// Discovers properties and applies attribute-based overrides.
   /// </summary>
   private PropertyMetadata[] DiscoverProperties(Type type)
   {
      var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
      var result = new List<PropertyMetadata>();

      // Get all properties
      foreach(var prop in type.GetProperties(bindingFlags))
      {
         if(!prop.CanRead)
            continue;

         result.Add(CreatePropertyMetadata(prop));
      }

      // Get all public fields (less common but supported)
      foreach(var field in type.GetFields(bindingFlags))
      {
         result.Add(CreatePropertyMetadata(field));
      }

      return [.. result];
   }

   /// <summary>
   /// Creates metadata for a property with attribute overrides.
   /// </summary>
   private PropertyMetadata CreatePropertyMetadata(MemberInfo member)
   {
      // Check for [AjisIgnore]
      var ignoreAttr = member.GetCustomAttribute<AjisIgnoreAttribute>();
      if(ignoreAttr != null)
         return new PropertyMetadata(member, "", isIgnored: true, isRequired: false, null, GetMemberType(member));

      // Check for [AjisPropertyName] override
      var propertyNameAttr = member.GetCustomAttribute<AjisPropertyNameAttribute>();
      var ajisKey = propertyNameAttr?.Name ?? _namingPolicy.ConvertName(member.Name);

      // Check for [AjisRequired]
      var requiredAttr = member.GetCustomAttribute<AjisRequiredAttribute>();
      bool isRequired = requiredAttr != null;

      // Check for [AjisNumberFormat]
      var numberFormatAttr = member.GetCustomAttribute<AjisNumberFormatAttribute>();
      AjisNumberStyle? numberStyle = numberFormatAttr?.Style;

      var propertyType = GetMemberType(member);

      return new PropertyMetadata(
          member,
          ajisKey,
          isIgnored: false,
          isRequired,
          numberStyle,
          propertyType);
   }

   /// <summary>
   /// Gets the type of a property or field.
   /// </summary>
   private static Type GetMemberType(MemberInfo member)
   {
      return member switch
      {
         PropertyInfo prop => prop.PropertyType,
         FieldInfo field => field.FieldType,
         _ => throw new ArgumentException($"Unsupported member type: {member.GetType().Name}")
      };
   }

   /// <summary>
   /// Gets a property value from an object instance.
   /// </summary>
   public object? GetValue(object obj, PropertyMetadata metadata)
   {
      if(obj == null)
         throw new ArgumentNullException(nameof(obj));

      return metadata.Member switch
      {
         PropertyInfo prop => prop.GetValue(obj),
         FieldInfo field => field.GetValue(obj),
         _ => throw new InvalidOperationException($"Unknown member type: {metadata.Member.GetType().Name}")
      };
   }

   /// <summary>
   /// Sets a property value on an object instance.
   /// </summary>
   public void SetValue(object obj, PropertyMetadata metadata, object? value)
   {
      if(obj == null)
         throw new ArgumentNullException(nameof(obj));

      try
      {
         switch(metadata.Member)
         {
            case PropertyInfo prop:
               if(prop.CanWrite)
                  prop.SetValue(obj, value);
               break;
            case FieldInfo field:
               field.SetValue(obj, value);
               break;
            default:
               throw new InvalidOperationException($"Unknown member type: {metadata.Member.GetType().Name}");
         }
      }
      catch(TargetInvocationException ex)
      {
         // Unwrap reflection exceptions
         throw new InvalidOperationException(
             $"Error setting property {metadata.Member.Name}: {ex.InnerException?.Message}",
             ex.InnerException);
      }
   }
}
