# AJIS Diagnostics and Error Reporting

> Normative specification for diagnostics, errors, localization, and developer-facing reporting

---

## 1. Scope of this document

This document defines the **AJIS diagnostics model**, which governs:

* how errors and warnings are represented,
* how precise source locations are reported,
* how diagnostics interact with exceptions,
* how localization is supported without performance penalties,
* how diagnostics are rendered for humans and tools.

This document is **normative** for all AJIS implementations.

---

## 2. Design principles

AJIS diagnostics follow these principles:

* **Precision first** – every diagnostic must point to an exact location
* **Non-allocating hot path** – diagnostics must be cheap to create
* **Separation of concerns** – diagnostics are data, rendering is external
* **Localization-ready** – messages are keys, not hardcoded strings
* **Tooling-friendly** – structured data over formatted text

---

## 3. Diagnostic vs. exception

AJIS distinguishes between:

* **Diagnostics** – structured descriptions of problems or conditions
* **Exceptions** – control-flow termination mechanisms

Rules:

* diagnostics may exist **without throwing exceptions**,
* fatal errors must eventually result in an exception,
* diagnostics may be emitted *before* an exception is thrown,
* diagnostics may be consumed even if an exception aborts execution.

---

## 4. Diagnostic severity levels

AJIS defines the following severity levels:

* **Info** – informational messages (optional)
* **Warning** – non-fatal issues, recoverable conditions
* **Error** – fatal parsing or semantic errors
* **Critical** – internal or invariant violations

Severity determines:

* whether parsing may continue,
* whether an exception must be thrown,
* how tooling should react.

---

## 5. Diagnostic identity

Each diagnostic is identified by:

* a **stable diagnostic code** (e.g. `AJIS1003`),
* a **message key** for localization,
* optional structured parameters.

The diagnostic code:

* must be stable across versions,
* must not depend on localized text,
* may be used for filtering and automation.

---

## 6. Source location model

Diagnostics must report **precise source location**.

Normatively supported location data:

* absolute byte offset,
* line number (1-based),
* column number (1-based),
* optional logical path (e.g. JSON/AJIS property path).

Location data must be:

* monotonic during streaming,
* accurate even in large files,
* independent of rendering format.

---

## 7. Localization model

AJIS diagnostics are **fully localizable**.

Rules:

* diagnostics store **message keys**, not rendered text,
* localization occurs at rendering time,
* fallback language must be available.

A minimal embedded dictionary may be provided by the implementation,
with support for user-supplied dictionaries.

Localization must not affect parsing performance.

---

## 8. Rendering and presentation

Diagnostics may be rendered in multiple ways:

* plain text (CLI),
* structured output (JSON, AJIS),
* APEP-style human-readable blocks,
* UI components.

Rendering:

* is not part of the diagnostic core,
* may be deferred or repeated,
* may include source excerpts when available.

---

## 9. Diagnostics and event streams

Diagnostics integrate with the AJIS event system.

Rules:

* diagnostics are emitted as **diagnostic events**,
* diagnostic events must not be dropped,
* ordering relative to other events must be preserved.

Diagnostics may also be collected independently of event streams.

---

## 10. Performance considerations

Diagnostics creation must:

* avoid string formatting on hot paths,
* minimize allocations,
* allow pooling or reuse where appropriate.

When diagnostics are disabled or ignored,
the overhead must be negligible.

---

## 11. Relationship to implementations

This document defines **what diagnostics represent**.

Implementation documents define:

* concrete diagnostic structures,
* exception types,
* rendering helpers.

Implementations must not weaken or reinterpret these semantics.

---

## 12. Status of this specification

This document is considered **stable**.

Future versions may add new diagnostic codes
but existing codes and semantics must remain valid.
