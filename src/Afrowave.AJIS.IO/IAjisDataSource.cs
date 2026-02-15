#nullable enable

using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Non-generic interface for AjisDataSource to support saving all changes.
/// </summary>
public interface IAjisDataSource : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Saves all pending changes to the data source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads the entity state from the data source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a data source for AJIS/ATP files that supports CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type stored in the data source.</typeparam>
public interface IAjisDataSource<T> : IAjisDataSource, IAsyncDisposable, IDisposable where T : class
{
    /// <summary>
    /// Gets the file path of the data source.
    /// </summary>
    string FilePath { get; }

    /// <summary>
    /// Gets the format used by this data source (AJIS or ATP).
    /// </summary>
    AjisFormat Format { get; }

    /// <summary>
    /// Gets or sets the primary key property name for this entity type.
    /// </summary>
    string? KeyPropertyName { get; set; }

    /// <summary>
    /// Gets the entity configuration for this data source.
    /// </summary>
    AjisEntityConfiguration<T>? Configuration { get; }

    /// <summary>
    /// Adds a new entity to the data source.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the data source.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the data source.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities in the data source.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the data source.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities from the data source.
    /// </summary>
    /// <param name="entities">The entities to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match entities for removal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RemoveWhereAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an entity by its primary key value.
    /// </summary>
    /// <param name="keyValue">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The found entity or null if not found.</returns>
    Task<T?> FindByKeyAsync(object keyValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an entity by its primary key value synchronously.
    /// </summary>
    /// <param name="keyValue">The primary key value.</param>
    /// <returns>The found entity or null if not found.</returns>
    T? FindByKey(object keyValue);

    /// <summary>
    /// Gets the count of entities in the data source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the count of entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the count of matching entities.</returns>
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specified predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match entities.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing true if any entity matches, otherwise false.</returns>
    Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
