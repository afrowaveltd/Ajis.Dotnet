#nullable enable

namespace Afrowave.AJIS.IO;

/// <summary>
/// Extension methods for AjisSet with indexing support.
/// </summary>
public static class AjisSetIndexExtensions
{
    /// <summary>
    /// Creates an index for fast lookups on a specified property.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="set">The AjisSet to create index for.</param>
    /// <param name="indexPropertyName">Name of the property to index.</param>
    /// <returns>A new <see cref="AjisIndex{T}"/> instance.</returns>
    public static AjisIndex<T> CreateIndex<T>(this AjisSet<T> set, string indexPropertyName) where T : class
    {
        return new AjisIndex<T>(set.FilePath, indexPropertyName);
    }

    /// <summary>
    /// Creates an index for fast lookups on a specified property.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="dataSource">The data source to create index for.</param>
    /// <param name="indexPropertyName">Name of the property to index.</param>
    /// <returns>A new <see cref="AjisIndex{T}"/> instance.</returns>
    public static AjisIndex<T> CreateIndex<T>(this IAjisDataSource<T> dataSource, string indexPropertyName) where T : class
    {
        return new AjisIndex<T>(dataSource.FilePath, indexPropertyName);
    }
}
