# Afrowave.AJIS.Serialization

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.Serialization.svg)](https://www.nuget.org/packages/Afrowave.AJIS.Serialization)

Segment-based serializer and high-level API for the AJIS (.NET) framework. Provides compact and human-readable serialization with multiple modes, value serialization, and streaming support.

## Overview

The Serialization library enables converting between AJIS segments and .NET objects with enterprise-grade features:

* **Segment serializer** - High-throughput serialization from segments
* **High-level API** - `AjisSerializer<T>` for object mapping
* **Multiple modes** - Strict JSON, Canonical, Pretty formatting
* **Value serialization** - `AjisValue` for polymorphic serialization
* **Async streaming** - Memory-efficient serialization over streams
* **Zero-allocation** - Optimized for high-throughput scenarios

## Features

* **Segment serialization** - `AjisSerialize` for low-level segment-to-text
* **High-level API** - `AjisSerializer` for object-to-text conversion
* **Multiple modes**:
  * `Compact` - Minimal size, no whitespace
  * `Pretty` - Readable with indentation and newlines
  * `Canonical` - Deterministic output (sorted keys)
* **Value serialization** - Support for `AjisValue` (text/stream/bytes)
* **Async support** - All serialization methods support async patterns
* **Type mapping** - Property attributes, naming policies
* **Error handling** - Detailed diagnostics and exception messages

## Installation

```bash
dotnet add package Afrowave.AJIS.Serialization
```

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisSerialize` | Low-level segment serializer |
| `AjisSerializer` | High-level serializer API |
| `AjisSerializer<T>` | Generic serializer with type mapping |
| `AjisValue` | Polymorphic value representation |
| `AjisSegment` | Streaming segment |
| `AjisSerializationOptions` | Serialization configuration |

## Dependencies

* **Afrowave.AJIS.Core** - Core contracts and settings
* **Afrowave.AJIS.Streaming** - Segment parser and model

## Usage

### Basic serialization

```csharp
using Afrowave.AJIS.Serialization;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

var user = new User { Id = 1, Name = "Alice" };
var serializer = new AjisSerializer();

// Serialize to text
string text = serializer.Serialize(user);
// {"Id":1,"Name":"Alice"}
```

### Serialization modes

```csharp
var options = new AjisSerializationOptions
{
    Compact = false,      // Add spaces after commas/colons
    Pretty = true,        // Add newlines and indentation
    Canonicalize = true   // Order object properties by key
};

string prettyText = serializer.Serialize(user, options);
/*
{
    "Id" : 1,
    "Name" : "Alice"
}
*/
```

### Value serialization

```csharp
var value = new AjisValue(new Dictionary<string, AjisValue>
{
    ["name"] = new AjisValue("Alice"),
    ["age"] = new AjisValue(30)
});

string text = serializer.Serialize(value);
```

### Async streaming

```csharp
await using var stream = File.Create("users.ajis");
await serializer.SerializeAsync(user, stream);
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [Serialization Modes](Docs/SerializationModes.md) - Processing profiles and mode selection
* [Canonicalization](Docs/Canonicalization.md) - Canonical serialization details
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide

## Compatibility

* .NET 10.0+
* Nullable reference types enabled

## License

MIT
