# File Operations Reference

Complete guide to CRUD operations with AJIS files.

## Overview

AJIS provides intuitive file operations similar to working with a database:

```csharp
using Afrowave.AJIS.IO;

// CREATE
AjisFile.Create("data.ajis", items);

// READ
var all = AjisFile.ReadAll<Item>("data.ajis");
var one = AjisFile.FindByKey<Item>("data.ajis", "Id", 123);

// UPDATE
AjisFile.Update("data.ajis", "Id", 123, updatedItem);

// DELETE
AjisFile.DeleteByKey<Item>("data.ajis", "Id", 123);
```

## Creating Files

### Create from List

```csharp
var users = new List<User>
{
    new User { Id = 1, Name = "Alice" },
    new User { Id = 2, Name = "Bob" }
};

AjisFile.Create("users.ajis", users);
```

### Create from IEnumerable

```csharp
IEnumerable<Product> GetProducts()
{
    yield return new Product { Id = 1, Name = "Laptop" };
    yield return new Product { Id = 2, Name = "Mouse" };
}

AjisFile.Create("products.ajis", GetProducts());
```

### Create Async from AsyncEnumerable

```csharp
async IAsyncEnumerable<LogEntry> GetLogsAsync()
{
    await foreach (var log in fetchLogsFromApiAsync())
    {
        yield return log;
    }
}

await AjisFile.CreateAsync("logs.ajis", GetLogsAsync());
```

### Append to Existing File

```csharp
// Add single item
var newUser = new User { Id = 3, Name = "Charlie" };
AjisFile.Append("users.ajis", newUser);

// Add multiple items
var newUsers = new[] 
{
    new User { Id = 4, Name = "Diana" },
    new User { Id = 5, Name = "Eve" }
};
AjisFile.AppendMany("users.ajis", newUsers);
```

## Reading Files

### Read All Records

```csharp
// Load everything into memory
var users = AjisFile.ReadAll<User>("users.ajis");

Console.WriteLine($"Total users: {users.Count}");
foreach (var user in users)
{
    Console.WriteLine($"{user.Id}: {user.Name}");
}
```

### Enumerate (Streaming)

```csharp
// Memory-efficient streaming
foreach (var user in AjisFile.Enumerate<User>("users.ajis"))
{
    Console.WriteLine($"{user.Id}: {user.Name}");
    // Process one at a time - low memory usage
}
```

### Find by Key

```csharp
// Find single record by key property
var user = AjisFile.FindByKey<User>("users.ajis", "Id", 1);
if (user != null)
{
    Console.WriteLine($"Found: {user.Name}");
}
```

### Find by Predicate

```csharp
// Find all matching records
var activeUsers = AjisFile.FindByPredicate<User>(
    "users.ajis",
    u => u.IsActive && u.Age > 18
);

foreach (var user in activeUsers)
{
    Console.WriteLine(user.Name);
}
```

### First or Default

```csharp
// Find first matching record
var admin = AjisFile.FirstOrDefault<User>(
    "users.ajis",
    u => u.Role == "Admin"
);

if (admin != null)
{
    Console.WriteLine($"Admin: {admin.Name}");
}
```

## Updating Records

### Update by Key

```csharp
// Find and update
var user = AjisFile.FindByKey<User>("users.ajis", "Id", 1);
if (user != null)
{
    user.Name = "Alice Smith";
    user.Email = "alice.smith@example.com";
    
    AjisFile.Update("users.ajis", "Id", 1, user);
}
```

### Update Multiple

```csharp
// Update all matching records
AjisFile.UpdateWhere<User>(
    "users.ajis",
    u => u.IsActive == false,  // Where clause
    u => { u.IsActive = true; return u; }  // Update action
);
```

### Upsert (Insert or Update)

```csharp
// Update if exists, insert if not
var user = new User { Id = 1, Name = "Alice" };

AjisFile.Upsert("users.ajis", "Id", 1, user);
```

## Deleting Records

### Delete by Key

```csharp
// Delete single record
AjisFile.DeleteByKey<User>("users.ajis", "Id", 1);
```

### Delete by Predicate

```csharp
// Delete all matching
AjisFile.DeleteWhere<User>(
    "users.ajis",
    u => u.LastLogin < DateTime.Now.AddDays(-365)
);
```

### Clear All

```csharp
// Delete all records (file remains)
AjisFile.Clear<User>("users.ajis");

// Or delete the file entirely
File.Delete("users.ajis");
```

## Indexing for Fast Lookups

### Create Index

```csharp
// Create index on Id property
using var index = AjisFile.CreateIndex<User>("users.ajis", "Id");
index.Build();

// Fast lookup by Id
var user = index.FindByKey(123);
```

### Multiple Indexes

```csharp
// Index by Id
using var idIndex = AjisFile.CreateIndex<User>("users.ajis", "Id");
idIndex.Build();

// Index by Email
using var emailIndex = AjisFile.CreateIndex<User>("users.ajis", "Email");
emailIndex.Build();

// Fast lookups
var userById = idIndex.FindByKey(123);
var userByEmail = emailIndex.FindByKey("alice@example.com");
```

### Check if Key Exists

```csharp
using var index = AjisFile.CreateIndex<User>("users.ajis", "Id");
index.Build();

if (index.ContainsKey(123))
{
    Console.WriteLine("User exists!");
}
```

## Batch Operations

### Batch Insert

```csharp
var users = Enumerable.Range(1, 10000)
    .Select(i => new User { Id = i, Name = $"User{i}" });

// Efficient batch write
AjisFile.Create("users.ajis", users);
```

### Batch Update

```csharp
// Read, modify, write back
var users = AjisFile.ReadAll<User>("users.ajis");

foreach (var user in users)
{
    if (user.Email == null)
        user.Email = $"{user.Name.ToLower()}@example.com";
}

AjisFile.Create("users.ajis", users);
```

## Transactions (Atomic Updates)

```csharp
// Backup before update
var backupPath = "users.ajis.bak";
File.Copy("users.ajis", backupPath, overwrite: true);

try
{
    // Perform updates
    var users = AjisFile.ReadAll<User>("users.ajis");
    
    foreach (var user in users)
    {
        user.UpdatedAt = DateTime.Now;
    }
    
    AjisFile.Create("users.ajis", users);
    
    // Success - delete backup
    File.Delete(backupPath);
}
catch
{
    // Failure - restore backup
    File.Copy(backupPath, "users.ajis", overwrite: true);
    File.Delete(backupPath);
    throw;
}
```

## File Management

### Check if File Exists

```csharp
if (File.Exists("users.ajis"))
{
    var users = AjisFile.ReadAll<User>("users.ajis");
}
```

### Get File Info

```csharp
var fileInfo = new FileInfo("users.ajis");
Console.WriteLine($"Size: {fileInfo.Length / 1024} KB");
Console.WriteLine($"Modified: {fileInfo.LastWriteTime}");
```

### Copy/Move Files

```csharp
// Copy
File.Copy("users.ajis", "users_backup.ajis");

// Move/Rename
File.Move("users.ajis", "customers.ajis");
```

## Performance Tips

### ✅ DO: Use Enumeration for Large Files

```csharp
// Good: Streams data
foreach (var user in AjisFile.Enumerate<User>("large.ajis"))
{
    ProcessUser(user);
}
```

### ❌ DON'T: Load Everything for Large Files

```csharp
// Bad: Loads everything into memory
var allUsers = AjisFile.ReadAll<User>("large.ajis");  // Could be GB!
```

### ✅ DO: Use Indexes for Frequent Lookups

```csharp
// Good: Build index once
using var index = AjisFile.CreateIndex<User>("users.ajis", "Id");
index.Build();

for (int i = 0; i < 1000; i++)
{
    var user = index.FindByKey(i);  // Fast!
}
```

### ❌ DON'T: Scan Without Index

```csharp
// Bad: Scans entire file each time
for (int i = 0; i < 1000; i++)
{
    var user = AjisFile.FindByKey<User>("users.ajis", "Id", i);  // Slow!
}
```

## Complete Example

```csharp
using Afrowave.AJIS.IO;

public class UserManager
{
    private const string FilePath = "users.ajis";

    public void Initialize()
    {
        if (!File.Exists(FilePath))
        {
            AjisFile.Create(FilePath, new List<User>());
        }
    }

    public void AddUser(User user)
    {
        AjisFile.Append(FilePath, user);
    }

    public User? GetUser(int id)
    {
        return AjisFile.FindByKey<User>(FilePath, "Id", id);
    }

    public List<User> GetActiveUsers()
    {
        return AjisFile.FindByPredicate<User>(
            FilePath,
            u => u.IsActive
        ).ToList();
    }

    public void UpdateUser(User user)
    {
        AjisFile.Update(FilePath, "Id", user.Id, user);
    }

    public void DeactivateUser(int id)
    {
        var user = GetUser(id);
        if (user != null)
        {
            user.IsActive = false;
            UpdateUser(user);
        }
    }

    public void DeleteUser(int id)
    {
        AjisFile.DeleteByKey<User>(FilePath, "Id", id);
    }

    public int GetUserCount()
    {
        return AjisFile.ReadAll<User>(FilePath).Count;
    }
}
```

---

**See Also:**
- [Querying Data](../Querying/BasicQueries.md)
- [Indexing](Indexing.md)
- [Performance Best Practices](../Performance/BestPractices.md)
