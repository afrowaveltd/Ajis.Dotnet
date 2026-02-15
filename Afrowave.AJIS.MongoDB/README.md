# Afrowave.AJIS.MongoDB

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.MongoDB.svg)](https://www.nuget.org/packages/Afrowave.AJIS.MongoDB)

MongoDB integration for the AJIS (.NET) framework. Provides BSON serializers and collection operations for seamless document storage.

## Overview

The MongoDB library enables storing AJIS-serialized objects in MongoDB documents:

* **BSON serializers** - Custom MongoDB BSON serializers for AJIS
* **Collection operations** - High-level CRUD operations
* **Aggregation pipeline** - Full support for MongoDB aggregations
* **Async streaming** - Efficient operations over large datasets
* **Type mapping** - Full support for AJIS type mapping features

## Features

* **BSON serializer** - `AjisBsonSerializer<T>` for document serialization
* **Collection wrapper** - `AjisMongoCollection<T>` for simplified operations
* **Repository pattern** - `AjisMongoRepository<T>` for data access
* **Aggregation support** - Pipeline operators and group operations
* **Index support** - MongoDB index configuration
* **Bulk operations** - Efficient batch insert/update/delete
* **Filter support** - Strongly-typed filters with AJIS properties

## Installation

```bash
dotnet add package Afrowave.AJIS.MongoDB
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| Afrowave.AJIS.Core | >= 1.0.0 | Core contracts and settings |
| Afrowave.AJIS.Serialization | >= 1.0.0 | Serializer and value types |
| MongoDB.Driver | >= 3.6.0 | MongoDB .NET driver |

## Library Summary

| Feature | Description |
|---------|-------------|
| Target Framework | .NET 10.0 |
| Serialization Format | BSON (MongoDB format) |
| Key Components | AjisBsonSerializer<T>, AjisMongoCollection<T> |
| Aggregation Support | Full pipeline operators |
| Collection Operations | Full CRUD operations |
| Nullable Reference Types | Enabled |

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisBsonSerializer<T>` | BSON serializer for AJIS |
| `AjisMongoCollection<T>` | MongoDB collection wrapper |
| `AjisMongoRepository<T>` | Repository for CRUD operations |
| `AjisMongoExtensions` | Extension methods for registration |

## Usage

### Basic configuration

```csharp
// Register AJIS serializers
AjisMongoExtensions.RegisterAjisSerializers();

// Repository
public class UserRepository : AjisMongoRepository<User>
{
    public UserRepository(IMongoDatabase database)
        : base(database, "users") { }
}

// Usage
var repository = new UserRepository(database);

// Insert document
await repository.InsertAsync(new User { Name = "John", Email = "john@example.com" });

// Find by ID
var user = await repository.GetByIdAsync(1);

// Complex queries
var activeUsers = await repository.FindAsync(u => u.IsActive && u.Age > 18);
```

### Aggregation pipeline

```csharp
var result = await repository.Aggregate()
    .Match(u => u.IsActive)
    .Group(u => u.Role, g => new { Role = g.Key, Count = g.Count() })
    .ToListAsync();
```

### Bulk operations

```csharp
var users = Enumerable.Range(1, 1000)
    .Select(i => new User { Name = $"User{i}", Age = i });

await repository.BulkInsertAsync(users);
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [MongoDB Integration](../../Docs/api/README.md#mongodb) - MongoDB documentation
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide

## Compatibility

* .NET 10.0+
* MongoDB.Driver 3.6+
* Nullable reference types enabled

## License

MIT
