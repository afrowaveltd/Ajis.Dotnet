# AJIS Compliance Checklist (M1)

## Status

**M1 – Compliance definition**

This document defines what an implementation MUST satisfy to be considered **AJIS‑compliant (M1)**.

---

## 1. Scope

This checklist applies to:

* span‑based parsers
* stream‑based parsers
* all language implementations

Reference implementation for M1 is **.NET**.

---

## 2. Input Handling

An M1‑compliant implementation MUST:

* accept UTF‑8 byte input
* accept any valid JSON as valid AJIS
* reject invalid UTF‑8 deterministically
* support span OR stream input (stream preferred)

Stream implementations MUST:

* tolerate partial reads
* work with non‑seekable streams

---

## 3. StreamWalk Contract

The implementation MUST:

* perform a single‑pass walk
* not require full document buffering
* invoke visitor callbacks synchronously
* preserve span/stream parity

For identical input + options:

* event sequence MUST be identical
* error code and offset MUST be identical

---

## 4. Visitor Events

The implementation MUST emit:

* container events (`Begin/End Object`, `Begin/End Array`)
* name/value pairing for objects
* primitive value events

Rules:

* container balance MUST be correct
* after `OnName`, exactly one value MUST follow
* `OnEndDocument` MUST be emitted exactly once on success
* `OnEndDocument` MUST NOT be emitted on failure

---

## 5. Slice Semantics

Slices passed to the visitor MUST:

* represent contiguous bytes
* follow slice flag definitions
* be valid only during callback lifetime

Tokens spanning buffer boundaries MUST be assembled deterministically.

---

## 6. Options and Modes

The implementation MUST support:

* AJIS mode (default)
* JSON compatibility mode

In JSON compatibility mode:

* comments MUST NOT be emitted
* directives MUST NOT be emitted
* AJIS‑only syntax MUST be rejected

---

## 7. Extensions (AJIS features)

An M1‑compliant implementation MUST support:

* comments (if enabled)
* directives (if enabled)
* identifier‑style property names (if enabled)

If disabled:

* behavior MUST be deterministic (reject or skip as documented)

---

## 8. Limits and Safety

The implementation MUST enforce:

* maximum nesting depth
* maximum token size

Optional but recommended:

* maximum document size

On limit violation:

* parsing MUST stop immediately
* deterministic limit error MUST be returned

---

## 9. Error Model

The implementation MUST:

* return structured errors (not only exceptions)
* provide error code and byte offset
* keep errors stable across buffer sizes

Optional diagnostics:

* line/column
* token preview

Diagnostics MUST NOT affect error code or offset.

---

## 10. Determinism Rules

An M1‑compliant implementation MUST be deterministic:

* same input + options → same events
* same input + options → same error
* buffer size MUST NOT change behavior

---

## 11. Reference Authority

For M1:

* **.NET implementation is authoritative**
* other implementations MUST converge to its externally visible behavior

This includes:

* event ordering
* slice flags
* error codes
* option defaults

---

## 12. Non‑requirements (Explicit)

M1 does NOT require:

* DOM building
* schema validation
* round‑trip formatting guarantees
* mutation APIs

---

## 13. Versioning Rule

Once an M1 document is locked:

* behavior MUST NOT change
* only M2 may extend or relax rules

---

*End of document.*
