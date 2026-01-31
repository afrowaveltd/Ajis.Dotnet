# AJIS Events / Visitor Contract (M1)

## Status

**M1 – Locked contract**

This document defines the canonical StreamWalk visitor event model.
All AJIS readers/parsers MUST be able to drive a visitor using these events.

---

## 1. Purpose

The event model enables:

* allocation-free streaming parse
* building DOM-like structures as an optional layer
* zero-copy slices for tokens
* deterministic behavior across span and stream input

The visitor receives a sequence of events describing the document.

---

## 2. Event Ordering Rules

### 2.1 Determinism

For the same input + options, the event sequence MUST be identical.

### 2.2 Containers

* `OnBeginObject` / `OnEndObject` MUST be balanced
* `OnBeginArray` / `OnEndArray` MUST be balanced
* containers are properly nested

### 2.3 End of document

* On success: `OnEndDocument` emitted exactly once
* On failure: `OnEndDocument` MUST NOT be emitted

### 2.4 Comments and directives

If enabled:

* comments/directives MAY appear between any two structural tokens
* they MUST NOT break container balance

---

## 3. Slice Lifetime

All events that include slices (names, strings, numbers, directives, comments) follow:

* slice content is valid only for the duration of the callback
* consumers MUST copy if they need persistence

This rule is required for both span and stream readers.

---

## 4. Canonical Event Set

Implementations MUST support at least these events.
Exact method names may differ, but semantics MUST match.

### 4.1 Document

* `OnBeginDocument()` (optional)

  * may be emitted before the first real token
  * if implemented, MUST be emitted exactly once on success and failure

* `OnEndDocument()`

  * emitted exactly once on success
  * never emitted on failure

### 4.2 Containers

* `OnBeginObject()`

* `OnEndObject()`

* `OnBeginArray()`

* `OnEndArray()`

### 4.3 Object member naming

Object members are represented by a name event followed by a value event.

* `OnName(slice)`

  * emitted only inside an object
  * must be followed by exactly one value event (primitive or container)

Name slice flags MUST be provided as defined in `docs/api/slices.md`:

* quoted vs identifier-style

### 4.4 Primitive values

* `OnString(slice)`

* `OnNumber(slice)`

* `OnBooleanTrue()`

* `OnBooleanFalse()`

* `OnNull()`

Number slice flags MUST be provided (bases, sign, etc.) per slices contract.

### 4.5 Non-structural extensions

If enabled by options:

* `OnComment(slice)`

  * comment slice includes full lexical payload according to slices contract

* `OnDirective(slice)`

  * directive slice includes full lexical payload according to slices contract

If disabled, implementation MUST follow its documented policy:

* reject with error, or
* skip silently

---

## 5. Value Event Definition

A "value event" is one of:

* `OnString`
* `OnNumber`
* `OnBooleanTrue`
* `OnBooleanFalse`
* `OnNull`
* `OnBeginObject` (with its matching end)
* `OnBeginArray` (with its matching end)

Inside an object:

* after `OnName`, exactly one value event MUST occur.

Inside an array:

* value events occur in sequence.

---

## 6. JSON Compatibility Requirements

When JsonCompatibility is enabled:

* `OnComment` MUST NOT be emitted
* `OnDirective` MUST NOT be emitted
* any AJIS-only token forms MUST be rejected deterministically

The remaining event stream MUST match JSON semantics.

---

## 7. Compliance Checklist

A compliant visitor model MUST:

* define the canonical event set (or strict superset)
* guarantee deterministic ordering
* enforce container balancing
* enforce object name -> single value rule
* enforce slice lifetime rule
* obey success/failure `OnEndDocument` rules

---

*End of document.*
