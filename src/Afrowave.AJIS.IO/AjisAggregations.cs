#nullable enable

using System.Linq.Expressions;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Aggregation extension methods for AJIS queries.
/// Provides Count, Any, All, Sum, Average, Min, Max - just like LINQ to SQL!
/// </summary>
public static class AjisAggregations
{
    // ===== COUNT OPERATIONS =====

    /// <summary>
    /// Returns the number of elements in the sequence.
    /// </summary>
    public static int Count<T>(this IQueryable<T> source) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // If it's an AjisQueryable, execute efficiently
        if (source is AjisQueryable<T> ajisQuery)
        {
            return ExecuteCount(ajisQuery);
        }

        // Fallback to standard LINQ
        return Queryable.Count(source);
    }

    /// <summary>
    /// Returns the number of elements that satisfy the condition.
    /// </summary>
    public static int Count<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return source.Where(predicate).Count();
    }

    /// <summary>
    /// Returns the number of elements as long.
    /// </summary>
    public static long LongCount<T>(this IQueryable<T> source) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return source.Count();
    }

    // ===== ANY/ALL OPERATIONS =====

    /// <summary>
    /// Determines whether the sequence contains any elements.
    /// </summary>
    public static bool Any<T>(this IQueryable<T> source) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Efficient: just check if there's at least one element
        if (source is AjisQueryable<T> ajisQuery)
        {
            return ExecuteAny(ajisQuery);
        }

        return Queryable.Any(source);
    }

    /// <summary>
    /// Determines whether any element satisfies the condition.
    /// </summary>
    public static bool Any<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return source.Where(predicate).Any();
    }

    /// <summary>
    /// Determines whether all elements satisfy the condition.
    /// </summary>
    public static bool All<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // Efficient: return false on first non-match
        if (source is AjisQueryable<T> ajisQuery)
        {
            return ExecuteAll(ajisQuery, predicate);
        }

        return Queryable.All(source, predicate);
    }

    // ===== NUMERIC AGGREGATIONS =====

    /// <summary>
    /// Computes the sum of a sequence of values.
    /// </summary>
    public static TResult Sum<T, TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> selector) 
        where T : notnull
        where TResult : struct
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var compiled = selector.Compile();
        dynamic sum = default(TResult);

        foreach (var item in source)
        {
            sum += (dynamic)compiled(item);
        }

        return sum;
    }

    /// <summary>
    /// Computes the average of a sequence of values.
    /// </summary>
    public static double Average<T>(this IQueryable<T> source, Expression<Func<T, double>> selector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var compiled = selector.Compile();
        double sum = 0;
        int count = 0;

        foreach (var item in source)
        {
            sum += compiled(item);
            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        return sum / count;
    }

    /// <summary>
    /// Computes the average of a sequence of int values.
    /// </summary>
    public static double Average<T>(this IQueryable<T> source, Expression<Func<T, int>> selector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var compiled = selector.Compile();
        long sum = 0;
        int count = 0;

        foreach (var item in source)
        {
            sum += compiled(item);
            count++;
        }

        if (count == 0)
            throw new InvalidOperationException("Sequence contains no elements");

        return (double)sum / count;
    }

    /// <summary>
    /// Returns the minimum value in a sequence.
    /// </summary>
    public static TResult Min<T, TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> selector) 
        where T : notnull
        where TResult : IComparable<TResult>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var compiled = selector.Compile();
        TResult? min = default;
        bool hasValue = false;

        foreach (var item in source)
        {
            var value = compiled(item);
            if (!hasValue || (value != null && value.CompareTo(min!) < 0))
            {
                min = value;
                hasValue = true;
            }
        }

        if (!hasValue)
            throw new InvalidOperationException("Sequence contains no elements");

        return min!;
    }

    /// <summary>
    /// Returns the maximum value in a sequence.
    /// </summary>
    public static TResult Max<T, TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> selector) 
        where T : notnull
        where TResult : IComparable<TResult>
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var compiled = selector.Compile();
        TResult? max = default;
        bool hasValue = false;

        foreach (var item in source)
        {
            var value = compiled(item);
            if (!hasValue || (value != null && value.CompareTo(max!) > 0))
            {
                max = value;
                hasValue = true;
            }
        }

        if (!hasValue)
            throw new InvalidOperationException("Sequence contains no elements");

        return max!;
    }

    // ===== DISTINCT OPERATION =====

    /// <summary>
    /// Returns distinct elements from a sequence.
    /// </summary>
    public static IQueryable<T> Distinct<T>(this IQueryable<T> source) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return Queryable.Distinct(source);
    }

    /// <summary>
    /// Returns distinct elements based on a key selector.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector) where T : notnull
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var compiled = keySelector.Compile();
        var seen = new HashSet<TKey>();

        foreach (var item in source)
        {
            var key = compiled(item);
            if (key != null && seen.Add(key))
            {
                yield return item;
            }
        }
    }

    // ===== HELPER METHODS =====

    private static int ExecuteCount<T>(AjisQueryable<T> ajisQuery) where T : notnull
    {
        // Get the file path from the queryable
        var filePath = GetFilePath(ajisQuery);
        
        // Use EnhancedVisitor to apply filters, then count
        var visitor = new EnhancedAjisQueryVisitor<T>();
        visitor.Visit(ajisQuery.Expression);

        var result = visitor.ExecuteQuery<T>(filePath);
        return result.Count();
    }

    private static bool ExecuteAny<T>(AjisQueryable<T> ajisQuery) where T : notnull
    {
        var filePath = GetFilePath(ajisQuery);
        
        var visitor = new EnhancedAjisQueryVisitor<T>();
        visitor.Visit(ajisQuery.Expression);

        var result = visitor.ExecuteQuery<T>(filePath);
        return result.Any();
    }

    private static bool ExecuteAll<T>(AjisQueryable<T> ajisQuery, Expression<Func<T, bool>> predicate) where T : notnull
    {
        var filePath = GetFilePath(ajisQuery);
        var compiled = predicate.Compile();

        // Enumerate and check all elements
        foreach (var item in AjisFile.Enumerate<T>(filePath))
        {
            if (!compiled(item))
                return false;
        }

        return true;
    }

    private static string GetFilePath<T>(AjisQueryable<T> ajisQuery) where T : notnull
    {
        // Use reflection to get the private _filePath field
        var field = typeof(AjisQueryable<T>).GetField("_filePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field == null)
            throw new InvalidOperationException("Cannot access file path from AjisQueryable");

        return (string)(field.GetValue(ajisQuery) ?? throw new InvalidOperationException("File path is null"));
    }
}
