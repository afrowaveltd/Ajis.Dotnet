#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Collections.Concurrent;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Index for fast lookup of objects in AJIS files.
/// </summary>
public sealed class AjisFileIndex<T> : IDisposable where T : notnull
{
    private readonly string _filePath;
    private readonly string _keyProperty;
    private readonly ConcurrentDictionary<object, long> _index = new();
    private readonly AjisConverter<T> _converter;
    private bool _isBuilt;

    public AjisFileIndex(string filePath, string keyProperty)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _keyProperty = keyProperty ?? throw new ArgumentNullException(nameof(keyProperty));
        _converter = new AjisConverter<T>();
    }

    /// <summary>
    /// Builds the index by scanning the file.
    /// </summary>
    public void Build()
    {
        if(_isBuilt) return;

        // Simplified implementation - just enumerate all objects and index them
        var objects = AjisFile.Enumerate<T>(_filePath).ToList();
        
        for(int i = 0; i < objects.Count; i++)
        {
            var obj = objects[i];
            if(obj != null)
            {
                var keyValue = GetKeyValue(obj);
                if(keyValue != null)
                {
                    // Use index as position (simplified)
                    _index[keyValue] = i;
                }
            }
        }

        _isBuilt = true;
    }

    /// <summary>
    /// Finds an object by key value.
    /// </summary>
    public T? FindByKey(object keyValue)
    {
        if(!_isBuilt) Build();

        if(_index.TryGetValue(keyValue, out long position))
        {
            return ReadObjectAtPosition(position);
        }

        return default;
    }

    /// <summary>
    /// Gets all indexed keys.
    /// </summary>
    public IEnumerable<object> GetKeys()
    {
        if(!_isBuilt) Build();
        return _index.Keys;
    }

    /// <summary>
    /// Checks if key exists in index.
    /// </summary>
    public bool ContainsKey(object keyValue)
    {
        if(!_isBuilt) Build();
        return _index.ContainsKey(keyValue);
    }

    private T? ParseObjectAtPosition(System.Text.Json.Utf8JsonReader reader)
    {
        // This is a simplified implementation
        // In practice, you'd need to properly parse the JSON object
        // For now, return null to avoid compilation errors
        return default;
    }

    private T? ReadObjectAtPosition(long position)
    {
        // Simplified implementation - re-enumerate and find by index
        var objects = AjisFile.Enumerate<T>(_filePath).ToList();
        if(position >= 0 && position < objects.Count)
        {
            return objects[(int)position];
        }
        return default;
    }

    private object? GetKeyValue(T obj)
    {
        var property = typeof(T).GetProperty(_keyProperty);
        return property?.GetValue(obj);
    }

    public void Dispose()
    {
        _index.Clear();
    }
}