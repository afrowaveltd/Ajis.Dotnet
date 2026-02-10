# Quick Start Guide

Get started with AJIS in 5 minutes!

## Installation

Add AJIS to your project:

```bash
dotnet add package Afrowave.AJIS.IO
dotnet add package Afrowave.AJIS.Serialization
```

## Your First AJIS File

### 1. Create a Model

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}
```

### 2. Write Data

```csharp
using Afrowave.AJIS.IO;

// Create some users
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com", Age = 30, IsActive = true },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com", Age = 25, IsActive = true },
    new User { Id = 3, Name = "Charlie", Email = "charlie@example.com", Age = 35, IsActive = false }
};

// Write to file
AjisFile.Create("users.ajis", users);
```

**Output file** (`users.ajis`):
```json
[
    {"Id":1,"Name":"Alice","Email":"alice@example.com","Age":30,"IsActive":true},
    {"Id":2,"Name":"Bob","Email":"bob@example.com","Age":25,"IsActive":true},
    {"Id":3,"Name":"Charlie","Email":"charlie@example.com","Age":35,"IsActive":false}
]
```

### 3. Read Data

```csharp
// Read all users
var allUsers = AjisFile.ReadAll<User>("users.ajis");

foreach (var user in allUsers)
{
    Console.WriteLine($"{user.Name} - {user.Email}");
}
```

### 4. Query Data

```csharp
// Find active users over 25
var activeUsers = AjisFile.FindByPredicate<User>(
    "users.ajis",
    u => u.IsActive && u.Age > 25
);

foreach (var user in activeUsers)
{
    Console.WriteLine($"{user.Name} is {user.Age} years old");
}
// Output:
// Alice is 30 years old
```

### 5. Use LINQ

```csharp
// Query with LINQ
var query = from u in AjisQuery.FromFile<User>("users.ajis")
            where u.Age > 25 && u.IsActive
            orderby u.Name
            select new { u.Name, u.Email };

foreach (var item in query)
{
    Console.WriteLine($"{item.Name} - {item.Email}");
}
// Output:
// Alice - alice@example.com
```

### 6. Update Records

```csharp
// Update a user
var user = AjisFile.FindByKey<User>("users.ajis", "Id", 1);
if (user != null)
{
    user.Age = 31;
    AjisFile.Update("users.ajis", "Id", 1, user);
}
```

### 7. Add Records

```csharp
// Add a new user
var newUser = new User 
{ 
    Id = 4, 
    Name = "Diana", 
    Email = "diana@example.com", 
    Age = 28, 
    IsActive = true 
};

AjisFile.Append("users.ajis", newUser);
```

### 8. Delete Records

```csharp
// Delete a user by ID
AjisFile.DeleteByKey<User>("users.ajis", "Id", 3);
```

## Fast Lookups with Indexing

For large files, create an index for fast lookups:

```csharp
// Create an index on the Id property
using var index = AjisFile.CreateIndex<User>("users.ajis", "Id");
index.Build();

// Fast lookup by Id
var user = index.FindByKey(2);
if (user != null)
{
    Console.WriteLine($"Found: {user.Name}");
}
```

## Complete Example

Here's a complete working example:

```csharp
using Afrowave.AJIS.IO;

// Define model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

class Program
{
    static void Main()
    {
        // Create sample data
        var products = new List<Product>
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.99m, Stock = 5 },
            new Product { Id = 2, Name = "Mouse", Price = 25.50m, Stock = 50 },
            new Product { Id = 3, Name = "Keyboard", Price = 75.00m, Stock = 30 }
        };

        // Write to file
        AjisFile.Create("products.ajis", products);
        Console.WriteLine("✅ Created products.ajis");

        // Read all
        var allProducts = AjisFile.ReadAll<Product>("products.ajis");
        Console.WriteLine($"📦 Total products: {allProducts.Count}");

        // Query with LINQ
        var expensiveProducts = from p in AjisQuery.FromFile<Product>("products.ajis")
                               where p.Price > 50
                               orderby p.Price descending
                               select p;

        Console.WriteLine("\n💰 Expensive products:");
        foreach (var p in expensiveProducts)
        {
            Console.WriteLine($"  {p.Name}: ${p.Price}");
        }

        // Find by predicate
        var inStock = AjisFile.FindByPredicate<Product>(
            "products.ajis",
            p => p.Stock > 10
        );

        Console.WriteLine($"\n📊 In stock (>10): {inStock.Count()} items");

        // Fast indexed lookup
        using var index = AjisFile.CreateIndex<Product>("products.ajis", "Id");
        index.Build();

        var laptop = index.FindByKey(1);
        if (laptop != null)
        {
            Console.WriteLine($"\n🔍 Found: {laptop.Name} - ${laptop.Price}");
        }
    }
}
```

**Output:**
```
✅ Created products.ajis
📦 Total products: 3

💰 Expensive products:
  Laptop: $999.99
  Keyboard: $75.00

📊 In stock (>10): 2 items

🔍 Found: Laptop - $999.99
```

## Next Steps

Now that you've got the basics, explore:

- **[File Operations](../FileOperations/Creating.md)** - CRUD in detail
- **[Querying](../Querying/BasicQueries.md)** - Advanced queries
- **[Performance](../Performance/BestPractices.md)** - Optimization tips
- **[ATP Tooling](../ATPTooling/Overview.md)** - Binary attachments

## Common Patterns

### Configuration Files

```csharp
public class AppConfig
{
    public string AppName { get; set; } = "";
    public string Version { get; set; } = "";
    public Dictionary<string, string> Settings { get; set; } = new();
}

// Save config
var config = new AppConfig 
{ 
    AppName = "MyApp", 
    Version = "1.0.0",
    Settings = new() { ["Theme"] = "Dark", ["Language"] = "en" }
};

AjisFile.Create("config.ajis", new[] { config });

// Load config
var loadedConfig = AjisFile.ReadAll<AppConfig>("config.ajis").First();
```

### Logging

```csharp
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}

// Append logs
AjisFile.Append("app.log.ajis", new LogEntry
{
    Timestamp = DateTime.Now,
    Level = "INFO",
    Message = "Application started"
});
```

### Data Export

```csharp
// Export database query to AJIS
var customers = dbContext.Customers.ToList();
AjisFile.Create("customers_export.ajis", customers);
```

---

**Next:** [Installation Guide](Installation.md) | [Your First App](FirstApp.md)
