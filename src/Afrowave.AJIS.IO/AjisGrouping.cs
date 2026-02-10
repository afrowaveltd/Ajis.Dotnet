#nullable enable

using System.Linq.Expressions;

namespace Afrowave.AJIS.IO;

/// <summary>
/// GroupBy extension methods for AJIS queries.
/// Provides SQL-like grouping and aggregation capabilities.
/// </summary>
public static class AjisGrouping
{
    /// <summary>
    /// Groups elements by a key selector.
    /// </summary>
    public static IEnumerable<IGrouping<TKey, T>> GroupBy<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var compiled = keySelector.Compile();
        return source.ToList().GroupBy(compiled);
    }

    /// <summary>
    /// Groups elements and projects each group.
    /// </summary>
    public static IEnumerable<TResult> GroupBy<T, TKey, TResult>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        Func<TKey, IEnumerable<T>, TResult> resultSelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (resultSelector == null)
            throw new ArgumentNullException(nameof(resultSelector));

        var compiled = keySelector.Compile();
        var groups = source.ToList().GroupBy(compiled);

        foreach (var group in groups)
        {
            yield return resultSelector(group.Key, group);
        }
    }

    /// <summary>
    /// Groups elements and applies an element selector before grouping.
    /// </summary>
    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<T, TKey, TElement>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, TElement>> elementSelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (elementSelector == null)
            throw new ArgumentNullException(nameof(elementSelector));

        var compiledKey = keySelector.Compile();
        var compiledElement = elementSelector.Compile();

        return source.ToList().GroupBy(compiledKey, compiledElement);
    }
}

/// <summary>
/// Helper methods for grouped data aggregation - SQL-like GROUP BY with aggregates.
/// </summary>
public static class GroupedAggregations
{
    /// <summary>
    /// Groups by key and counts items in each group.
    /// Example: SELECT Category, COUNT(*) FROM Products GROUP BY Category
    /// </summary>
    public static IEnumerable<(TKey Key, int Count)> GroupByCount<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        return source.GroupBy(keySelector)
            .Select(g => (g.Key, g.Count()));
    }

    /// <summary>
    /// Groups by key and sums values in each group.
    /// Example: SELECT Category, SUM(Price) FROM Products GROUP BY Category
    /// </summary>
    public static IEnumerable<(TKey Key, TValue Sum)> GroupBySum<T, TKey, TValue>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        Func<T, TValue> valueSelector) 
        where T : notnull
        where TValue : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));

        var compiledKey = keySelector.Compile();

        return source.ToList()
            .GroupBy(compiledKey)
            .Select(g =>
            {
                dynamic sum = default(TValue);
                foreach (var item in g)
                {
                    sum += (dynamic)valueSelector(item);
                }
                return (g.Key, (TValue)sum);
            });
    }

    /// <summary>
    /// Groups by key and calculates average in each group.
    /// Example: SELECT Category, AVG(Price) FROM Products GROUP BY Category
    /// </summary>
    public static IEnumerable<(TKey Key, double Average)> GroupByAverage<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        Func<T, double> valueSelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));

        var compiledKey = keySelector.Compile();

        return source.ToList()
            .GroupBy(compiledKey)
            .Select(g => (g.Key, g.Average(valueSelector)));
    }

    /// <summary>
    /// Groups by key and finds min/max in each group.
    /// Example: SELECT Category, MIN(Price), MAX(Price) FROM Products GROUP BY Category
    /// </summary>
    public static IEnumerable<(TKey Key, TValue Min, TValue Max)> GroupByMinMax<T, TKey, TValue>(
        this IQueryable<T> source,
        Expression<Func<T, TKey>> keySelector,
        Func<T, TValue> valueSelector) 
        where T : notnull
        where TValue : IComparable<TValue>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        if (valueSelector == null)
            throw new ArgumentNullException(nameof(valueSelector));

        var compiledKey = keySelector.Compile();

        return source.ToList()
            .GroupBy(compiledKey)
            .Select(g => (
                g.Key,
                g.Min(valueSelector)!,
                g.Max(valueSelector)!
            ));
    }
}
