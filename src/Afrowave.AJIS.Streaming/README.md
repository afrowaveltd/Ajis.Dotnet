# Afrowave.AJIS.Streaming

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.Streaming.svg)](https://www.nuget.org/packages/Afrowave.AJIS.Streaming)

Low-memory streaming parser for the AJIS (.NET) framework. Produces AJIS segments with memory-bounded parsing and single-pass streaming for processing large datasets efficiently.

## Overview

The Streaming library provides the low-level streaming infrastructure for the AJIS ecosystem:

* **UTF-8 streaming parser** - Single-pass parsing with zero-copy token delivery
* **Segment production** - `AjisSegment` model for streaming token stream
* **Memory-bounded parsing** - Bounded memory usage regardless of input size
* **Zero-allocation** - Optimized for high-throughput, low-latency scenarios
* **Async support** - Non-blocking async streaming with IAsyncEnumerable

## Features

* **Streaming reader primitives** - `IAjisReader`, `AjisSpanReader`, `AjisStreamReader`
* **UTF-8 text scanning** - Strings, numbers, comments (lexer)
* **Segment parser** - `ParseSegments` implementation mapping streams to `AjisSegment`
* **AjisSegment model** - Token stream representation with value and kind
* **Single-pass algorithms** - No random access, streaming forward only
* **Bounded memory** - No full document materialization
* **Memory-mapped support** - Files larger than available memory

## Installation

```bash
dotnet add package Afrowave.AJIS.Streaming
```

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisReader` | Low-level streaming reader |
| `AjisLexer` | Lexical analysis engine |
| `AjisToken` | Token representation |
| `AjisSliceUtf8` | UTF-8 slice for zero-allocation token delivery |
| `AjisSegment` | Streaming segment |
| `AjisSegmentMap` | Map transformation pipeline |
| `AjisSegmentFilter` | Filter transformation pipeline |
| `AjisSegmentSelect` | Select transformation pipeline |

## Dependencies

* **Afrowave.AJIS.Core** - Core contracts and settings

## Usage

### Basic streaming parsing

```csharp
using System.Text;
using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming;

string ajisText = """
{
    name: "John Doe"
    age: 35
    email: "john@example.com"
}
""";

var bytes = Encoding.UTF8.GetBytes(ajisText);
using var ms = new MemoryStream(bytes);
using var reader = new StreamReader(ms);

var parser = new AjisLexerParserStreamingAsync(reader);
await foreach (var segment in parser.ParseAsync())
{
    Console.WriteLine($"{segment.Kind}: {segment.Value}");
}
```

### Segment transformations

```csharp
// Map transformation
var mapped = segments.Map(segment => new AjisSegment(segment.Kind, Transform(segment.Value)));

// Filter transformation
var filtered = segments.Where(segment => segment.Kind == AjisSegmentKind.Property);

// Select transformation
var selected = segments.Select(segment => segment.Value);
```

### Large file processing

```csharp
// Memory-efficient processing of large files
await foreach (var segment in ParseSegmentsAsync(largeFileStream))
{
    ProcessSegment(segment);
}
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [Segment Contract](Docs/SegmentContract.md) - Segment model details
* [Streaming Model](Docs/StreamingModel.md) - Streaming architecture
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide
* [API Reader Contract](../../Docs/api/reader.md) - Reader interface
* [Streaming API](../../Docs/api/README.md#streaming-api) - Streaming docs

## Compatibility

* .NET 10.0+
* Nullable reference types enabled
* Requires `AllowUnsafeBlocks` for optimized memory operations

## License

MIT
