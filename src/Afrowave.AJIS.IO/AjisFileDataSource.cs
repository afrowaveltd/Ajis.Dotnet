#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Collections;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Data source implementation for pure text-based AJIS files.
/// </summary>
/// <remarks>
/// This implementation is optimized for text-only data without binary attachments.
/// For files with binary attachments, use <see cref="AtpFileDataSource{T}"/> instead.
/// </remarks>
/// <typeparam name="T">The entity type stored in the data source.</typeparam>
public class AjisFileDataSource<T> : IAjisDataSource<T> where T : class
{
    private readonly string _filePath;
    private bool _isDisposed;
    private List<T>? _cachedData;
    private readonly AjisConverter<List<T>> _converter;
    private readonly object _loadLock = new();

    /// <summary>
    /// Gets the file path of the data source.
    /// </summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Gets the format used by this data source (AJIS).
    /// </summary>
    public AjisFormat Format => AjisFormat.Ajis;

    /// <summary>
    /// Gets or sets the primary key property name for this entity type.
    /// </summary>
    public string? KeyPropertyName { get; set; }

    /// <summary>
    /// Gets the entity configuration for this data source.
    /// </summary>
    public AjisEntityConfiguration<T>? Configuration { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AjisFileDataSource{T}"/>.
    /// </summary>
    /// <param name="filePath">Path to the AJIS file.</param>
    /// <param name="configuration">Optional entity configuration.</param>
    public AjisFileDataSource(string filePath, AjisEntityConfiguration<T>? configuration = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        _filePath = filePath;
        Configuration = configuration;
        _converter = new AjisConverter<List<T>>();
    }

    /// <summary>
    /// Loads data from the file if not already loaded.
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (_cachedData != null)
            return;

        await Task.Run(() =>
        {
            lock (_loadLock)
            {
                if (_cachedData != null)
                    return;

                if (!File.Exists(_filePath))
                {
                    _cachedData = new List<T>();
                    return;
                }

                var json = File.ReadAllText(_filePath);
                _cachedData = _converter.Deserialize(json) ?? new List<T>();
            }
        });
    }

    /// <summary>
    /// Saves the current state to the file with file locking.
    /// </summary>
    private async Task SaveAsync()
    {
        await Task.Run(() =>
        {
            lock (_loadLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(AjisFileDataSource<T>));

                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
                var json = _converter.Serialize(_cachedData);
                writer.Write(json);
            }
        });
    }

    /// <summary>
    /// Gets the primary key value from an entity.
    /// </summary>
    private object? GetKeyValue(T entity)
    {
        if (entity == null)
            return null;

        var keyPropName = KeyPropertyName ?? DetectKeyName();
        if (string.IsNullOrEmpty(keyPropName))
            return null;

        var property = typeof(T).GetProperty(keyPropName);
        return property?.GetValue(entity);
    }

    /// <summary>
    /// Automatically detects the primary key property name.
    /// </summary>
    private string? DetectKeyName()
    {
        var properties = typeof(T).GetProperties();
        
        // Check for [AjisKey] attribute first
        foreach (var prop in properties)
        {
            if (Attribute.IsDefined(prop, typeof(AjisKeyAttribute)))
                return prop.Name;
        }

        // Check for "Id" or "{ClassName}Id" pattern
        var className = typeof(T).Name;
        foreach (var prop in properties)
        {
            if (prop.Name == "Id" || prop.Name == $"{className}Id")
                return prop.Name;
        }

        return null;
    }

    /// <summary>
    /// Finds an entity by its primary key value.
    /// </summary>
    private T? FindByKeyInternal(object keyValue)
    {
        if (_cachedData == null)
            return default;

        var keyPropName = KeyPropertyName ?? DetectKeyName();
        if (string.IsNullOrEmpty(keyPropName))
            return default;

        return _cachedData.FirstOrDefault(e => GetKeyValue(e)?.Equals(keyValue) == true);
    }

    #region IAjisDataSource<T> Implementation

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        _cachedData!.Add(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        _cachedData!.AddRange(entities);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();

        var keyValue = GetKeyValue(entity);
        if (keyValue == null)
            throw new InvalidOperationException("Entity has no valid key value");

        var index = _cachedData!.FindIndex(e => GetKeyValue(e)?.Equals(keyValue) == true);
        if (index >= 0)
        {
            _cachedData[index] = entity;
        }
        else
        {
            throw new KeyNotFoundException($"Entity with key {keyValue} not found");
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();

        foreach (var entity in entities)
        {
            await UpdateAsync(entity, cancellationToken);
        }
    }

    public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        _cachedData!.Remove(entity);
    }

    public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        _cachedData!.RemoveAll(e => entities.Contains(e));
    }

    public async Task RemoveWhereAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        var compiled = predicate.Compile();
        _cachedData!.RemoveAll(t => compiled(t));
    }

    public async Task<T?> FindByKeyAsync(object keyValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        return FindByKeyInternal(keyValue);
    }

    public T? FindByKey(object keyValue)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        return FindByKeyInternal(keyValue);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        return _cachedData!.Count;
    }

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        var compiled = predicate.Compile();
        return _cachedData!.Count(t => compiled(t));
    }

    public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureLoadedAsync();
        var compiled = predicate.Compile();
        return _cachedData!.Any(t => compiled(t));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SaveAsync();
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cachedData = null;
        await EnsureLoadedAsync();
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        Dispose();
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

    #endregion
}
