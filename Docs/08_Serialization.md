# AJIS Serialization

> Normative specification for AJIS serialization and streaming output

---

## 1. Scope of this document

This document defines the **AJIS serialization model**, including:

* how AJIS values are written to output,
* streaming and incremental serialization,
* interaction with events, diagnostics, and cancellation,
* guarantees about output correctness and ordering.

This document is **normative** for all AJIS serializers.

---

## 2. Design goals

AJIS serialization follows these goals:

* **Symmetry** – serialization mirrors parsing semantics
* **Streaming-first** – output may be produced incrementally
* **Low memory** – no implicit buffering of full documents
* **Deterministic output** – identical input yields identical output
* **Observability** – progress and diagnostics are emitted consistently

---

## 3. Serialization model

AJIS serialization is defined as a **one-directional streaming process**.

Core principles:

* serializers do not require full materialization of values,
* output order follows input order strictly,
* serializers must respect selected parser/serializer mode.

---

## 4. Serializer input forms

AJIS serializers may accept input in multiple forms:

* immutable AJIS values,
* streaming value sequences,
* structured write commands (begin/end object, property, value).

Implementations may support one or more forms.

---

## 5. Streaming serialization

### 5.1 Incremental output

Streaming serializers:

* write output as soon as sufficient information is available,
* must not delay output unnecessarily,
* must support very large documents.

---

### 5.2 Output targets

Serialization targets may include:

* byte streams,
* text writers,
* network streams,
* file streams.

Serializers must not assume seekable output.

---

## 6. Formatting and canonical form

### 6.1 Formatting

Formatting options:

* compact (minified),
* pretty-printed (indented),
* canonical (normalized spacing and ordering).

Formatting affects presentation only, not semantics.

---

### 6.2 Canonical serialization

Canonical serialization:

* produces a stable textual representation,
* is suitable for hashing and signing,
* must follow strictly defined formatting rules.

Canonical rules are defined in a separate specification section.

---

## 7. Events and progress reporting

AJIS serializers integrate with the event system.

Rules:

* progress events must reflect forward progress of output generation,
* diagnostic events must be emitted immediately,
* event emission must never block serialization.

---

## 8. Cancellation behavior

Serialization must support cooperative cancellation.

Rules:

* cancellation must stop further output promptly,
* partially written output must remain syntactically valid up to the last write,
* serializers must release resources immediately on cancellation.

---

## 9. Error handling

Serialization errors may include:

* invalid value structures,
* unsupported features for selected mode,
* I/O failures.

Rules:

* fatal errors must result in exceptions,
* diagnostics may be emitted before failure,
* no further output may be written after a fatal error.

---

## 10. Performance considerations

AJIS serializers must:

* minimize allocations on hot paths,
* avoid unnecessary buffering,
* support backpressure via output targets.

When events and diagnostics are disabled,
serialization overhead must be minimal.

---

## 11. Relationship to implementations

This document defines **what serialization guarantees**.

Implementation documents define:

* concrete writer APIs,
* buffering strategies,
* output encoding.

Implementations must not weaken the guarantees defined here.

---

## 12. Status of this specification

This document is considered **stable** for textual AJIS.

Binary and ATP serialization is defined in a separate document.

---

# AJIS → JSON Export Profiles

AJIS supports multiple JSON export profiles to balance interoperability, tooling support, and round‑trip fidelity.

## Export Profiles

### Minimal

* Produces clean, standard JSON
* No AJIS directives
* No comments
* No round‑trip guarantees

Use when JSON is consumed by external systems or APIs.

---

### Functional (default)

* Preserves AJIS directives required for tooling and transformations
* Omits comments
* Enables partial round‑trip reconstruction

This is the **default export mode**.

---

### DeterministicRoundtrip

* Preserves AJIS directives
* Preserves comments
* Enables full reconstruction of the AJIS text representation

Use when complete round‑trip fidelity is required.

---

## Default Behavior

> Functional mode is the default AJIS → JSON export profile.

---

# Directives and History

## History Directives

History directives are emitted **only** when exporting AJIS to JSON and only when information would otherwise be lost.

### Example

* Non‑decimal numbers are converted to decimal for JSON
* A history directive records the original base

```json
{
  "category": "history",
  "name": "number_base",
  "scope": "next",
  "value": 16
}
```

History directives MUST be concise and MUST NOT duplicate information already implicit in JSON.

---

# Comments and Export

Comments are a first‑class feature of AJIS.

## Comment Export Rules

* **Minimal**: comments are discarded
* **Functional**: comments are discarded
* **DeterministicRoundtrip**: comments are preserved

In DeterministicRoundtrip mode, comments are exported as JS‑escaped strings within `ajis_directives_block.comments`.

Comment export is enabled **only** in DeterministicRoundtrip mode.

---

# JSON Interoperability

## ajis_directives_block

The `ajis_directives_block` object may be included in JSON output depending on export mode.

* Present in **Functional** and **DeterministicRoundtrip** modes
* May include:

  * `directives`
  * `comments` (DeterministicRoundtrip only)

This block enables tooling, diagnostics, and optional round‑trip reconstruction while keeping JSON payloads interoperable.

---

# API Notes

Serialization APIs default to Functional export unless explicitly specified otherwise.

> Converts AJIS to JSON using the specified export mode. Functional mode is used by default.
