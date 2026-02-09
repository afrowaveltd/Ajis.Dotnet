#nullable enable

using System.Collections.Frozen;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// PHASE 7B: Global singleton PropertyMapper for shared type metadata cache.
/// Ensures all AjisConverter instances share same property discovery and caching.
/// Eliminates duplicate reflection work and Dictionary allocations.
/// </summary>
internal static class GlobalPropertyMapperFactory
{
    private static readonly Lazy<PropertyMapper> s_defaultMapper = new(
        () => new PropertyMapper(PascalCaseNamingPolicy.Instance),
        LazyThreadSafetyMode.PublicationOnly
    );

    private static readonly Lazy<PropertyMapper> s_camelCaseMapper = new(
        () => new PropertyMapper(CamelCaseNamingPolicy.Instance),
        LazyThreadSafetyMode.PublicationOnly
    );

    /// <summary>
    /// Gets the default (PascalCase) PropertyMapper singleton.
    /// Shared across all AjisConverter instances using default naming policy.
    /// </summary>
    public static PropertyMapper Default => s_defaultMapper.Value;

    /// <summary>
    /// Gets the CamelCase PropertyMapper singleton.
    /// Shared across all AjisConverter instances using camelCase naming policy.
    /// </summary>
    public static PropertyMapper CamelCase => s_camelCaseMapper.Value;

    /// <summary>
    /// Gets or creates a PropertyMapper for a custom naming policy.
    /// Note: Custom policies are not cached. Use Default or CamelCase for best performance.
    /// </summary>
    public static PropertyMapper GetOrCreate(INamingPolicy namingPolicy)
    {
        if (ReferenceEquals(namingPolicy, PascalCaseNamingPolicy.Instance))
            return Default;
        if (ReferenceEquals(namingPolicy, CamelCaseNamingPolicy.Instance))
            return CamelCase;

        // Custom policy - not cached (user's responsibility)
        return new PropertyMapper(namingPolicy);
    }
}
