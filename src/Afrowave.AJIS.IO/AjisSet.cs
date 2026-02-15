#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Collections;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Represents a set of entities in an AJIS/ATP data source.
/// Similar to DbSet&lt;T&gt; in Entity Framework Core.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class AjisSet<T> : IAjisDataSource<T> where T : class
{
    private readonly IAjisDataSource<T> _dataSource;
    #pragma warning disable CS0414 // Field is assigned but its value is never used
    private bool _isDisposed;
    #pragma warning restore CS0414

    /// <summary>
    /// Gets the file path of the data source.
    /// </summary>
    public string FilePath => _dataSource.FilePath;

    /// <summary>
    /// Gets the format used by this data source (AJIS or ATP).
    /// </summary>
    public AjisFormat Format => _dataSource.Format;

    /// <summary>
    /// Gets or sets the primary key property name for this entity type.
    /// </summary>
    public string? KeyPropertyName
    {
        get => _dataSource.KeyPropertyName;
        set => _dataSource.KeyPropertyName = value;
    }

    /// <summary>
    /// Gets the entity configuration for this data source.
    /// </summary>
    public AjisEntityConfiguration<T>? Configuration => _dataSource.Configuration;

    /// <summary>
    /// Initializes a new instance of <see cref="AjisSet{T}"/>.
    /// </summary>
    /// <param name="dataSource">The underlying data source.</param>
    public AjisSet(IAjisDataSource<T> dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    #region IAjisDataSource<T> Implementation

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dataSource.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dataSource.AddRangeAsync(entities, cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dataSource.UpdateAsync(entity, cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dataSource.UpdateRangeAsync(entities, cancellationToken);
    }

    public async Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dataSource.RemoveAsync(entity, cancellationToken);
    }

    public async Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dataSource.RemoveRangeAsync(entities, cancellationToken);
    }

    public async Task RemoveWhereAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        await _dataSource.RemoveWhereAsync(predicate, cancellationToken);
    }

    public async Task<T?> FindByKeyAsync(object keyValue, CancellationToken cancellationToken = default)
    {
        return await _dataSource.FindByKeyAsync(keyValue, cancellationToken);
    }

    public T? FindByKey(object keyValue)
    {
        return _dataSource.FindByKey(keyValue);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.CountAsync(cancellationToken);
    }

    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dataSource.CountAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dataSource.AnyAsync(predicate, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dataSource.SaveChangesAsync(cancellationToken);
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _dataSource.ReloadAsync(cancellationToken);
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _isDisposed = true;
        _dataSource.Dispose();
        GC.SuppressFinalize(this);
    }

    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        Dispose();
        return System.Threading.Tasks.ValueTask.CompletedTask;
    }

    #endregion
}
