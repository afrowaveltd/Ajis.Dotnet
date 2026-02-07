# AJIS Events and Observability

> Normative specification for event streaming, progress reporting, and diagnostics emission

---

## 1. Scope of this document

This document defines the **AJIS event model**, which enables:

* progress reporting during long-running operations,
* emission of diagnostics and warnings,
* observability without blocking parsing or serialization,
* integration with user interfaces, logging systems, and tooling.

This document is **normative** for all AJIS implementations that claim support for event streaming.

---

## 2. Design goals

The AJIS event system is designed to satisfy the following goals:

* **Non-intrusive** – must not affect core parsing performance when unused
* **Streaming-first** – events are emitted incrementally
* **Fire-and-forget** – producers must never wait on consumers
* **Backpressure-safe** – event emission must not cause unbounded memory growth
* **Deterministic** – event semantics are predictable and well-defined

---

## 3. Event stream model

AJIS defines a **single unified event stream** per operation.

All events emitted during parsing, serialization, or related operations
belong to this stream.

Conceptually, the event stream is:

* ordered,
* append-only,
* asynchronous,
* optional.

Consumers may subscribe to the stream to observe progress or diagnostics,
or ignore it entirely.

---

## 4. Event transport abstraction

AJIS does not mandate a specific transport mechanism,
but the reference .NET implementation uses:

* a bounded asynchronous channel,
* optimized for low overhead and lock-free fast paths.

Normative requirements:

* event emission **must never block** the core operation,
* if the event buffer is full, defined drop or coalescing policies apply,
* the absence of a consumer must not degrade performance.

---

## 5. Event categories

AJIS events are categorized by purpose.

### 5.1 Progress events

Progress events report forward movement of an operation.

Typical use cases:

* updating progress bars,
* estimating remaining time,
* visualizing large file processing.

Normative rules:

* progress events may be **coalesced**,
* only monotonic progress is allowed,
* percentage-based progress is optional but recommended.

Reference implementation notes:

* `ParseSegmentsAsync` emits `AjisMilestoneEvent` with name `parse` at start/end
* `AjisProgressEvent` is emitted with 0% and 100% when total size is known
* `AjisSerialize.ToStreamAsync` and `AjisSerializer.SerializeAsync` emit `serialize` milestones and progress

---

### 5.2 Diagnostic events

Diagnostic events report:

* warnings,
* recoverable errors,
* non-fatal anomalies.

Rules:

* diagnostic events **must not be dropped**,
* ordering relative to progress events must be preserved,
* diagnostics may reference precise source locations.

---

### 5.3 Phase and lifecycle events

Phase events describe high-level operation stages, such as:

* initialization,
* parsing,
* validation,
* serialization,
* finalization.

These events are optional but useful for tooling and tracing.

---

### 5.4 Debug and trace events

Debug-level events:

* provide fine-grained insight into internal behavior,
* are intended for development and diagnostics,
* may be dropped aggressively if buffer pressure occurs.

Implementations must ensure that disabling debug events
eliminates their runtime overhead.

---

## 6. Event throttling and coalescing

To prevent excessive event traffic, AJIS defines throttling policies.

Normative guidance:

* progress events should be emitted at meaningful intervals
  (e.g. percentage change or time-based thresholds),
* repeated progress updates may be coalesced into the most recent state,
* diagnostics and lifecycle events must bypass throttling.

---

## 7. Error interaction

AJIS events coexist with exception-based error handling.

Rules:

* fatal errors may be reported via diagnostics before an exception is thrown,
* event emission must cease once a fatal exception terminates the operation,
* event streams must be properly completed or closed on failure.

---

## 8. Completion semantics

At the end of an operation:

* the event stream must signal completion,
* no further events may be emitted,
* consumers must be able to distinguish normal completion from failure.

Completion is a semantic boundary, not an event type.

---

## 9. Performance considerations

Event emission must satisfy:

* O(1) or amortized O(1) cost per event,
* zero or near-zero allocations on the hot path,
* no mandatory synchronization with consumers.

When events are disabled, the implementation must reduce to a no-op.

---

## 10. Relationship to implementations

This document defines **what events mean** and **when they may occur**.

Implementation documents define:

* concrete event data structures,
* transport choices,
* language-specific APIs.

Implementations must not change the semantics defined here.

---

## 11. Status of this specification

This document is considered **stable** for the AJIS text core.

Minor clarifications may be added, but fundamental semantics
are not expected to change.
