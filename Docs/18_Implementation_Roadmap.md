# AJIS.Dotnet – Implementation Roadmap

> **Status:** FIXED / CANONICAL
>
> This document defines the authoritative implementation roadmap for **AJIS.Dotnet**.
> It supersedes any legacy or archived documentation (including historical AJIS_ATP binary headers).
> All functionality MUST follow the specifications present in this repository.

---

## 0. Global Principles (Non‑Negotiable)

These principles apply to **all** phases and MUST NOT be violated by any implementation choice.

1. **Streaming‑first**
   A valid execution path MUST exist that can process arbitrarily large files with bounded memory usage.

2. **Engine / Strategy Architecture**
   A single public API is exposed, while multiple internal engines MAY exist and be selected dynamically.

3. **Strict JSON Compatibility**
   AJIS parsers and serializers MUST support a strict JSON mode that is a drop‑in replacement for existing JSON tooling.

4. **LAX Parsing Is Explicit and Isolated**
   JavaScript‑tolerant parsing (LAX) is a separate mode and MUST NOT affect strict semantics.

5. **Test Data Is a Contract**
   All content under `test_data/` is considered normative and shared across language implementations.

6. **Performance Is a Project Goal**
   Performance parity with `System.Text.Json` is a required long‑term objective, not an optional optimization.

---

## 1. Compatibility Modes

### 1.1 Strict JSON Mode

* Fully RFC‑compliant JSON
* No comments, no trailing commas, no alternative numeric bases
* Serializer output MUST be valid JSON
* Intended as a **drop‑in replacement** for existing JSON libraries

### 1.2 AJIS Canonical / Pretty Modes

* Canonical mode produces deterministic output suitable for hashing, diffing and caching
* Pretty mode prioritizes human readability
* Both may use AJIS extensions defined by the current specification

### 1.3 LAX Mode (JavaScript‑Tolerant)

* Accepts a defined subset of JavaScript object literal syntax
* Examples (subject to specification):

  * Unquoted identifiers as keys
  * Trailing commas
  * Single‑quoted strings
  * JavaScript‑style comments
* LAX MUST always be explicitly enabled via options
* Diagnostics SHOULD report tolerated constructs

---

## 2. Engine Selection Model

### 2.1 Engine Preferences

The runtime MAY select different internal engines based on configuration and data characteristics.

```text
Auto | LowMemory | Balanced | HighThroughput
```

* **Auto**: runtime decides based on input size, mode and options
* **LowMemory**: prioritizes minimal allocations and streaming safety
* **Balanced**: general‑purpose default
* **HighThroughput**: optimized for speed and server workloads

### 2.2 Engine Contract

Each engine MUST:

* Declare supported modes and features
* Be selectable without changing public APIs
* Preserve identical semantic results

---

## 3. Milestones

### M1 – StreamWalk Reference (COMPLETED)

Purpose: establish deterministic behavior and diagnostics for text processing.

**Fixed & Unit‑Tested:**

* `.case` test file contract (canonical + legacy tolerance)
* Diagnostic code mapping
* Deterministic trace slice rendering
* LAX explicitly rejected as unsupported

---

### M1.1 – Engine Selection Skeleton

Purpose: introduce engine abstraction without changing behavior.

Deliverables:

* Engine interface and selector
* Engine preference enum
* M1 wrapped as a selectable engine

**Fixed & Unit‑Tested:**

* Deterministic engine selection
* Runner → selector → engine wiring

---

### M2 – Text Primitives (Lexer / Reader)

Purpose: unified UTF‑8 reader for all parsers.

Scope:

* Whitespace and comments
* Strings and escape sequences
* Numeric literals (bases, separators)
* Offset / line / column tracking

**Fixed & Unit‑Tested:**

* Escape correctness
* Unicode edge cases
* Numeric validation
* Position tracking accuracy

---

### M3 – Low‑Memory Streaming Parser

Purpose: guaranteed safe processing of very large documents.

Scope:

* Stream‑based segmentation
* Minimal allocations
* Configurable limits and protections

**Fixed & Unit‑Tested:**

* Segment structure
* Error conditions
* Limit enforcement

**Suite Tests:**

* Large generated datasets
* Memory usage validation

---

### M4 – Serializer

Modes:

1. StrictJson
2. AjisCanonical
3. AjisPretty

**Fixed & Unit‑Tested:**

* Canonical determinism
* Strict JSON validity

---

### M5 – LAX Parser

Purpose: JavaScript‑tolerant reader.

Scope (per specification):

* Unquoted keys
* Trailing commas
* Single‑quoted strings
* JavaScript comments

Diagnostics MUST clearly indicate tolerated constructs.

---

### M6 – High‑Throughput Engines

Purpose: performance parity with `System.Text.Json`.

Techniques MAY include:

* Span‑based fast paths
* SIMD acceleration
* Buffer pooling

Validation via benchmarks, not unit tests.

---

### M7 – Mapping Layer

Purpose: usability comparable to Newtonsoft.Json.

Scope:

* Flexible naming policies
* Custom converters
* Path‑aware error reporting

---

### M8 – Ecosystem Integration

Scope:

* AJIS file reader/writer
* Search and sorting engine
* Entity Framework integration
* HTTP content type: `text/ajis`
* JavaScript tooling

---

## 4. Test Data Strategy

### 4.1 Data Sets

* `test_data/streamwalk` – deterministic parsing contracts
* `test_data_legacy/common` – shared semantic datasets (imported from legacy AJIS_ATP)

### 4.2 Manifest

Each dataset MUST be documented in `test_data/MANIFEST.md` with:

* Purpose
* Applicable milestones
* Stability level

---

## 5. Explicit Exclusions

* Legacy binary headers (e.g. `\0AJS`) are **ignored entirely**
* No backward compatibility with deprecated binary formats
* ATP and binary payloads are deferred until text processing is fully finalized

---

## 6. Roadmap Governance

* Any change to this document requires explicit agreement
* Conflicts with legacy documentation MUST be resolved in favor of this repository
* Ambiguities MUST be clarified before implementation

---

**This roadmap is authoritative.**
