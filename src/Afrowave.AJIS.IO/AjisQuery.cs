#nullable enable

using System.Collections;
using System.Linq.Expressions;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Linq provider for querying AJIS files.
/// Usage: var users = from u in AjisQuery&lt;User&gt;("users.json") where u.Age > 18 select u;
/// </summary>
public static class AjisQuery
{
    /// <summary>
    /// Creates a queryable collection from an AJIS file.
    /// </summary>
    public static IQueryable<T> FromFile<T>(string filePath) where T : notnull
    {
        return new AjisQueryable<T>(filePath);
    }

    /// <summary>
    /// Creates a queryable collection with indexing for better performance.
    /// </summary>
    public static IQueryable<T> FromFile<T>(string filePath, string indexProperty) where T : notnull
    {
        return new AjisQueryable<T>(filePath, indexProperty);
    }
}

/// <summary>
/// IQueryable implementation for AJIS files.
/// </summary>
internal class AjisQueryable<T> : IQueryable<T> where T : notnull
{
    private readonly string _filePath;
    private readonly string? _indexProperty;

    public AjisQueryable(string filePath, string? indexProperty = null, Expression? expression = null)
    {
        _filePath = filePath;
        _indexProperty = indexProperty;
        Provider = new AjisQueryProvider<T>(filePath, indexProperty);
        Expression = expression ?? Expression.Constant(this);
    }

    public Type ElementType => typeof(T);
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Query provider for AJIS files.
/// </summary>
internal class AjisQueryProvider<T> : IQueryProvider where T : notnull
{
    private readonly string _filePath;
    private readonly string? _indexProperty;

    public AjisQueryProvider(string filePath, string? indexProperty)
    {
        _filePath = filePath;
        _indexProperty = indexProperty;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new AjisQueryable<T>(_filePath, _indexProperty, expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return (IQueryable<TElement>)CreateQuery(expression);
    }

    public object Execute(Expression expression)
    {
        return Execute<IEnumerable<T>>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        // Use enhanced visitor with full LINQ support
        var visitor = new EnhancedAjisQueryVisitor<T>();
        visitor.Visit(expression);

        // Execute complete query pipeline
        var result = visitor.ExecuteQuery<T>(_filePath);

        // Handle different result types
        if (typeof(TResult) == typeof(T))
        {
            return (TResult)(object)(result.FirstOrDefault() ?? throw new InvalidOperationException("No matching element found"));
        }

        if (typeof(TResult).IsAssignableFrom(typeof(IEnumerable<T>)))
        {
            return (TResult)(object)result;
        }

        if (typeof(TResult).IsGenericType)
        {
            var genericType = typeof(TResult).GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>) || genericType == typeof(List<>))
            {
                return visitor.ExecuteQuery<TResult>(_filePath).First();
            }
        }
        
        return (TResult)(object)result;
    }
}

/// <summary>
/// Expression visitor to parse Linq queries.
/// </summary>
internal class AjisQueryVisitor<T> : ExpressionVisitor where T : notnull
{
    public Func<T, bool>? WherePredicate { get; private set; }
    public object? KeyLookup { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if(node.Method.Name == "Where" && node.Arguments.Count == 2)
        {
            // Parse Where clause
            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            WherePredicate = (Func<T, bool>)lambda.Compile();
        }
        else if(node.Method.Name == "FirstOrDefault" && node.Arguments.Count == 2)
        {
            // Parse FirstOrDefault with predicate
            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            var predicate = (Func<T, bool>)lambda.Compile();

            // Try to extract key lookup from equality comparison
            KeyLookup = ExtractKeyFromPredicate(predicate);
        }

        return base.VisitMethodCall(node);
    }

    private object? ExtractKeyFromPredicate(Func<T, bool> predicate)
    {
        // Simple implementation - try to detect x => x.Id == value pattern
        // TODO: More sophisticated predicate analysis
        return null;
    }
}