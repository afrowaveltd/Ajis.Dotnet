# AJIS .NET – Repository Overview & Architecture

> High-level map of the AJIS .NET repository, packages, and stability boundaries

---

## 1. Purpose of this document

This document answers three fundamental questions:

1. **What lives where?**
2. **Which parts are stable vs experimental?**
3. **How do the pieces fit together conceptually?**

It is intentionally written **before implementation**, to prevent architectural drift.

---

## 2. Repository top-level layout

```
Ajis.Dotnet/
│
├─ src/
│  ├─ Afrowave.AJIS.Core/        # Core contracts, settings, diagnostics
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
│  └─ Afrowave.AJIS.Testing/     # Shared test utilities
│
├─ benchmarks/
│  └─ Afrowave.AJIS.Benchmarks/
│
├─ Docs/
│  ├─ API/                       # API reference (en.md, [lang].md)
│  ├─ Architecture/              # Architecture guides (en.md)
│  ├─ Configuration/             # Configuration options (en.md)
│  ├─ GettingStarted/            # Quick start guides (en.md)
│  ├─ Performance/               # Performance guides (en.md)
│  ├─ ReleaseNotes/              # Release summaries (en.md)
│  ├─ Roadmap/                   # Implementation roadmap (en.md)
│  └─ ...
│
└─ README.md
```

Each project may also contain its own local `Docs/` folder with more focused documentation.

---

## 3. Package responsibilities

### 3.1 Afrowave.AJIS.Core (stable)

**Responsibilities**:

* `AjisSettings` - Configuration and options
* Exceptions & diagnostics - `AjisDiagnostic*`, error reporting
* Localization abstraction - `AjisLoc*`, resource loading
* Logging abstraction - `IAjisLogger`, extended logging
* Event / progress infrastructure - `IAjisEventSource`, progress tracking

**Rules**:

* No IO operations
* No reflection-heavy logic
* No streaming assumptions
* No parsing or serialization logic

This package must remain **small, stable, and dependency-light**.

---

### 3.2 Afrowave.AJIS.Streaming (stable core)

**Responsibilities**:

* UTF-8 reader primitives - `IAjisReader`, `AjisSpanReader`, `AjisStreamReader`
* Text scanning - strings, numbers, comments (lexer)
* `ParseSegments` implementation - maps streams to `AjisSegment`
* `AjisSegment` model - token stream representation
* Streaming algorithms - memory-bounded, single-pass

**Rules**:

* Single-pass only (no random access)
* Bounded memory (no full materialization)
* No object creation (segments only)
* Zero-copy where possible

This is the **performance heart** of AJIS.

---

### 3.3 Afrowave.AJIS.Serialization (stable)

**Responsibilities**:

* Segment serializer - `AjisSerialize`, `AjisSerializer`
* High-level API - `AjisSerializer<T>`, async/stream methods
* Multiple serialization modes - Strict JSON, Canonical, Pretty
* Value serialization - AjisValue (text/stream/bytes)

**Rules**:

* Must operate purely on segments
* Must not require random access
* Must support streaming end-to-end

---

### 3.4 Afrowave.AJIS.IO (stable, user-facing)

**Responsibilities**:

* File operations - `AjisFile`, `AjisFileReader`, `AjisFileWriter`
* Search & replace - partial read/write, search patterns
* Streaming I/O - async file operations, memory-mapped files
* Query support - Linq over AjisFile, indexed lookups

**Rules**:

* Never corrupt input files
* Always support streaming for large files
* Progress & diagnostics enabled by default

---

### 3.5 Afrowave.AJIS.Net (stable, user-facing)

**Responsibilities**:

* HTTP formatters - `AjisInputFormatter`, `AjisOutputFormatter`
* ASP.NET Core integration - middleware, extensions
* JSON compatibility - seamless replacement for System.Text.Json

**Rules**:

* Must follow ASP.NET Core patterns
* Must be opt-in (no breaking changes)
* Must maintain compatibility with existing code

---

### 3.6 Afrowave.AJIS.EntityFramework (stable)

**Responsibilities**:

* Value converters - `AjisValueConverter<T>`
* EF Core integration - `UseAjisFormat()` configuration
* Query translation - support in LINQ queries
* Complex type mapping - nested objects, collections

**Rules**:

* Must be opt-in (no automatic conversion)
* Must not affect existing EF behavior
* Must support all EF Core databases

---

### 3.7 Afrowave.AJIS.MongoDB (stable)

**Responsibilities**:

* BSON serializers - `AjisBsonSerializer<T>`
* MongoDB integration - seamless document storage
* Collection operations - insert, query, bulk operations
* Aggregation pipeline support

**Rules**:

* Must use MongoDB.Driver contracts
* Must support async streaming
* Must not require full document materialization

---

## 4. Stability levels

| Level            | Meaning                                      |
| ---------------- | -------------------------------------------- |
| **Stable**       | Public API frozen except additive changes    |
| **Preview**      | API usable but may change                    |
| **Experimental** | Internal or opt-in, no compatibility promise |

Initial targets:

* Core / Streaming / Serialization / IO / Net → **Stable**
* EntityFramework / MongoDB → **Stable**
* Future: ATP (binary format) → **Experimental initially**

---

## 5. Architectural flow

Conceptual data flow:

```
Input (Stream / Bytes)
   ↓
Text Scanner (Lexer)
   ↓
Segment Parser (AjisSegment stream)
   ↓
[Optional Transforms / Tools]
   ↓
Segment Serializer
   ↓
Output (Stream / File)
```

Object mapping, tools, and ATP all **sit on top** of this backbone.

---

## 6. Design invariants (must never break)

* AJIS text parsing is always streaming-first
* No feature may require full document materialization
* Tools must work on multi-GB files
* Diagnostics must be precise and localizable
* Performance optimizations must not leak into API semantics

---

## 7. Relationship to other documents

This document provides context for:

* `Docs/API/` - API reference documentation
* `Docs/Architecture/` - Architecture details
* `Docs/Roadmap/` - Implementation roadmap
* `Docs/GettingStarted/` - Quick start guides

It should be read **before** starting implementation work.

---

## 8. Current status

**Production ready** since v1.0.

All core packages (Core, Streaming, Serialization, IO, Net, EntityFramework, MongoDB) are stable and ready for production use.

---

## 9. Next steps

1. Finalize documentation in `Docs/`
2. Implement any missing APIs
3. Expand testing coverage
4. Performance optimization (M6 SIMD)
5. Binary format support (M11)

---

## 10. Documentation structure

All documentation follows a consistent pattern:

```
Docs/
├── <Topic>/
│   ├── en.md          # English (primary)
│   ├── [lang].md      # Translations (auto-generated)
```

Where `<Topic>` is:
- `API` - API reference
- `Architecture` - Architecture docs
- `Configuration` - Configuration options
- `GettingStarted` - Quick start
- `Performance` - Performance guides
- `ReleaseNotes` - Release summaries
- `Roadmap` - Implementation roadmap
- `Tutorial` - Step-by-step guides

This structure enables automated translation workflows.
