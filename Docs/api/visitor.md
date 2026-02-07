# AJIS Visitor Contract (M1)

## Status

**M1 – Locked contract**

This document defines the canonical visitor interface semantics for AJIS StreamWalk.
It is normative for all implementations that claim StreamWalk (M1) compliance.

---

## 1. Purpose

A visitor is the consumer-side contract used by StreamWalk to deliver:

* structure (objects, arrays)
* names (property keys)
* values (null, bool, number, string)
* optional extended syntax (comments, directives)

The visitor MUST be able to process input incrementally without requiring DOM materialization.

---

## 2. Fundamental Guarantees

A compliant StreamWalk implementation MUST guarantee:

1. **Deterministic ordering**

   * Events are emitted in a deterministic order as defined by `streamwalk.md`

2. **Single-pass, forward-only**

   * No rewinds or look-ahead exposure to the visitor

3. **No hidden buffering requirements**

   * Visitor must not depend on input source (span vs stream)

4. **Slice lifetime**

   * Any `AjisSliceUtf8` is valid ONLY during the callback

---

## 3. Interface Shape (conceptual)

The conceptual visitor interface includes the following callbacks:

### 3.1 Document lifecycle

* `OnStartDocument()` (optional in M1)
* `OnEndDocument()`

### 3.2 Structural events

* `OnStartObject()`
* `OnEndObject()`
* `OnStartArray()`
* `OnEndArray()`

### 3.3 Name event

* `OnPropertyName(AjisSliceUtf8 name)`

### 3.4 Value events

* `OnNull()`
* `OnBool(bool value)`
* `OnNumber(AjisSliceUtf8 number)`
* `OnString(AjisSliceUtf8 text)`

### 3.5 Optional extended events

* `OnComment(AjisSliceUtf8 text)`
* `OnDirective(AjisSliceUtf8 text)`

Implementations MAY define these as optional / no-op depending on feature flags.

---

## 4. Event Semantics

### 4.1 OnStartDocument / OnEndDocument

* `OnStartDocument` is optional in M1 and is not emitted by the .NET reference implementation.
* `OnEndDocument` MUST be emitted exactly once at the end of a successful traversal.
* If a fatal error occurs, `OnEndDocument` MUST NOT be emitted.

No other event may occur after `OnEndDocument`.

---

### 4.2 Object events

* `OnStartObject` indicates the beginning of an object.
* `OnEndObject` indicates the end of that same object.

Rules:

* Objects may be nested.
* The nesting MUST be properly balanced.

Within an object:

* `OnPropertyName` MUST occur zero or more times.
* Each `OnPropertyName` MUST be followed by exactly one value event.

---

### 4.3 Array events

* `OnStartArray` indicates the beginning of an array.
* `OnEndArray` indicates the end of that same array.

Rules:

* Arrays may be nested.
* The nesting MUST be properly balanced.

Within an array:

* Value events occur zero or more times.
* No `OnPropertyName` may occur directly inside an array.

---

### 4.4 OnPropertyName

`OnPropertyName(AjisSliceUtf8 name)` delivers the raw UTF-8 bytes of a property key.

Rules:

* MUST occur only when the current container is an object.
* MUST be followed immediately by exactly one value event.
* The name slice MUST represent exactly the source spelling (no unescape, no normalization).
* Property order MUST be preserved.

---

### 4.5 Value events

#### 4.5.1 OnNull

Emitted for the AJIS/JSON null literal.

#### 4.5.2 OnBool

Emitted for boolean literals.

* `true` → `OnBool(true)`
* `false` → `OnBool(false)`

#### 4.5.3 OnNumber

Emitted for numeric tokens.

Rules:

* Delivered as raw UTF-8 token slice.
* No numeric conversion is required.
* Visitor MAY parse to integer/float/decimal according to its needs.

#### 4.5.4 OnString

Emitted for string tokens.

Rules:

* Delivered as raw UTF-8 token slice representing the string text.
* No unescaping is performed.
* The visitor MAY decode/unescape if required.

Note:

* The .NET M1 reference implementation provides the string contents **without surrounding quotes**.

---

## 5. Comments and Directives

If enabled by options:

* Comments are emitted via `OnComment`.
* Directives are emitted via `OnDirective`.

Rules:

* These events MUST NOT alter structural/value ordering.
* They may appear:

  * between tokens
  * between property name and its value (only if syntax permits)
  * before/after the root value

If disabled:

* The implementation MUST skip them entirely.

Directive syntax:

* `#<namespace> <command> [key=value]...`

Reference helper:

* `AjisDirectiveParser` parses directive payloads into namespace/command/arguments
* `AjisDirectiveApplier.TryApply` applies supported directives to settings (returns updated settings)

---

## 6. Visitor Responsibilities

A visitor MUST:

1. **Not store slices**

   * It MUST NOT retain `AjisSliceUtf8` beyond the callback.
   * If it needs to store data, it MUST copy it.

2. **Not throw for control flow**

   * Exceptions SHOULD be reserved for truly exceptional conditions.
   * Normal validation failures SHOULD be represented in visitor state.

3. **Be re-entrant safe by design**

   * Visitor methods may be called rapidly and frequently.
   * Visitor SHOULD minimize allocations and locking.

---

## 7. Cancellation and Early Stop (M1)

M1 does not require a dedicated early-stop mechanism.

However, if an implementation provides one, it MUST:

* stop deterministically
* return a deterministic non-success result
* avoid emitting `OnEndDocument`

Any early-stop extension MUST NOT break existing visitors.

---

## 8. Compliance Checklist

A compliant visitor/event stream MUST satisfy:

* `OnStartDocument` once, first
* `OnEndDocument` once, last (only on success)
* balanced object and array start/end events
* `OnPropertyName` only inside objects
* each `OnPropertyName` followed by exactly one value event
* deterministic ordering independent of buffering
* slices valid only during callback

---

## Unified event delivery (M1)

StreamWalk uses a **single unified event channel**.

* `OnEvent(AjisStreamWalkEvent evt)` is called for every event in strict document order.
* `OnCompleted()` is called exactly once on successful completion.
* `OnError(AjisStreamWalkError error)` is called exactly once on failure.

This is the canonical M1 visitor contract and MUST NOT be split into multiple callbacks in M1.


*End of document.*
