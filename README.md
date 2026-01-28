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

## Repository structure (planned)

```
Ajis.Dotnet/
│
├─ src/
│  ├─ Afrowave.AJIS.Core           # Core parser, serializer, diagnostics
│  ├─ Afrowave.AJIS.Events         # Event & progress streaming infrastructure
│  ├─ Afrowave.AJIS.IO             # File & stream helpers
│  ├─ Afrowave.AJIS.Tools          # CLI and tooling utilities
│  └─ Afrowave.AJIS.Integrations   # Optional integrations (HTTP, EF, etc.)
│
├─ tests/
│  ├─ Afrowave.AJIS.Core.Tests
│  └─ test_data/                   # Shared, normalized test inputs (small → huge)
│
├─ benchmarks/
│  └─ Afrowave.AJIS.Benchmarks
│
├─ Docs/
│  ├─ 01_Public_API_Guidelines.md
│  ├─ 02_Diagnostics.md
│  ├─ 03_Format_Norms.md
│  ├─ 05_Events.md
│  └─ 06_Parsers_and_Modes.md
│
└─ README.md
```

Each project may also contain its own local `Docs/` folder with more focused documentation.

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

This repository is currently in the **architecture and documentation phase**.

* APIs are being designed.
* Norms are being written.
* No production guarantees yet.

This is intentional: correctness and clarity come before code volume.

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

If you are reading this early: welcome.
This project is being built deliberately, carefully, and in public.
