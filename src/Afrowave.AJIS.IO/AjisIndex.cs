#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Collections.Concurrent;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Index for fast lookups in AJIS/ATP data sources.
/// Implements a hash-based index for O(1) lookups by specified property.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class AjisIndex<T> : IDisposable where T : class
{
    private readonly string _filePath;
    private readonly string _indexPropertyName;
    private readonly AjisConverter<List<T>> _converter;
    private readonly ConcurrentDictionary<object, T?> _index = new();
    private readonly object _loadLock = new();
    private bool _isBuilt;
    #pragma warning disable CS0414 // Field is assigned but its value is never used
    private bool _isDisposed;
    #pragma warning restore CS0414
    #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private List<T>? _allItems;
    #pragma warning restore CS0649

    /// <summary>
    /// Gets the file path of the indexed data source.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Gets the property name being indexed.
    /// </summary>
    public string IndexPropertyName => _indexPropertyName;

    /// <summary>
    /// Gets the number of items in the index.
    /// </summary>
    public int Count => _index.Count;

    /// <summary>
    /// Initializes a new instance of <see cref="AjisIndex{T}"/>.
    /// </summary>
    /// <param name="filePath">Path to the AJIS/ATP file.</param>
    /// <param name="indexPropertyName">Name of the property to index.</param>
    public AjisIndex(string filePath, string indexPropertyName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (string.IsNullOrWhiteSpace(indexPropertyName))
            throw new ArgumentNullException(nameof(indexPropertyName));

        _filePath = filePath;
        _indexPropertyName = indexPropertyName;
        _converter = new AjisConverter<List<T>>();
    }

    /// <summary>
    /// Builds the index by scanning the file.
    /// This should be called before using the index for optimal performance.
    /// </summary>
    public async Task BuildAsync()
    {
        if (_isBuilt)
            return;

        await Task.Run(() =>
        {
            lock (_loadLock)
            {
                if (_isBuilt)
                    return;

                if (!File.Exists(_filePath))
                {
                    _isBuilt = true;
                    return;
                }

                var json = File.ReadAllText(_filePath);
                var items = _converter.Deserialize(json) ?? new List<T>();
                
                // Build index from items
                foreach (var item in items)
                {
                    if (item == null)
                        continue;

                    var value = GetIndexedValue(item);
                    if (value != null)
                    {
                        // Note: If multiple items have the same index value, only the last one is kept
                        // For multi-value indexes, use FindAll method which reads all items
                        _index[value] = item;
                    }
                }

                _isBuilt = true;
            }
        });
    }

    /// <summary>
    /// Builds the index synchronously.
    /// </summary>
    public void Build()
    {
        BuildAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Finds an entity by the indexed property value.
    /// </summary>
    /// <param name="value">The indexed property value.</param>
    /// <returns>The found entity, or null if not found.</returns>
    public async Task<T?> FindAsync(object value)
    {
        await EnsureBuiltAsync();

        if (_index.TryGetValue(value, out var entity))
        {
            return entity;
        }

        return null;
    }

    /// <summary>
    /// Finds an entity by the indexed property value (synchronous).
    /// </summary>
    /// <param name="value">The indexed property value.</param>
    /// <returns>The found entity, or null if not found.</returns>
    public T? Find(object value)
    {
        return FindAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Finds multiple entities that match the indexed value.
    /// Useful for non-unique indexes.
    /// </summary>
    /// <param name="value">The indexed property value.</param>
    /// <returns>Collection of matching entities.</returns>
    public async Task<IEnumerable<T>> FindAllAsync(object value)
    {
        await EnsureBuiltAsync();

        // Load all items for scan ( Avoids deadlock since we're not holding _loadLock during I/O)
        var allItems = new List<T>();
        
        if (File.Exists(_filePath))
        {
            var json = await File.ReadAllTextAsync(_filePath);
            allItems = _converter.Deserialize(json) ?? new List<T>();
        }

        // Filter items matching the value
        var results = new List<T>();
        foreach (var item in allItems)
        {
            if (item != null && GetIndexedValue(item)?.Equals(value) == true)
            {
                results.Add(item);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets all indexed values (unique).
    /// </summary>
    /// <returns>Collection of all indexed values.</returns>
    public async Task<IEnumerable<object>> GetValuesAsync()
    {
        await EnsureBuiltAsync();
        return _index.Keys;
    }

    /// <summary>
    /// Gets all indexed values (unique, synchronous).
    /// </summary>
    /// <returns>Collection of all indexed values.</returns>
    public IEnumerable<object> GetValues()
    {
        return GetValuesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if an entity with the given indexed value exists.
    /// </summary>
    /// <param name="value">The indexed property value.</param>
    /// <returns>True if exists, otherwise false.</returns>
    public async Task<bool> ContainsAsync(object value)
    {
        await EnsureBuiltAsync();
        return _index.ContainsKey(value);
    }

    /// <summary>
    /// Checks if an entity with the given indexed value exists (synchronous).
    /// </summary>
    /// <param name="value">The indexed property value.</param>
    /// <returns>True if exists, otherwise false.</returns>
    public bool Contains(object value)
    {
        return ContainsAsync(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reloads the index from the file.
    /// </summary>
    public async Task ReloadAsync()
    {
        _index.Clear();
        _isBuilt = false;
        await BuildAsync();
    }

    /// <summary>
    /// Reloads the index from the file (synchronous).
    /// </summary>
    public void Reload()
    {
        ReloadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the indexed property value from an entity.
    /// </summary>
    private object? GetIndexedValue(T entity)
    {
        if (entity == null)
            return null;

        var property = typeof(T).GetProperty(_indexPropertyName);
        return property?.GetValue(entity);
    }

    /// <summary>
    /// Ensures the index is built.
    /// </summary>
    private async Task EnsureBuiltAsync()
    {
        if (_isBuilt)
            return;

        await BuildAsync();
    }

    #region IDisposable Implementation

    public void Dispose()
    {
        _isDisposed = true;
        _index.Clear();
        _allItems?.Clear();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    #endregion
}

/// <summary>
/// Static extension methods for working with AJIS indexes.
/// </summary>
public static class AjisIndexExtensions
{
    /// <summary>
    /// Creates an index for fast lookups on a specified property.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="filePath">Path to the AJIS/ATP file.</param>
    /// <param name="indexPropertyName">Name of the property to index.</param>
    /// <returns>A new <see cref="AjisIndex{T}"/> instance.</returns>
    public static AjisIndex<T> CreateIndex<T>(this string filePath, string indexPropertyName) where T : class
    {
        return new AjisIndex<T>(filePath, indexPropertyName);
    }
}
