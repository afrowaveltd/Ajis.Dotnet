# AJIS Parsers and Modes

> Normative specification for parsing strategies, modes, and adaptive behavior

---

## 1. Scope of this document

This document defines the **AJIS parsing model**, including:

* supported parser modes,
* their intended use cases,
* memory and performance characteristics,
* adaptive mode selection.

The goal is to allow AJIS to scale from small configuration files
to multi-million-record data sets.

---

## 2. Core parsing principles

All AJIS parsers must adhere to the following principles:

* **Single-pass capable** – input may be processed without rewind
* **Streaming-first** – values may be emitted before full document completion
* **Deterministic behavior** – identical input yields identical output
* **Precise diagnostics** – errors report exact locations

Materialization of the full document is optional and mode-dependent.

Directive syntax:

* `#<namespace> <command> [key=value]...`

---

## 3. Parser modes overview

AJIS defines multiple **parser modes**, each optimized for a different scenario.

An implementation may support a subset of modes, but supported modes
must follow the semantics defined here.

Defined modes:

* **SmallFiles**
* **BigFiles**
* **ExtraFast**
* **Auto**

Processing profile hint:

* **Universal** (default)
* **LowMemory**
* **HighThroughput**

---

## 4. SmallFiles mode

### 4.1 Intended use

The SmallFiles mode is optimized for:

* configuration files,
* moderate-size data (typically up to tens of thousands of nodes),
* scenarios where simplicity and developer convenience matter more than raw throughput.

---

### 4.2 Characteristics

* full or partial in-memory materialization,
* rich object model availability,
* simpler internal control flow,
* slightly higher memory usage.

---

### 4.3 Guarantees

* predictable performance,
* easiest integration with high-level APIs,
* full diagnostic support.

---

## 5. BigFiles mode

### 5.1 Intended use

The BigFiles mode is optimized for:

* very large AJIS/JSON-like files,
* hundreds of thousands to millions of records,
* environments with limited memory headroom.

---

### 5.2 Characteristics

* strict single-pass streaming,
* minimal buffering,
* values emitted as soon as they are complete,
* no implicit full-document allocation.

---

### 5.3 Guarantees

* bounded memory usage independent of file size,
* deterministic emission order,
* compatibility with progress and event streaming.

---

## 6. ExtraFast mode

### 6.1 Intended use

The ExtraFast mode targets:

* performance-critical workloads,
* trusted or pre-validated inputs,
* batch processing where diagnostics may be secondary.

---

### 6.2 Characteristics

* reduced validation checks,
* minimal branching on hot paths,
* aggressive inlining and specialization,
* optional diagnostic suppression.

---

### 6.3 Trade-offs

* less descriptive error reporting,
* stricter assumptions about input correctness,
* not recommended for untrusted input.

---

## 7. Auto mode

### 7.1 Purpose

Auto mode allows the AJIS runtime to **select an appropriate parser mode automatically**.

---

### 7.2 Selection strategy

Auto mode may consider:

* input size estimation,
* structural complexity,
* available memory,
* caller-provided hints.

The selection process must be:

* fast,
* conservative (prefer safety over marginal gains),
* observable via diagnostic or debug events.

### 7.3 Processing profile mapping

Implementations may use the processing profile hint to select parser/serializer strategies.

* **Universal** → balanced selection (default)
* **LowMemory** → prefer streaming/minimal allocation engines
* **HighThroughput** → prefer speed-optimized engines

---

## 8. Emission model

In streaming modes, parsed elements may be emitted as:

* complete values,
* key-value pairs,
* structured events.

Emission rules:

* emitted values must be complete and immutable,
* parent-child relationships must be preserved via context identifiers,
* emission order must follow input order.

Reference implementation notes:

* `AjisParse.ParseSegmentsWithDirectives` returns segments plus settings updated by document directives.

---

## 9. Partial parsing and skipping

AJIS parsers may support:

* skipping of subtrees,
* early termination after a match,
* selective extraction of values.

These capabilities are optional but strongly encouraged
for BigFiles and Auto modes.

---

## 10. Error handling by mode

Different modes may handle errors differently:

* SmallFiles: detailed diagnostics and recovery where possible
* BigFiles: fail-fast with precise location
* ExtraFast: minimal diagnostics, immediate failure

Fatal errors must still respect the diagnostics contract.

---

## 11. Relationship to implementations

This document defines **behavioral contracts** for parser modes.

Implementation documents define:

* concrete algorithms,
* data structures,
* optimizations.

No implementation may weaken the guarantees described here.

---

## 12. Status of this specification

This document is considered **stable**,
with possible future extensions for additional modes
or adaptive heuristics.
