# Aggregation Functions

Master data aggregations in AJIS - Count, Sum, Average, Min, Max, and more!

## Quick Overview

```csharp
using Afrowave.AJIS.IO;

// Count records
var count = AjisQuery.FromFile<Product>("products.ajis").Count();

// Check if any exist
var hasExpensive = AjisQuery.FromFile<Product>("products.ajis")
    .Any(p => p.Price > 100);

// Calculate sum
var totalValue = AjisQuery.FromFile<Product>("products.ajis")
    .Sum(p => p.Price * p.Stock);

// Get average
var avgPrice = AjisQuery.FromFile<Product>("products.ajis")
    .Average(p => (double)p.Price);

// Find min/max
var cheapest = AjisQuery.FromFile<Product>("products.ajis")
    .Min(p => p.Price);
```

## Count Operations

### Count All Records

```csharp
// Total number of products
var total = AjisQuery.FromFile<Product>("products.ajis").Count();
Console.WriteLine($"Total products: {total}");
```

### Count with Condition

```csharp
// Count products in stock
var inStock = AjisQuery.FromFile<Product>("products.ajis")
    .Count(p => p.Stock > 0);

Console.WriteLine($"Products in stock: {inStock}");
```

### LongCount for Large Datasets

```csharp
// Use LongCount for >2 billion records
var hugeCount = AjisQuery.FromFile<LogEntry>("logs.ajis").LongCount();
```

## Any and All

### Any - Check if Elements Exist

```csharp
// Check if file has any data
bool hasData = AjisQuery.FromFile<Product>("products.ajis").Any();

// Check if any product matches condition
bool hasExpensive = AjisQuery.FromFile<Product>("products.ajis")
    .Any(p => p.Price > 1000);

if (hasExpensive)
{
    Console.WriteLine("We have premium products!");
}
```

### All - Check if All Match

```csharp
// Check if all products are in stock
bool allInStock = AjisQuery.FromFile<Product>("products.ajis")
    .All(p => p.Stock > 0);

// Check if all users are active
bool allActive = AjisQuery.FromFile<User>("users.ajis")
    .All(u => u.IsActive);

if (!allActive)
{
    Console.WriteLine("Some users are inactive");
}
```

## Sum - Calculate Totals

### Sum Integer Values

```csharp
// Total stock across all products
var totalStock = AjisQuery.FromFile<Product>("products.ajis")
    .Sum(p => p.Stock);

Console.WriteLine($"Total inventory: {totalStock} units");
```

### Sum Decimal Values

```csharp
// Total value of all products
var totalValue = AjisQuery.FromFile<Product>("products.ajis")
    .Sum(p => p.Price);

Console.WriteLine($"Total value: ${totalValue:F2}");
```

### Calculated Sum

```csharp
// Total inventory value (price * stock)
var inventoryValue = AjisQuery.FromFile<Product>("products.ajis")
    .Sum(p => p.Price * p.Stock);

Console.WriteLine($"Inventory value: ${inventoryValue:F2}");
```

## Average - Calculate Means

### Average Price

```csharp
// Average product price
var avgPrice = AjisQuery.FromFile<Product>("products.ajis")
    .Average(p => (double)p.Price);

Console.WriteLine($"Average price: ${avgPrice:F2}");
```

### Average with Filter

```csharp
// Average price of products in stock
var avgPriceInStock = AjisQuery.FromFile<Product>("products.ajis")
    .Where(p => p.Stock > 0)
    .Average(p => (double)p.Price);
```

## Min and Max

### Find Minimum Value

```csharp
// Cheapest product
var minPrice = AjisQuery.FromFile<Product>("products.ajis")
    .Min(p => p.Price);

Console.WriteLine($"Cheapest product: ${minPrice}");

// Oldest user
var oldestDate = AjisQuery.FromFile<User>("users.ajis")
    .Min(u => u.RegisteredDate);
```

### Find Maximum Value

```csharp
// Most expensive product
var maxPrice = AjisQuery.FromFile<Product>("products.ajis")
    .Max(p => p.Price);

Console.WriteLine($"Most expensive: ${maxPrice}");

// User with most points
var maxPoints = AjisQuery.FromFile<User>("users.ajis")
    .Max(u => u.Points);
```

## Distinct - Remove Duplicates

### Distinct Values

```csharp
// Get unique product categories
var categories = AjisQuery.FromFile<Product>("products.ajis")
    .DistinctBy(p => p.Category)
    .Select(p => p.Category)
    .ToList();

foreach (var category in categories)
{
    Console.WriteLine($"- {category}");
}
```

### DistinctBy with Key Selector

```csharp
// Get one product per category (first occurrence)
var onePerCategory = AjisQuery.FromFile<Product>("products.ajis")
    .DistinctBy(p => p.Category)
    .ToList();

Console.WriteLine($"Categories: {onePerCategory.Count}");
```

## Real-World Examples

### E-Commerce Dashboard

```csharp
public class DashboardStats
{
    public static void ShowStats()
    {
        var products = "products.ajis";

        // Total products
        var total = AjisQuery.FromFile<Product>(products).Count();

        // Products in stock
        var inStock = AjisQuery.FromFile<Product>(products)
            .Count(p => p.Stock > 0);

        // Out of stock
        var outOfStock = total - inStock;

        // Average price
        var avgPrice = AjisQuery.FromFile<Product>(products)
            .Average(p => (double)p.Price);

        // Total inventory value
        var totalValue = AjisQuery.FromFile<Product>(products)
            .Sum(p => p.Price * p.Stock);

        // Price range
        var minPrice = AjisQuery.FromFile<Product>(products).Min(p => p.Price);
        var maxPrice = AjisQuery.FromFile<Product>(products).Max(p => p.Price);

        // Display
        Console.WriteLine("📊 DASHBOARD STATS");
        Console.WriteLine($"Total Products: {total}");
        Console.WriteLine($"In Stock: {inStock} ({inStock * 100.0 / total:F1}%)");
        Console.WriteLine($"Out of Stock: {outOfStock}");
        Console.WriteLine($"Average Price: ${avgPrice:F2}");
        Console.WriteLine($"Price Range: ${minPrice} - ${maxPrice}");
        Console.WriteLine($"Total Inventory Value: ${totalValue:F2}");
    }
}
```

### Sales Report

```csharp
public class SalesReport
{
    public static void MonthlyReport(DateTime month)
    {
        var orders = AjisQuery.FromFile<Order>("orders.ajis")
            .Where(o => o.OrderDate.Month == month.Month && 
                       o.OrderDate.Year == month.Year);

        // Total orders
        var totalOrders = orders.Count();

        // Total revenue
        var totalRevenue = orders.Sum(o => o.Total);

        // Average order value
        var avgOrderValue = orders.Average(o => (double)o.Total);

        // Largest order
        var largestOrder = orders.Max(o => o.Total);

        // Number of customers
        var uniqueCustomers = orders
            .DistinctBy(o => o.CustomerId)
            .Count();

        Console.WriteLine($"📈 SALES REPORT - {month:MMMM yyyy}");
        Console.WriteLine($"Total Orders: {totalOrders}");
        Console.WriteLine($"Total Revenue: ${totalRevenue:F2}");
        Console.WriteLine($"Average Order: ${avgOrderValue:F2}");
        Console.WriteLine($"Largest Order: ${largestOrder:F2}");
        Console.WriteLine($"Unique Customers: {uniqueCustomers}");
    }
}
```

### User Analytics

```csharp
public class UserAnalytics
{
    public static void ShowAnalytics()
    {
        var users = "users.ajis";

        // Total users
        var total = AjisQuery.FromFile<User>(users).Count();

        // Active users
        var active = AjisQuery.FromFile<User>(users)
            .Count(u => u.IsActive);

        // New users this month
        var thisMonth = AjisQuery.FromFile<User>(users)
            .Count(u => u.RegisteredDate.Month == DateTime.Now.Month &&
                       u.RegisteredDate.Year == DateTime.Now.Year);

        // Average points
        var avgPoints = AjisQuery.FromFile<User>(users)
            .Average(u => u.Points);

        // Top user points
        var maxPoints = AjisQuery.FromFile<User>(users)
            .Max(u => u.Points);

        // Users with no activity
        var inactive = AjisQuery.FromFile<User>(users)
            .Count(u => u.LastLoginDate < DateTime.Now.AddDays(-30));

        Console.WriteLine("👥 USER ANALYTICS");
        Console.WriteLine($"Total Users: {total}");
        Console.WriteLine($"Active: {active} ({active * 100.0 / total:F1}%)");
        Console.WriteLine($"New This Month: {thisMonth}");
        Console.WriteLine($"Average Points: {avgPoints:F0}");
        Console.WriteLine($"Top User Points: {maxPoints}");
        Console.WriteLine($"Inactive (>30 days): {inactive}");
    }
}
```

## Combining Aggregations

### Multiple Aggregations

```csharp
// Get comprehensive stats
var products = AjisQuery.FromFile<Product>("products.ajis").ToList();

var stats = new
{
    Count = products.Count,
    TotalValue = products.Sum(p => p.Price * p.Stock),
    AvgPrice = products.Average(p => (double)p.Price),
    MinPrice = products.Min(p => p.Price),
    MaxPrice = products.Max(p => p.Price),
    InStock = products.Count(p => p.Stock > 0)
};

Console.WriteLine($"Products: {stats.Count}");
Console.WriteLine($"Value: ${stats.TotalValue:F2}");
Console.WriteLine($"Avg Price: ${stats.AvgPrice:F2}");
Console.WriteLine($"Range: ${stats.MinPrice} - ${stats.MaxPrice}");
```

### Conditional Aggregations

```csharp
// Stats for different categories
var categories = AjisQuery.FromFile<Product>("products.ajis")
    .DistinctBy(p => p.Category)
    .Select(p => p.Category)
    .ToList();

foreach (var category in categories)
{
    var categoryProducts = AjisQuery.FromFile<Product>("products.ajis")
        .Where(p => p.Category == category);

    var count = categoryProducts.Count();
    var avgPrice = categoryProducts.Average(p => (double)p.Price);
    var totalStock = categoryProducts.Sum(p => p.Stock);

    Console.WriteLine($"{category}: {count} products, avg ${avgPrice:F2}, {totalStock} in stock");
}
```

## Performance Tips

### ✅ DO: Use Specific Aggregations

```csharp
// Good: Direct aggregation
var count = AjisQuery.FromFile<Product>("products.ajis").Count();
```

### ❌ DON'T: Materialize Then Aggregate

```csharp
// Bad: Loads everything into memory first
var count = AjisQuery.FromFile<Product>("products.ajis").ToList().Count;
```

### ✅ DO: Filter Before Aggregate

```csharp
// Good: Filter first, then aggregate
var avgExpensive = AjisQuery.FromFile<Product>("products.ajis")
    .Where(p => p.Price > 100)  // Filter first
    .Average(p => (double)p.Price);
```

### ✅ DO: Use Any for Existence Checks

```csharp
// Good: Stops at first match
if (AjisQuery.FromFile<Product>("products.ajis").Any(p => p.Stock == 0))
{
    Console.WriteLine("Some products are out of stock");
}
```

### ❌ DON'T: Use Count > 0 for Existence

```csharp
// Bad: Counts everything
if (AjisQuery.FromFile<Product>("products.ajis").Count(p => p.Stock == 0) > 0)
{
    // ...
}
```

## See Also

- [Querying Data](BasicQueries.md) - Filter and find
- [Sorting](Sorting.md) - Order results
- [LINQ Support](LINQSupport.md) - Complete LINQ reference
- [Performance](../Performance/BestPractices.md) - Optimization tips
