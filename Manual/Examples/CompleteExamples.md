# Complete AJIS Examples

Real-world examples showcasing all AJIS features together.

## E-Commerce Application

Complete implementation of an e-commerce system using AJIS.

### Data Models

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

### Product Management

```csharp
public class ProductManager
{
    private const string ProductsFile = "products.ajis";

    // Add new product
    public void AddProduct(Product product)
    {
        AjisFile.Append(ProductsFile, product);
    }

    // Add multiple products
    public void ImportProducts(List<Product> products)
    {
        AjisFile.AppendMany(ProductsFile, products);
    }

    // Get product by ID
    public Product? GetProduct(int id)
    {
        return AjisFile.FindByKey<Product>(ProductsFile, "Id", id);
    }

    // Update product
    public void UpdateProduct(Product product)
    {
        AjisFile.UpdateByKey(ProductsFile, "Id", product.Id, 
            p => {
                p.Name = product.Name;
                p.Price = product.Price;
                p.Stock = product.Stock;
            });
    }

    // Delete product
    public void DeleteProduct(int id)
    {
        AjisFile.DeleteByKey<Product>(ProductsFile, "Id", id);
    }

    // Search products
    public List<Product> SearchProducts(string query)
    {
        return AjisQuery.FromFile<Product>(ProductsFile)
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList();
    }

    // Get products by category
    public List<Product> GetByCategory(string category)
    {
        return AjisQuery.FromFile<Product>(ProductsFile)
            .Where(p => p.Category == category)
            .OrderByDescending(p => p.Rating)
            .ThenBy(p => p.Price)
            .ToList();
    }

    // Get top rated products
    public List<Product> GetTopRated(int count = 10)
    {
        return AjisQuery.FromFile<Product>(ProductsFile)
            .Where(p => p.Stock > 0)
            .OrderByDescending(p => p.Rating)
            .Take(count)
            .ToList();
    }

    // Get low stock products
    public List<Product> GetLowStock(int threshold = 10)
    {
        return AjisQuery.FromFile<Product>(ProductsFile)
            .Where(p => p.Stock < threshold && p.Stock > 0)
            .OrderBy(p => p.Stock)
            .ToList();
    }

    // Product statistics
    public ProductStats GetStats()
    {
        var products = AjisQuery.FromFile<Product>(ProductsFile);

        return new ProductStats
        {
            TotalProducts = products.Count(),
            InStock = products.Count(p => p.Stock > 0),
            OutOfStock = products.Count(p => p.Stock == 0),
            AveragePrice = products.Average(p => (double)p.Price),
            TotalValue = products.Sum(p => p.Price * p.Stock),
            MinPrice = products.Min(p => p.Price),
            MaxPrice = products.Max(p => p.Price),
            Categories = products.DistinctBy(p => p.Category).Count()
        };
    }

    // Category report
    public List<CategoryReport> GetCategoryReport()
    {
        return AjisQuery.FromFile<Product>(ProductsFile)
            .GroupBy(
                p => p.Category,
                (category, products) => new CategoryReport
                {
                    Category = category,
                    ProductCount = products.Count(),
                    TotalValue = products.Sum(p => p.Price * p.Stock),
                    AveragePrice = products.Average(p => (double)p.Price),
                    InStock = products.Count(p => p.Stock > 0),
                    TotalStock = products.Sum(p => p.Stock)
                })
            .OrderByDescending(r => r.TotalValue)
            .ToList();
    }
}

public class ProductStats
{
    public int TotalProducts { get; set; }
    public int InStock { get; set; }
    public int OutOfStock { get; set; }
    public double AveragePrice { get; set; }
    public decimal TotalValue { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int Categories { get; set; }
}

public class CategoryReport
{
    public string Category { get; set; } = "";
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public double AveragePrice { get; set; }
    public int InStock { get; set; }
    public int TotalStock { get; set; }
}
```

### Order Management

```csharp
public class OrderManager
{
    private const string OrdersFile = "orders.ajis";

    // Create new order
    public void CreateOrder(Order order)
    {
        AjisFile.Append(OrdersFile, order);
    }

    // Get order by ID
    public Order? GetOrder(int id)
    {
        return AjisFile.FindByKey<Order>(OrdersFile, "Id", id);
    }

    // Get customer orders
    public List<Order> GetCustomerOrders(int customerId)
    {
        return AjisQuery.FromFile<Order>(OrdersFile)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    // Get recent orders
    public List<Order> GetRecentOrders(int days = 7)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        
        return AjisQuery.FromFile<Order>(OrdersFile)
            .Where(o => o.OrderDate >= cutoff)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    // Update order status
    public void UpdateStatus(int orderId, string status)
    {
        AjisFile.UpdateByKey(OrdersFile, "Id", orderId,
            o => o.Status = status);
    }

    // Sales statistics
    public SalesStats GetSalesStats(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = AjisQuery.FromFile<Order>(OrdersFile);

        if (startDate.HasValue)
            query = (IQueryable<Order>)query.Where(o => o.OrderDate >= startDate.Value);
        
        if (endDate.HasValue)
            query = (IQueryable<Order>)query.Where(o => o.OrderDate <= endDate.Value);

        return new SalesStats
        {
            TotalOrders = query.Count(),
            TotalRevenue = query.Sum(o => o.Total),
            AverageOrderValue = query.Average(o => (double)o.Total),
            LargestOrder = query.Max(o => o.Total),
            UniqueCustomers = query.DistinctBy(o => o.CustomerId).Count()
        };
    }

    // Daily sales report
    public List<DailySales> GetDailySales(int days = 30)
    {
        var startDate = DateTime.Now.AddDays(-days).Date;

        return AjisQuery.FromFile<Order>(OrdersFile)
            .Where(o => o.OrderDate >= startDate)
            .GroupBy(
                o => o.OrderDate.Date,
                (date, orders) => new DailySales
                {
                    Date = date,
                    OrderCount = orders.Count(),
                    Revenue = orders.Sum(o => o.Total),
                    UniqueCustomers = orders.DistinctBy(o => o.CustomerId).Count()
                })
            .OrderBy(d => d.Date)
            .ToList();
    }
}

public class SalesStats
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageOrderValue { get; set; }
    public decimal LargestOrder { get; set; }
    public int UniqueCustomers { get; set; }
}

public class DailySales
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public int UniqueCustomers { get; set; }
}
```

## User Management System

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime RegisteredDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; }
    public int Points { get; set; }
    public string Role { get; set; } = "User";
}

public class UserManager
{
    private const string UsersFile = "users.ajis";

    // Fast indexed lookup
    private AjisFileIndex<User>? _emailIndex;

    public UserManager()
    {
        // Create index for fast email lookups
        _emailIndex = AjisFile.CreateIndex<User>(UsersFile, "Email");
        _emailIndex.Build();
    }

    // Register new user
    public void Register(User user)
    {
        // Check if email exists
        if (_emailIndex!.ContainsKey(user.Email))
            throw new InvalidOperationException("Email already registered");

        user.RegisteredDate = DateTime.Now;
        user.IsActive = true;

        AjisFile.Append(UsersFile, user);
        
        // Rebuild index
        _emailIndex.Build();
    }

    // Find by email (fast!)
    public User? FindByEmail(string email)
    {
        return _emailIndex!.FindByKey(email);
    }

    // Update last login
    public void RecordLogin(string email)
    {
        var user = FindByEmail(email);
        if (user != null)
        {
            AjisFile.UpdateByKey(UsersFile, "Email", email,
                u => u.LastLoginDate = DateTime.Now);
        }
    }

    // Get active users
    public List<User> GetActiveUsers()
    {
        return AjisQuery.FromFile<User>(UsersFile)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToList();
    }

    // Get top users by points
    public List<User> GetTopUsers(int count = 10)
    {
        return AjisQuery.FromFile<User>(UsersFile)
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.Points)
            .Take(count)
            .ToList();
    }

    // Get inactive users
    public List<User> GetInactiveUsers(int days = 30)
    {
        var cutoff = DateTime.Now.AddDays(-days);

        return AjisQuery.FromFile<User>(UsersFile)
            .Where(u => u.LastLoginDate < cutoff || !u.LastLoginDate.HasValue)
            .OrderBy(u => u.LastLoginDate)
            .ToList();
    }

    // User statistics
    public UserStats GetStats()
    {
        var users = AjisQuery.FromFile<User>(UsersFile);
        var now = DateTime.Now;

        return new UserStats
        {
            TotalUsers = users.Count(),
            ActiveUsers = users.Count(u => u.IsActive),
            NewThisMonth = users.Count(u => 
                u.RegisteredDate.Month == now.Month && 
                u.RegisteredDate.Year == now.Year),
            AveragePoints = users.Average(u => u.Points),
            TopUserPoints = users.Max(u => u.Points)
        };
    }
}

public class UserStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewThisMonth { get; set; }
    public double AveragePoints { get; set; }
    public int TopUserPoints { get; set; }
}
```

## Analytics Dashboard

```csharp
public class Dashboard
{
    public static void ShowDashboard()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  E-COMMERCE DASHBOARD                    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Product Stats
        var productManager = new ProductManager();
        var productStats = productManager.GetStats();

        Console.WriteLine("📦 PRODUCTS");
        Console.WriteLine($"   Total: {productStats.TotalProducts}");
        Console.WriteLine($"   In Stock: {productStats.InStock} ({productStats.InStock * 100.0 / productStats.TotalProducts:F1}%)");
        Console.WriteLine($"   Categories: {productStats.Categories}");
        Console.WriteLine($"   Inventory Value: ${productStats.TotalValue:N2}");
        Console.WriteLine($"   Avg Price: ${productStats.AveragePrice:F2}");
        Console.WriteLine();

        // Sales Stats
        var orderManager = new OrderManager();
        var salesStats = orderManager.GetSalesStats();

        Console.WriteLine("💰 SALES");
        Console.WriteLine($"   Total Orders: {salesStats.TotalOrders}");
        Console.WriteLine($"   Revenue: ${salesStats.TotalRevenue:N2}");
        Console.WriteLine($"   Avg Order: ${salesStats.AverageOrderValue:F2}");
        Console.WriteLine($"   Unique Customers: {salesStats.UniqueCustomers}");
        Console.WriteLine();

        // Top Categories
        var categoryReport = productManager.GetCategoryReport();
        Console.WriteLine("📊 TOP CATEGORIES");
        foreach (var cat in categoryReport.Take(5))
        {
            Console.WriteLine($"   {cat.Category}:");
            Console.WriteLine($"      Products: {cat.ProductCount}, Value: ${cat.TotalValue:N2}");
        }
        Console.WriteLine();

        // Recent Activity
        var recentOrders = orderManager.GetRecentOrders(7);
        Console.WriteLine($"🔔 RECENT ACTIVITY (Last 7 days): {recentOrders.Count} orders");
    }
}
```

## See Also

- [Quick Start](../GettingStarted/QuickStart.md) - Get started quickly
- [File Operations](../FileOperations/FileOperationsReference.md) - CRUD reference
- [Querying](Sorting.md) - Sorting and filtering
- [Aggregations](Aggregations.md) - Count, Sum, Average
- [Performance](../Performance/BestPractices.md) - Optimization tips
