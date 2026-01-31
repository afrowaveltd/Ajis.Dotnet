# AJIS StreamWalk (M1)

## Status

**M1 – Locked contract**

This document defines the canonical StreamWalk operation that drives a visitor over AJIS input.

---

## 1. Purpose

StreamWalk is the core execution model of AJIS parsing.

It:

* consumes input (span or stream)
* validates according to options
* emits a deterministic sequence of visitor events
* returns success or a deterministic error

StreamWalk itself does **not** build a DOM.

---

## 2. Inputs

A StreamWalk invocation consists of:

* `Input`

  * span-based byte source **or** stream-based byte source

* `Options`

  * parsing mode (AJIS / JSON compatibility)
  * feature flags (comments, directives, identifiers)
  * safety limits

* `Visitor`

  * receives events synchronously during the walk

---

## 3. Execution Model

### 3.1 Single-pass

StreamWalk is strictly single-pass.

Rules:

* bytes are consumed in order
* no backtracking beyond local lookahead
* no buffering of the entire document

---

### 3.2 Span vs Stream parity

For the same input bytes and options:

* span-based StreamWalk
* stream-based StreamWalk (any buffer size)

MUST produce:

* identical visitor event sequence
* identical error code and offset on failure

---

## 4. Visitor Invocation Rules

### 4.1 Synchronous callbacks

Visitor callbacks are invoked synchronously during parsing.

Rules:

* no reentrancy
* no parallel visitor calls
* visitor code MUST return before parsing continues

---

### 4.2 Slice lifetime

Slices passed to the visitor:

* are valid only during the callback
* MUST NOT be retained without copying

This applies uniformly to span and stream input.

---

## 5. Success and Failure Semantics

### 5.1 Success

On successful completion:

1. All input bytes are consumed (except trailing whitespace)
2. Container balance is correct
3. Validation rules are satisfied
4. `OnEndDocument` is emitted exactly once
5. StreamWalk returns `Success`

---

### 5.2 Failure

On failure:

* parsing stops immediately
* no further visitor events are emitted
* `OnEndDocument` MUST NOT be emitted
* a deterministic `AjisError` is returned

Partial visitor events MAY already have been delivered.

---

## 6. Trailing Data Rules

After the first complete document:

* trailing whitespace is allowed
* any non-whitespace byte is an error (`TrailingGarbage`)

This rule applies equally to AJIS and JSON modes.

---

## 7. Limits and Early Abort

If any configured limit is exceeded:

* parsing MUST stop immediately
* a limit-specific error code MUST be returned
* visitor is not informed of completion

Limits include (see options):

* maximum nesting depth
* maximum token size
* maximum document size

---

## 8. JSON Compatibility Mode

When JSON compatibility is enabled:

* AJIS extensions are rejected deterministically
* the visitor event stream corresponds to JSON semantics
* error behavior remains identical

---

## 9. Return Model

Conceptually, StreamWalk returns:

* `Success`

  * no error

* `Failure`

  * accompanied by an `AjisError`

Implementations MAY expose this as:

* return code + out parameter
* result struct
* error object

Exceptions MUST NOT be required for control flow.

---

## 10. Compliance Checklist

A compliant StreamWalk implementation MUST:

* operate in a single pass
* guarantee span/stream parity
* invoke visitor synchronously
* enforce slice lifetime rules
* enforce success/failure semantics strictly
* return deterministic errors

---

*End of document.*
