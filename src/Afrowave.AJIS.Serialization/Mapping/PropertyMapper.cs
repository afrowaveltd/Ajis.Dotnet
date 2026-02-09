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
internal sealed class PropertyMetadata
{
    /// <summary>
    /// Gets the .NET property or field information.
    /// </summary>
    public MemberInfo Member { get; }

    /// <summary>
    /// Gets the AJIS key name (after applying naming policy or [AjisPropertyName] override).
    /// </summary>
    public string AjisKey { get; }

    /// <summary>
    /// Gets whether this property should be ignored during mapping.
    /// </summary>
    public bool IsIgnored { get; }

    /// <summary>
    /// Gets whether this property is required (must not be null).
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the custom number format, if any.
    /// </summary>
    public AjisNumberStyle? NumberStyle { get; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// Initializes metadata for a property member.
    /// </summary>
    public PropertyMetadata(
        MemberInfo member,
        string ajisKey,
        bool isIgnored,
        bool isRequired,
        AjisNumberStyle? numberStyle,
        Type propertyType)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
        AjisKey = ajisKey ?? throw new ArgumentNullException(nameof(ajisKey));
        IsIgnored = isIgnored;
        IsRequired = isRequired;
        NumberStyle = numberStyle;
        PropertyType = propertyType ?? throw new ArgumentNullException(nameof(propertyType));
    }
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
internal sealed class PropertyMapper
{
    private readonly INamingPolicy _namingPolicy;
    private readonly ConcurrentDictionary<Type, PropertyMetadata[]> _cache = new();

    /// <summary>
    /// Initializes a new PropertyMapper with a naming policy.
    /// </summary>
    /// <param name="namingPolicy">The naming policy for property name conversion.</param>
    public PropertyMapper(INamingPolicy namingPolicy)
    {
        _namingPolicy = namingPolicy ?? throw new ArgumentNullException(nameof(namingPolicy));
    }

    /// <summary>
    /// Gets all mapped properties for a type, with attribute-based overrides applied.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>Array of property metadata.</returns>
    public PropertyMetadata[] GetProperties(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Try cache first
        if (_cache.TryGetValue(type, out var cached))
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
        foreach (var prop in type.GetProperties(bindingFlags))
        {
            if (!prop.CanRead)
                continue;

            result.Add(CreatePropertyMetadata(prop));
        }

        // Get all public fields (less common but supported)
        foreach (var field in type.GetFields(bindingFlags))
        {
            result.Add(CreatePropertyMetadata(field));
        }

        return result.ToArray();
    }

    /// <summary>
    /// Creates metadata for a property with attribute overrides.
    /// </summary>
    private PropertyMetadata CreatePropertyMetadata(MemberInfo member)
    {
        // Check for [AjisIgnore]
        var ignoreAttr = member.GetCustomAttribute<AjisIgnoreAttribute>();
        if (ignoreAttr != null)
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
        if (obj == null)
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
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        try
        {
            switch (metadata.Member)
            {
                case PropertyInfo prop:
                    if (prop.CanWrite)
                        prop.SetValue(obj, value);
                    break;
                case FieldInfo field:
                    field.SetValue(obj, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown member type: {metadata.Member.GetType().Name}");
            }
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap reflection exceptions
            throw new InvalidOperationException(
                $"Error setting property {metadata.Member.Name}: {ex.InnerException?.Message}",
                ex.InnerException);
        }
    }
}
