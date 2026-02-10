# Sorting Data

Learn how to sort and order AJIS data efficiently, just like with EF Core.

## Quick Example

```csharp
using Afrowave.AJIS.IO;

// Sort users by age (ascending)
var users = AjisQuery.FromFile<User>("users.ajis")
    .OrderBy(u => u.Age)
    .ToList();

// Sort by name (descending)
var sorted = AjisQuery.FromFile<User>("users.ajis")
    .OrderByDescending(u => u.Name)
    .ToList();
```

## Sorting Methods

### OrderBy - Ascending Sort

```csharp
// Single property
var byAge = AjisQuery.FromFile<Product>("products.ajis")
    .OrderBy(p => p.Price)
    .ToList();

// Multiple properties (then by)
var sorted = AjisQuery.FromFile<Product>("products.ajis")
    .OrderBy(p => p.Category)
    .ThenBy(p => p.Name)
    .ToList();
```

### OrderByDescending - Descending Sort

```csharp
// Most expensive first
var expensive = AjisQuery.FromFile<Product>("products.ajis")
    .OrderByDescending(p => p.Price)
    .ToList();

// Newest first, then by name
var recent = AjisQuery.FromFile<Product>("products.ajis")
    .OrderByDescending(p => p.CreatedDate)
    .ThenBy(p => p.Name)
    .ToList();
```

## Complete Examples

### E-Commerce: Sort Products

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
    public int Stock { get; set; }
    public double Rating { get; set; }
}

// Sort by multiple criteria
var products = AjisQuery.FromFile<Product>("products.ajis")
    .Where(p => p.Stock > 0)                    // In stock only
    .OrderByDescending(p => p.Rating)           // Best rated first
    .ThenBy(p => p.Price)                       // Then cheapest
    .Take(10)                                   // Top 10
    .ToList();

foreach (var product in products)
{
    Console.WriteLine($"{product.Name} - ${product.Price} (★{product.Rating})");
}
```

### Users: Sort and Page

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime RegisteredDate { get; set; }
    public int Points { get; set; }
}

// Paginated results
int pageSize = 20;
int pageNumber = 1;

var users = AjisQuery.FromFile<User>("users.ajis")
    .OrderByDescending(u => u.Points)           // Top users first
    .Skip((pageNumber - 1) * pageSize)          // Skip previous pages
    .Take(pageSize)                             // Take current page
    .ToList();

Console.WriteLine($"Page {pageNumber}:");
foreach (var user in users)
{
    Console.WriteLine($"{user.Name}: {user.Points} points");
}
```

### Logs: Sort by Time

```csharp
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}

// Recent errors first
var recentErrors = AjisQuery.FromFile<LogEntry>("app.log.ajis")
    .Where(log => log.Level == "ERROR")
    .OrderByDescending(log => log.Timestamp)
    .Take(100)
    .ToList();

Console.WriteLine("Recent Errors:");
foreach (var log in recentErrors)
{
    Console.WriteLine($"[{log.Timestamp}] {log.Message}");
}
```

## Advanced Sorting

### Custom Comparers

```csharp
// Case-insensitive sort
var sorted = AjisQuery.FromFile<User>("users.ajis")
    .ToList()
    .OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase);
```

### Complex Sort Logic

```csharp
// Sort by calculated values
var users = AjisQuery.FromFile<User>("users.ajis")
    .ToList()
    .OrderByDescending(u => CalculateScore(u))
    .ToList();

int CalculateScore(User user)
{
    return user.Points * 2 + user.Badges.Count * 10;
}
```

### Null Handling

```csharp
// Nulls last
var sorted = AjisQuery.FromFile<Product>("products.ajis")
    .ToList()
    .OrderBy(p => p.Description ?? "zzz")  // Null values at end
    .ToList();
```

## Performance Tips

### ✅ DO: Use Indexed Sorting

```csharp
// Create index on sort property
using var index = AjisFile.CreateIndex<User>("users.ajis", "Age");
index.Build();

// Fast sorted retrieval
var sortedUsers = index.GetKeys()
    .OrderBy(key => key)
    .Select(key => index.FindByKey(key))
    .ToList();
```

### ✅ DO: Filter Before Sorting

```csharp
// Good: Filter first, then sort
var active = AjisQuery.FromFile<User>("users.ajis")
    .Where(u => u.IsActive)          // Filter first (fewer items)
    .OrderBy(u => u.Name)            // Then sort
    .ToList();
```

### ❌ DON'T: Sort Before Filtering

```csharp
// Bad: Sort all, then filter
var active = AjisQuery.FromFile<User>("users.ajis")
    .OrderBy(u => u.Name)            // Sorts everything!
    .Where(u => u.IsActive)          // Then filters
    .ToList();
```

### ✅ DO: Use Take() for Top-N

```csharp
// Good: Only sort what you need
var top10 = AjisQuery.FromFile<Product>("products.ajis")
    .OrderByDescending(p => p.Sales)
    .Take(10)                        // Only materialize top 10
    .ToList();
```

## Sorting Large Files

For very large files (>100MB), consider:

### 1. External Sort

```csharp
// Sort in chunks and merge
public static IEnumerable<T> SortLargeFile<T>(string filePath, 
    Func<T, IComparable> keySelector) where T : notnull
{
    const int chunkSize = 10000;
    var chunks = new List<List<T>>();

    // Read and sort chunks
    foreach (var chunk in AjisFile.Enumerate<T>(filePath).Chunk(chunkSize))
    {
        var sorted = chunk.OrderBy(keySelector).ToList();
        chunks.Add(sorted);
    }

    // Merge sorted chunks
    return MergeSortedChunks(chunks, keySelector);
}

static IEnumerable<T> MergeSortedChunks<T>(List<List<T>> chunks, 
    Func<T, IComparable> keySelector)
{
    var enumerators = chunks.Select(c => c.GetEnumerator()).ToList();
    var heap = new SortedSet<(IComparable key, int chunkIndex, T value)>();

    // Initialize heap
    for (int i = 0; i < enumerators.Count; i++)
    {
        if (enumerators[i].MoveNext())
        {
            var value = enumerators[i].Current;
            heap.Add((keySelector(value), i, value));
        }
    }

    // Merge
    while (heap.Count > 0)
    {
        var min = heap.Min;
        heap.Remove(min);
        yield return min.value;

        if (enumerators[min.chunkIndex].MoveNext())
        {
            var value = enumerators[min.chunkIndex].Current;
            heap.Add((keySelector(value), min.chunkIndex, value));
        }
    }
}
```

### 2. Index-Based Sorting

```csharp
// Build sorted index without loading all data
public class SortedIndex<T, TKey> where T : notnull
{
    private readonly SortedDictionary<TKey, List<long>> _index = new();
    
    public void BuildFromFile(string filePath, Func<T, TKey> keySelector)
    {
        long position = 0;
        foreach (var item in AjisFile.Enumerate<T>(filePath))
        {
            var key = keySelector(item);
            if (!_index.ContainsKey(key))
                _index[key] = new List<long>();
            
            _index[key].Add(position);
            position++;
        }
    }
    
    public IEnumerable<T> GetSorted(string filePath)
    {
        foreach (var kvp in _index)
        {
            foreach (var pos in kvp.Value)
            {
                yield return ReadAtPosition(filePath, pos);
            }
        }
    }
}
```

## Combining Sorting with Other Operations

### Filtering + Sorting + Paging

```csharp
var query = AjisQuery.FromFile<Product>("products.ajis")
    .Where(p => p.Category == "Electronics")  // Filter
    .Where(p => p.Stock > 0)                  // More filters
    .OrderByDescending(p => p.Rating)         // Sort
    .ThenBy(p => p.Price)                     // Then by
    .Skip(20)                                 // Page 2
    .Take(10)                                 // 10 per page
    .Select(p => new                          // Project
    {
        p.Name,
        p.Price,
        p.Rating
    })
    .ToList();
```

### Grouping + Sorting

```csharp
// Top 3 products per category
var topByCategory = AjisQuery.FromFile<Product>("products.ajis")
    .ToList()
    .GroupBy(p => p.Category)
    .SelectMany(g => g
        .OrderByDescending(p => p.Sales)
        .Take(3)
        .Select(p => new
        {
            Category = g.Key,
            p.Name,
            p.Sales
        }))
    .ToList();
```

## EF Core-like Examples

### Just like DbSet

```csharp
// EF Core style
var users = AjisQuery.FromFile<User>("users.ajis")
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .ThenByDescending(u => u.JoinDate)
    .Select(u => new { u.Id, u.Name, u.Email })
    .ToList();

// Same as:
// var users = dbContext.Users
//     .Where(u => u.IsActive)
//     .OrderBy(u => u.Name)
//     .ThenByDescending(u => u.JoinDate)
//     .Select(u => new { u.Id, u.Name, u.Email })
//     .ToList();
```

---

**See Also:**
- [Filtering](Filtering.md) - Filter data
- [Aggregations](Aggregations.md) - Count, sum, etc.
- [LINQ Support](LINQSupport.md) - Full LINQ reference
- [Performance](../Performance/BestPractices.md) - Optimization tips
