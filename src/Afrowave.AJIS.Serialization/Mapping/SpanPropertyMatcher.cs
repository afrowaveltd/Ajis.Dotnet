#nullable enable

using System.Text;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Fast property name matching using Span-based comparisons.
/// Eliminates string allocations during property lookup.
/// </summary>
internal sealed class SpanPropertyMatcher
{
    private readonly Dictionary<string, PropertyMetadata> _exactMatchCache = new();
    private readonly byte[][] _propertyNamesUtf8;
    private readonly PropertyMetadata[] _properties;

    public SpanPropertyMatcher(PropertyMetadata[] properties)
    {
        _properties = properties;
        _propertyNamesUtf8 = new byte[properties.Length][];

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            _exactMatchCache[prop.AjisKey] = prop;
            _propertyNamesUtf8[i] = Encoding.UTF8.GetBytes(prop.AjisKey);
        }
    }

    /// <summary>
    /// Finds property by UTF8 name bytes (zero allocation).
    /// </summary>
    public PropertyMetadata? FindProperty(ReadOnlySpan<byte> nameUtf8)
    {
        // Fast path: exact match by bytes
        for (int i = 0; i < _propertyNamesUtf8.Length; i++)
        {
            if (nameUtf8.SequenceEqual(_propertyNamesUtf8[i]))
                return _properties[i];
        }

        // Fallback: case-insensitive (requires allocation)
        var nameStr = Encoding.UTF8.GetString(nameUtf8);
        foreach (var prop in _properties)
        {
            if (prop.AjisKey.Equals(nameStr, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        return null;
    }

    /// <summary>
    /// Finds property by string name (cached).
    /// </summary>
    public PropertyMetadata? FindProperty(string name)
    {
        if (_exactMatchCache.TryGetValue(name, out var prop))
            return prop;

        // Case-insensitive fallback
        foreach (var p in _properties)
        {
            if (p.AjisKey.Equals(name, StringComparison.OrdinalIgnoreCase))
                return p;
        }

        return null;
    }
}
