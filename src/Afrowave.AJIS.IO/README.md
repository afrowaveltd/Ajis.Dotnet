# Afrowave.AJIS.IO

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.IO.svg)](https://www.nuget.org/packages/Afrowave.AJIS.IO)

File-level operations and streaming I/O for the AJIS (.NET) framework. Provides high-performance file CRUD operations, indexed lookups, LINQ queries, and async streaming I/O for processing large datasets.

## Overview

The IO library enables file-based data storage using the AJIS format with enterprise-grade features:

* **File-level operations** - Create, read, update, delete operations for AJIS files
* **Indexed lookups** - Fast key-based lookups (up to 10x faster than enumeration)
* **LINQ support** - Query AJIS files using LINQ expressions
* **Async streaming** - Memory-efficient processing of multi-GB files
* **Partial I/O** - Search, replace, and read specific segments without loading entire file
* **Memory-mapped files** - Support for files larger than available memory

## Features

* **High-level file operations** - `AjisFile` static class with CRUD methods
* **Indexed access** - `AjisFileIndex<T>` for O(1) key-based lookups
* **LINQ queries** - `AjisQuery` provider with Where, OrderBy, Skip, Take, Select, aggregations
* **Aggregations** - Count, Sum, Average, Min, Max, Any, All, Distinct operations
* **Streaming I/O** - Memory-bounded file operations via async enumerables
* **Memory-mapped support** - Large file processing via `MemoryMappedFile`
* **Async by default** - All file operations support async patterns

## Installation

```bash
dotnet add package Afrowave.AJIS.IO
```

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisFile` | Static class with file CRUD operations |
| `AjisFileIndex<T>` | Index for fast key-based lookups |
| `AjisQuery` | LINQ query provider for AJIS files |
| `AjisAggregations` | Aggregation utilities |
| `AjisFileReader` | Streaming file reader |
| `AjisFileWriter` | Streaming file writer |

## Dependencies

* **Afrowave.AJIS.Core** - Core contracts and settings
* **Afrowave.AJIS.Streaming** - Segment parser
* **Afrowave.AJIS.Serialization** - Serializer and value types

## Usage

### Basic file operations

```csharp
using Afrowave.AJIS.IO;

var users = new[] {
    new User { Id = 1, Name = "Alice" },
    new User { Id = 2, Name = "Bob" }
};

// Create file
AjisFile.Create("users.ajis", users);

// Read all items
var allUsers = AjisFile.ReadAll<User>("users.ajis");

// Stream items
await foreach (var user in AjisFile.EnumerateAsync<User>("users.ajis"))
{
    ProcessUser(user);
}
```

### Indexed lookups

```csharp
// Build index
var index = AjisFile.CreateIndex<User>("users.ajis", u => u.Id);

// Fast lookup
var user = index.FindByKey(1);
```

### LINQ queries

```csharp
var activeUsers = AjisQuery.FromFile<User>("users.ajis")
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Skip(10)
    .Take(20)
    .ToList();
```

### Aggregations

```csharp
var count = AjisQuery.FromFile<User>("users.ajis").Count();
var avgPoints = AjisQuery.FromFile<User>("users.ajis").Average(u => u.Points);
var maxAge = AjisQuery.FromFile<User>("users.ajis").Max(u => u.Age);
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide
* [Architecture](../../Docs/Architecture/en.md) - Repository architecture
* [File Operations](../../Docs/15_Tools_CLI_and_FileOps.md) - File I/O details
* [Querying](../../Docs/api/README.md#linq-support) - LINQ documentation

## Compatibility

* .NET 10.0+
* Nullable reference types enabled

## License

MIT
