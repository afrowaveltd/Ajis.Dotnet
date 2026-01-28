# AJIS Modes and Heuristics (Auto Selection)

> Normative specification for mode selection, heuristics, and deterministic fallbacks

---

## 1. Scope of this document

This document defines how AJIS selects operational modes for:

* parsing,
* serialization,
* streaming pipelines.

It introduces **Auto selection**, which allows AJIS to choose the most suitable
strategy based on input properties and runtime constraints.

---

## 2. Goals

Auto selection must satisfy:

* **Safety** – never choose a mode that risks uncontrolled memory growth
* **Determinism** – identical conditions yield identical mode decisions
* **Speed** – mode selection must be fast (sub-millisecond for typical inputs)
* **Observability** – decision reasoning can be emitted via events (debug)
* **Override support** – user may force a specific mode

---

## 3. Defined modes

AJIS defines the following logical modes:

* **SmallFiles** – convenience and rich model
* **BigFiles** – bounded memory streaming
* **ExtraFast** – aggressive hot path, trusted inputs
* **Auto** – selects one of the above

Implementations may internally map these to multiple concrete strategies.

---

## 4. Inputs to the heuristic

Auto selection MAY consider:

### 4.1 Input size

* total input length if known
* rolling estimate if unknown

### 4.2 Structure density

* nesting depth
* token density (commas, colons, braces)
* average value length

### 4.3 Feature usage

* comments
* multi-line text blocks
* non-decimal numbers (binary/octal/hex)
* escape complexity

### 4.4 Runtime conditions

* available memory estimate
* configured memory limits
* user-provided hints

---

## 5. Probe phase

Auto selection performs a **probe phase**.

Probe phase requirements:

* must not allocate large buffers
* must not decode full strings unless required
* must not consume the stream irreversibly unless the implementation supports rewind

Recommended probe approach:

* inspect the first N bytes (e.g. 8–64 KB)
* track simple counters and maxima
* optionally sample deeper blocks if safe

Probe may emit debug events.

---

## 6. Decision rules

Auto mode produces a **ModeDecision** result.

Normative guidance:

* if the input size is unknown or large, prefer **BigFiles**
* if nesting is extreme, prefer **BigFiles** with strict depth limits
* if input is small and developer convenience is preferred, choose **SmallFiles**
* if caller explicitly marks input as trusted and requests max throughput, choose **ExtraFast**

Decisions must be conservative.

---

## 7. Deterministic fallback

If the probe is inconclusive:

* the default fallback must be **BigFiles**.

Rationale:

* BigFiles ensures bounded memory,
* avoids catastrophic failures on huge inputs,
* preserves correctness.

---

## 8. Overrides

Users may override Auto selection by:

* forcing a specific mode in options
* providing explicit hints (expected size, trusted input, required features)

Overrides must:

* take precedence over heuristics
* be observable via diagnostic/debug events

---

## 9. Mode-specific limits

Each mode may define safety limits:

* maximum nesting depth
* maximum string length
* maximum numeric token length

Auto mode may adjust limits dynamically
based on observed conditions.

---

## 10. Observability

Mode selection may emit:

* **ModeSelected** events
* **ProbeStats** events
* **FallbackUsed** events

These events:

* must be disabled by default (debug-only)
* must never affect parsing throughput

---

## 11. Relationship to other documents

This document complements:

* Parsers and Modes (behavioral contracts)
* Streaming and Pipelines (flow and composition)
* Events (observability)

---

## 12. Status of this specification

This document is considered **stable**.

Implementations may improve heuristics,
but must preserve the safety and determinism principles.
