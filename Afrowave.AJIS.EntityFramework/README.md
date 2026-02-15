# Afrowave.AJIS.EntityFramework

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.EntityFramework.svg)](https://www.nuget.org/packages/Afrowave.AJIS.EntityFramework)

Entity Framework Core integration for the AJIS (.NET) framework. Provides value converters for storing complex types as AJIS in database columns.

## Overview

The EntityFramework library enables storing AJIS-serialized objects in Entity Framework Core databases:

* **Value converters** - EF Core `ValueConverter` for AJIS serialization
* **Configuration API** - Simple configuration with `UseAjisSerialization()`
* **LINQ support** - Query support for AJIS properties
* **Complex type mapping** - Nested objects, collections, and dictionaries
* **Database agnostic** - Works with all EF Core providers (SQL Server, PostgreSQL, SQLite, etc.)

## Features

* **Value converter** - `AjisValueConverter<T>` for automatic serialization
* **Collection converter** - `AjisCollectionConverter<T>` for collections
* ** DbContext base** - `AjisDbContext` with automatic configuration
* **Repository pattern** - `AjisFileRepository<T>` for file-based storage
* **LINQ translation** - Query support for AJIS properties
* **Migration support** - Works with EF Core migrations
* **Type mapping** - Full support for AJIS type mapping features

## Installation

```bash
dotnet add package Afrowave.AJIS.EntityFramework
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| Afrowave.AJIS.Core | >= 1.0.0 | Core contracts and settings |
| Afrowave.AJIS.Serialization | >= 1.0.0 | Serializer and value types |
| Microsoft.EntityFrameworkCore | >= 10.0.0 | EF Core core package |
| Microsoft.EntityFrameworkCore.Relational | >= 10.0.0 | Relational database support |

## Library Summary

| Feature | Description |
|---------|-------------|
| Target Framework | .NET 10.0 |
| Serialization Format | AJIS (custom format) |
| Key Components | AjisValueConverter<T>, AjisCollectionConverter<T> |
| LINQ Support | Full query translation |
| Database Support | All EF Core providers |
| Nullable Reference Types | Enabled |

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisValueConverter<T>` | EF Core value converter for AJIS |
| `AjisCollectionConverter<T>` | Converter for collections |
| `AjisDbContext` | Base DbContext with AJIS support |
| `AjisFileRepository<T>` | File-based repository |
| `AjisModelBuilderExtensions` | Model configuration extensions |

## Usage

### Basic configuration

```csharp
public class UserProfile
{
    public int Id { get; set; }
    public string Username { get; set; }

    // Complex object stored as AJIS
    public UserPreferences Preferences { get; set; }
}

public class AppDbContext : AjisDbContext
{
    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AJIS serialization
        modelBuilder.Entity<UserProfile>()
            .Property(e => e.Preferences)
            .UseAjisSerialization();
    }
}
```

### Repository usage

```csharp
public class UserRepository : AjisFileRepository<User>
{
    public UserRepository() : base("users.ajis") { }
}

// Usage
var repo = new UserRepository();
await repo.InsertAsync(new User { Name = "Alice" });
var user = await repo.GetByIdAsync(1);
```

### LINQ queries

```csharp
var users = context.Users
    .Where(u => u.Preferences.DarkMode)
    .OrderBy(u => u.Username)
    .ToList();
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [EF Core Integration](../../Docs/M10_EF_Core_Integration_Design.md) - EF Core design details
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide

## Compatibility

* .NET 10.0+
* Entity Framework Core 10.0+
* Nullable reference types enabled

## License

MIT
