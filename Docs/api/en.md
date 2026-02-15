# AJIS API Reference

> Comprehensive API documentation for AJIS .NET libraries

---

## libraries

| Library | Package | Status | Description |
|---------|---------|--------|-------------|
| **Afrowave.AJIS.Core** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Core) | Stable | Core contracts, settings, diagnostics, localization, logging, events |
| **Afrowave.AJIS.Streaming** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Streaming) | Stable | UTF-8 streaming parser, segment production, memory-bounded |
| **Afrowave.AJIS.Serialization** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Serialization) | Stable | Segment serializer, high-level AjisSerializer API |
| **Afrowave.AJIS.IO** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.IO) | Stable | File operations, search/replace, partial read/write |
| **Afrowave.AJIS.Net** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Net) | Stable | ASP.NET Core formatters (input/output) |
| **Afrowave.AJIS.EntityFramework** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.EntityFramework) | Stable | EF Core value converters |
| **Afrowave.AJIS.MongoDB** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.MongoDB) | Stable | MongoDB BSON serializers |

---

## Documentation structure

All documentation follows a consistent organization:

```
Docs/
├── API/                  # API reference (this directory)
│   ├── Core.md          # Core library API
│   ├── Streaming.md     # Streaming library API
│   ├── Serialization.md # Serialization library API
│   ├── IO.md            # IO library API
│   └── Net.md           # Net library API
├── Architecture/        # Architecture docs
│   └── en.md            # Repository overview & architecture
├── GettingStarted/      # Quick start guides
│   └── en.md            # Getting started with AJIS
├── Configuration/       # Configuration options
│   └── en.md            # Configuration reference
├── Performance/         # Performance guides
│   └── en.md            # Performance best practices
├── ReleaseNotes/        # Release summaries
│   └── en.md            # Release notes and changelog
└── Roadmap/             # Implementation roadmap
    └── en.md            # Roadmap and future plans
```

Each document is written in English (`en.md`) with automated translation support for other languages.

---

##Quick start

### Install packages

```bash
# Core library (required)
dotnet add package Afrowave.AJIS.Core

# Choose additional packages based on needs:
dotnet add package Afrowave.AJIS.Streaming
dotnet add package Afrowave.AJIS.Serialization
dotnet add package Afrowave.AJIS.IO
dotnet add package Afrowave.AJIS.Net
dotnet add package Afrowave.AJIS.EntityFramework
dotnet add package Afrowave.AJIS.MongoDB
```

### Parse AJIS

```csharp
using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming;

var parser = new AjisLexerParserStreamingAsync(reader);
await foreach (var segment in parser.ParseAsync())
{
    // Process segments
}
```

### Serialize to AJIS

```csharp
using Afrowave.AJIS.Serialization;

var serializer = new AjisSerializer();
string ajisText = serializer.Serialize(myObject);
```

### File I/O

```csharp
using Afrowave.AJIS.IO;

AjisFile.Create("data.ajis", myData);
var loaded = AjisFile.ReadAll<MyType>("data.ajis");
```

---

## API documentation details

### Core Library

The Core library provides:

- Settings and options (`AjisSettings`, `AjisOptions`)
- Diagnostics and error reporting (`AjisDiagnostic`, `AjisException`)
- Localization support (`AjisLocDictionary`, `AjisTextProvider`)
- Logging abstraction (`IAjisLogger`)
- Event/progress infrastructure (`IAjisEventSource`)

**API Reference:** `API/Core.md`

### Streaming Library

The Streaming library provides:

- UTF-8 reader primitives (`IAjisReader`, `AjisSpanReader`, `AjisStreamReader`)
- Lexer and tokenization (`AjisLexer`, `AjisToken`)
- Segment parser (`AjisParse.ParseSegments`)
- Segment model (`AjisSegment`)

**API Reference:** `API/Streaming.md`

### Serialization Library

The Serialization library provides:

- Segment serializer (`AjisSerialize`)
- High-level API (`AjisSerializer`, `AjisSerializer<T>`)
- Serialization options (`AjisSerializationOptions`)
- Multiple modes (Strict JSON, Canonical, Pretty)
- Value serialization (`AjisValue`)

**API Reference:** `API/Serialization.md`

### IO Library

The IO library provides:

- File operations (`AjisFile`, `AjisFileReader`, `AjisFileWriter`)
- Search and replace operations
- Partial read/write
- Async streaming I/O
- Memory-mapped file support

**API Reference:** `API/IO.md`

### Net Library

The Net library provides:

- HTTP formatters (`AjisInputFormatter`, `AjisOutputFormatter`)
- ASP.NET Core integration
- Extension methods for service configuration

**API Reference:** `API/Net.md`

### EntityFramework Integration

The EntityFramework integration provides:

- Value converters (`AjisValueConverter<T>`)
- EF Core configuration (`UseAjisFormat()`)
- LINQ query support

**API Reference:** `API/EntityFramework.md`

### MongoDB Integration

The MongoDB integration provides:

- BSON serializers (`AjisBsonSerializer<T>`)
- Collection operations
- Aggregation pipeline support

**API Reference:** `API/MongoDB.md`

---

## Related documentation

- **Architecture:** See `Architecture/en.md` for repository overview and design principles
- **Getting Started:** See `GettingStarted/en.md` for quick start guides
- **Configuration:** See `Configuration/en.md` for configuration options
- **Performance:** See `Performance/en.md` for performance best practices
- **Release Notes:** See `ReleaseNotes/en.md` for version history
- **Roadmap:** See `Roadmap/en.md` for implementation plans

---

## Support

For issues, questions, or contributions:

- GitHub Issues: [repository](https://github.com/ajis/dotnet/issues)
- Documentation: Full documentation in `Docs/` folder
