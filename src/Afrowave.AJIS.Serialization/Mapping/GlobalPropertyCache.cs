#nullable enable

using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// PHASE 8C: Global static cache for property metadata.
/// Eliminates per-instance cache duplication and ensures all deserializers share same metadata.
/// Inspired by System.Text.Json global type cache strategy.
/// </summary>
internal static class GlobalPropertyCache
{
    // Concurrent collections for thread-safe global caching
    private static readonly ConcurrentDictionary<Type, PropertyMetadata[]> s_properties = new();
    private static readonly ConcurrentDictionary<Type, FrozenDictionary<string, PropertyMetadata>> s_exactLookup = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyMetadata>> s_caseInsensitiveLookup = new();

    /// <summary>
    /// Gets property metadata array for a type (cached globally).
    /// </summary>
    public static PropertyMetadata[] GetProperties(Type type, PropertyMapper mapper)
    {
        return s_properties.GetOrAdd(type, t => mapper.GetProperties(t).ToArray());
    }

    /// <summary>
    /// Gets exact-match (case-sensitive) property lookup for a type.
    /// Uses FrozenDictionary (.NET 10) for optimal read performance.
    /// </summary>
    public static FrozenDictionary<string, PropertyMetadata> GetExactLookup(Type type, PropertyMapper mapper)
    {
        return s_exactLookup.GetOrAdd(type, t =>
        {
            var props = GetProperties(t, mapper);
            return props.ToFrozenDictionary(p => p.AjisKey, StringComparer.Ordinal);
        });
    }

    /// <summary>
    /// Gets case-insensitive property lookup for a type (fallback).
    /// </summary>
    public static Dictionary<string, PropertyMetadata> GetCaseInsensitiveLookup(Type type, PropertyMapper mapper)
    {
        return s_caseInsensitiveLookup.GetOrAdd(type, t =>
        {
            var props = GetProperties(t, mapper);
            return props.ToDictionary(p => p.AjisKey, StringComparer.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Clears all cached metadata (for testing purposes).
    /// </summary>
    public static void Clear()
    {
        s_properties.Clear();
        s_exactLookup.Clear();
        s_caseInsensitiveLookup.Clear();
    }
}
