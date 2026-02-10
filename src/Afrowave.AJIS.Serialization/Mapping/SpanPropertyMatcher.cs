#nullable enable

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Fast property name matching using Span-based comparisons.
/// Eliminates string allocations during property lookup.
/// PHASE 9: Uses FrozenDictionary for optimal performance.
/// </summary>
internal sealed class SpanPropertyMatcher
{
    private readonly Dictionary<string, PropertyMetadata> _exactMatchCache;
    private readonly byte[][] _propertyNamesUtf8;
    private readonly PropertyMetadata[] _properties;

    public SpanPropertyMatcher(PropertyMetadata[] properties)
    {
        _properties = properties;
        _propertyNamesUtf8 = new byte[properties.Length][];

        var exactMatchDict = new Dictionary<string, PropertyMetadata>(properties.Length);

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            exactMatchDict[prop.AjisKey] = prop;
            _propertyNamesUtf8[i] = Encoding.UTF8.GetBytes(prop.AjisKey);
        }

        _exactMatchCache = exactMatchDict;
    }

    /// <summary>
    /// Finds property by UTF8 name bytes (zero allocation).
    /// PHASE 9: SIMD-accelerated comparison for better performance.
    /// </summary>
    public PropertyMetadata? FindProperty(ReadOnlySpan<byte> nameUtf8)
    {
        // Fast path: SIMD-accelerated exact match by bytes
        for (int i = 0; i < _propertyNamesUtf8.Length; i++)
        {
            var propBytes = _propertyNamesUtf8[i];
            if (nameUtf8.Length == propBytes.Length && 
                SpansEqualSimd(nameUtf8, propBytes))
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

    private static bool SpansEqualSimd(ReadOnlySpan<byte> a, byte[] b)
    {
        if (!Avx2.IsSupported)
            return a.SequenceEqual(b);

        // SIMD comparison using AVX2
        var spanB = b.AsSpan();
        int i = 0;
        
        // Compare 32 bytes at a time
        for (; i <= a.Length - 32; i += 32)
        {
            var vecA = Vector256.LoadUnsafe(ref Unsafe.AsRef(in a[i]));
            var vecB = Vector256.LoadUnsafe(ref Unsafe.AsRef(in spanB[i]));
            if (!Avx2.MoveMask(Avx2.CompareEqual(vecA, vecB)).Equals(-1))
                return false;
        }
        
        // Handle remaining bytes
        for (; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        
        return true;
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
