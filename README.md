# AJIS for .NET

**Afrowave JSON-like Interchange Specification**

> A high-performance, stream-first, normatively defined data interchange format
> designed for very large files, low-memory environments, and long-term evolution.

---

## What is AJIS

**AJIS** (Afrowave JSON-like Interchange Specification) is a structured data format inspired by JSON,
but designed from the beginning for scenarios where JSON starts to reach its limits:

* very large files (hundreds of MB to GB),
* streaming and incremental processing,
* precise diagnostics and error locations,
* extensibility beyond text-only payloads,
* predictable performance and memory usage.

AJIS is **not a universal replacement for JSON**.
It is a specialist format for cases where correctness, performance, and control matter more than minimal syntax.

---

## Why another format?

JSON is excellent and ubiquitous, but it has structural limits:

* many parsers require loading large portions of the document into memory,
* limited control over parsing strategy,
* weak diagnostics for malformed or partial data,
* difficult incremental updates without full reparse,
* no native concept of hybrid or binary extensions.

AJIS was designed explicitly to address these problems.

---

## Core design principles

AJIS follows several strict principles:

* **Streaming first**
  Every parser and serializer must be able to operate in a single pass.

* **Low memory by design**
  No implicit full-document materialization.

* **Normative specification**
  The format is precisely defined; implementations follow the specification, not vice versa.

* **Multiple parsing modes**
  Different strategies for small files, very large files, and ultra-fast scenarios.

* **Precise diagnostics**
  Errors report exact position, context, and reason.

* **Tooling-friendly**
  Designed to support searching, filtering, partial updates, and CRUD-like operations directly on disk.

---

## AJIS is more than JSON

Although AJIS is syntactically close to JSON in its basic text form, it intentionally goes far beyond JSON capabilities.

AJIS is designed as a **layered format**:

* a familiar **textual layer** (JSON-like, human-readable), and
* optional **extended layers** for advanced and large-scale scenarios.

Key extensions defined by the AJIS specification include:

* **Multi-line text values**
  Native support for readable multi-line strings without excessive escaping.

* **Comments**
  Inline and block comments for configuration files, diagnostics, and documentation purposes.

* **Extended numeric formats**
  Support for numeric literals in bases other than decimal (e.g. binary, hexadecimal), when enabled by mode.

* **Binary data integration**
  AJIS supports referencing or embedding binary payloads in a structured and streamable way.

* **Signatures and integrity metadata**
  Optional cryptographic signatures and checksums for validation and trust.

* **Hybrid text–binary containers**
  Planned support for AJIS + binary data combined into a single container format
  (e.g. **ATP – Afrowave Transport Package**).

In this repository, development starts with the **textual AJIS layer**, which is the most JSON-like and the foundation for all other features.

Binary and transport layers are part of the roadmap and will be implemented once the text core is stable and well-defined.

---

## AJIS in .NET

This repository contains the **clean, modern .NET implementation of AJIS**, built step by step with:

* .NET 10 as the primary target platform,
* optional legacy compatibility layers where reasonable,
* strong focus on performance benchmarking,
* documentation-first development approach.

The goal is not just a parser, but a **complete AJIS ecosystem for .NET**.

---

## Repository structure

```
Ajis.Dotnet/
│
├─ src/
│  ├─ Afrowave.AJIS.Core/        # Core parser, serializer, diagnostics
│  ├─ Afrowave.AJIS.Streaming/   # UTF-8 streaming parser & segments
│  ├─ Afrowave.AJIS.Serialization/# Segment serializer & high-level API
│  ├─ Afrowave.AJIS.IO/          # File operations & streaming I/O
│  └─ Afrowave.AJIS.Net/         # ASP.NET Core integration
│
├─ Afrowave.AJIS.EntityFramework/# EF Core value converters
├─ Afrowave.AJIS.MongoDB/        # MongoDB BSON serializers
│
├─ tests/
│  ├─ Afrowave.AJIS.Core.Tests/
│  ├─ Afrowave.AJIS.IO.Tests/
│  ├─ Afrowave.AJIS.Serialization.Tests/
│  ├─ Afrowave.AJIS.Net.Tests/
│  └─ Afrowave.AJIS.Testing/
│
├─ benchmarks/
│  └─ Afrowave.AJIS.Benchmarks/
│
├─ Docs/
│  ├─ API/                       # API reference
│  ├─ Architecture/              # Architecture docs
│  ├─ GettingStarted/            # Quick start guides
│  ├─ Configuration/             # Configuration options
│  ├─ Performance/               # Performance guides
│  ├─ ReleaseNotes/              # Release summaries
│  └─ Roadmap/                   # Implementation roadmap
│
└─ README.md
```

Each project may also contain its own local `Docs/` folder with more focused documentation.

See `Docs/` for comprehensive documentation.

---

## Parsing and serialization modes

AJIS supports multiple internal strategies, for example:

* **Small files** – minimal overhead, fast materialization
* **Big files** – streaming, block-based, low memory
* **Extra-fast** – aggressive assumptions for trusted input
* **Auto mode** – fast probe, then automatic strategy selection

The library may choose the most appropriate mode automatically based on file characteristics.

---

## Events, progress, and observability

AJIS parsers and serializers emit a unified **event stream**, allowing clients to:

* track progress (e.g. percentage or bytes processed),
* receive diagnostics and warnings,
* visualize long-running operations,
* integrate logging or UI without blocking parsing.

Listening is optional; when unused, overhead is minimal.

---

## Status

**Production Ready** since v1.0

All core packages (Core, Streaming, Serialization, IO, Net, EntityFramework, MongoDB) are stable and ready for production use.

### Features

- ✅ Full AJIS/JSON parsing with 3 text modes (JSON/AJIS/Lex)
- ✅ Type-safe M7 mapping
- ✅ Built-in file I/O with streaming support
- ✅ HTTP integration patterns for ASP.NET Core
- ✅ EF Core value converters
- ✅ MongoDB BSON serializers
- ✅ Stress-tested (100K-1M records)
- ✅ Fair performance comparison with System.Text.Json and Newtonsoft.Json
- ✅ 60+ comprehensive tests

For more details, see [`Docs/ReleaseNotes/en.md`](Docs/ReleaseNotes/en.md).

---

## Performance

### Baseline Results

```
Small Object (1KB):
  ✅ AJIS:              51.41 µs
  ⚠️  System.Text.Json:  5.08 µs  (10x faster)
  ❌ Newtonsoft.Json:   17.69 µs

Average Across All Tests:
  AJIS:              163.18 µs
  System.Text.Json:   91.92 µs  (1.78x faster than AJIS)
  Newtonsoft.Json:   455.12 µs  (2.79x slower than AJIS)
```

 AJIS provides more features than both System.Text.Json and Newtonsoft.Json, while maintaining competitive performance.

See [`Docs/ReleaseNotes/en.md`](Docs/ReleaseNotes/en.md) for complete performance data and [`Docs/Performance/en.md`](Docs/Performance/en.md) for performance tips.

---

## Quick start

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

### ASP.NET Core

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddAjisFormatters();
```

See [`Docs/GettingStarted/en.md`](Docs/GettingStarted/en.md) for more examples.

---

## Documentation

Comprehensive documentation is available in the `Docs/` folder:

- **[API Reference](Docs/API/en.md)** - Complete API documentation for all libraries
- **[Getting Started](Docs/GettingStarted/en.md)** - Quick start guides and examples
- **[Architecture](Docs/Architecture/en.md)** - Repository overview and architecture
- **[Configuration](Docs/Configuration/en.md)** - Configuration options and settings
- **[Performance](Docs/Performance/en.md)** - Performance best practices
- **[Release Notes](Docs/ReleaseNotes/en.md)** - Version history and changelog
- **[Roadmap](Docs/Roadmap/en.md)** - Implementation plans and future features

---

## Libraries

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

## Relationship to previous AJIS work

This implementation builds on earlier AJIS normative documents and experiments
(the AJIS_ATP project), but starts with a **clean architecture and stricter separation of concerns**.

---

## License

This project is licensed under the **Afrowave Humanitarian License**.

The license emphasizes ethical use, educational access, and humanitarian principles.

See the `LICENSE` file for full terms.

---

## Philosophy

AJIS is part of the **Afrowave ecosystem**:

* open and transparent,
* designed for long-term sustainability,
* focused on education, tooling, and serious infrastructure,
* avoiding hidden behavior and implicit magic.

---

## Next steps

* Finalize core documentation in `Docs/`
* Implement minimal streaming parser
* Establish benchmark baseline
* Expand gradually, one well-defined component at a time

---

## Contributing

This project is part of the Afrowave ecosystem.
Contributions are welcome! Please see the main `README.md` and `Docs/` folder for details.

---

## Support

For issues, questions, or contributions:

* GitHub Issues: [repository](https://github.com/ajis/dotnet/issues)
* Full documentation: `Docs/` folder
