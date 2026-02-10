#nullable enable

using System.Collections;
using System.Linq.Expressions;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Enhanced Linq provider for querying AJIS files with full LINQ support.
/// Supports: Where, OrderBy, ThenBy, Skip, Take, Select - just like EF Core!
/// </summary>
public static class AjisQueryExtensions
{
    /// <summary>
    /// Creates a queryable collection from an AJIS file with enhanced LINQ support.
    /// </summary>
    public static IQueryable<T> AsQueryable<T>(this string filePath) where T : notnull
    {
        return AjisQuery.FromFile<T>(filePath);
    }

    /// <summary>
    /// Creates an indexed queryable for fast lookups.
    /// </summary>
    public static IQueryable<T> AsQueryable<T>(this string filePath, string indexProperty) where T : notnull
    {
        return AjisQuery.FromFile<T>(filePath, indexProperty);
    }
}

/// <summary>
/// Enhanced query visitor with full LINQ support.
/// </summary>
internal class EnhancedAjisQueryVisitor<T> : ExpressionVisitor where T : notnull
{
    public Func<T, bool>? WherePredicate { get; private set; }
    public List<(LambdaExpression KeySelector, bool Descending)> OrderByExpressions { get; } = new();
    public int? SkipCount { get; private set; }
    public int? TakeCount { get; private set; }
    public LambdaExpression? SelectExpression { get; private set; }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Where" when node.Arguments.Count == 2:
                ParseWhere(node);
                break;

            case "OrderBy" when node.Arguments.Count == 2:
                ParseOrderBy(node, descending: false);
                break;

            case "OrderByDescending" when node.Arguments.Count == 2:
                ParseOrderBy(node, descending: true);
                break;

            case "ThenBy" when node.Arguments.Count == 2:
                ParseThenBy(node, descending: false);
                break;

            case "ThenByDescending" when node.Arguments.Count == 2:
                ParseThenBy(node, descending: true);
                break;

            case "Skip" when node.Arguments.Count == 2:
                ParseSkip(node);
                break;

            case "Take" when node.Arguments.Count == 2:
                ParseTake(node);
                break;

            case "Select" when node.Arguments.Count == 2:
                ParseSelect(node);
                break;
        }

        return base.VisitMethodCall(node);
    }

    private void ParseWhere(MethodCallExpression node)
    {
        var lambda = ExtractLambda(node.Arguments[1]);
        if (lambda != null)
        {
            var compiled = lambda.Compile() as Func<T, bool>;
            if (WherePredicate == null)
                WherePredicate = compiled;
            else
            {
                var existing = WherePredicate;
                WherePredicate = t => existing(t) && (compiled?.Invoke(t) ?? true);
            }
        }
    }

    private void ParseOrderBy(MethodCallExpression node, bool descending)
    {
        var lambda = ExtractLambda(node.Arguments[1]);
        if (lambda != null)
        {
            OrderByExpressions.Clear(); // OrderBy replaces previous ordering
            OrderByExpressions.Add((lambda, descending));
        }
    }

    private void ParseThenBy(MethodCallExpression node, bool descending)
    {
        var lambda = ExtractLambda(node.Arguments[1]);
        if (lambda != null)
        {
            OrderByExpressions.Add((lambda, descending));
        }
    }

    private void ParseSkip(MethodCallExpression node)
    {
        if (node.Arguments[1] is ConstantExpression constant && constant.Value is int skipValue)
        {
            SkipCount = skipValue;
        }
    }

    private void ParseTake(MethodCallExpression node)
    {
        if (node.Arguments[1] is ConstantExpression constant && constant.Value is int takeValue)
        {
            TakeCount = takeValue;
        }
    }

    private void ParseSelect(MethodCallExpression node)
    {
        SelectExpression = ExtractLambda(node.Arguments[1]);
    }

    private static LambdaExpression? ExtractLambda(Expression expression)
    {
        if (expression is UnaryExpression unary && unary.Operand is LambdaExpression lambda)
            return lambda;
        
        if (expression is LambdaExpression directLambda)
            return directLambda;

        return null;
    }

    /// <summary>
    /// Executes the complete query pipeline.
    /// </summary>
    public IEnumerable<TResult> ExecuteQuery<TResult>(string filePath)
    {
        // Start with base enumeration
        IEnumerable<T> result = AjisFile.Enumerate<T>(filePath);

        // Apply WHERE filter
        if (WherePredicate != null)
        {
            result = result.Where(WherePredicate);
        }

        // Apply ORDER BY
        if (OrderByExpressions.Count > 0)
        {
            IOrderedEnumerable<T>? ordered = null;

            for (int i = 0; i < OrderByExpressions.Count; i++)
            {
                var (keySelector, descending) = OrderByExpressions[i];
                var compiled = keySelector.Compile();

                if (i == 0)
                {
                    // First ordering
                    ordered = descending
                        ? result.OrderByDescending(t => compiled.DynamicInvoke(t))
                        : result.OrderBy(t => compiled.DynamicInvoke(t));
                }
                else if (ordered != null)
                {
                    // ThenBy
                    ordered = descending
                        ? ordered.ThenByDescending(t => compiled.DynamicInvoke(t))
                        : ordered.ThenBy(t => compiled.DynamicInvoke(t));
                }
            }

            result = ordered ?? result;
        }

        // Apply SKIP
        if (SkipCount.HasValue)
        {
            result = result.Skip(SkipCount.Value);
        }

        // Apply TAKE
        if (TakeCount.HasValue)
        {
            result = result.Take(TakeCount.Value);
        }

        // Apply SELECT projection
        if (SelectExpression != null)
        {
            var compiled = SelectExpression.Compile();
            var projected = result.Select(t => compiled.DynamicInvoke(t));
            return projected.Cast<TResult>();
        }

        return result.Cast<TResult>();
    }
}
