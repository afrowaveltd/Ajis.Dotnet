# AJIS StreamWalk API (M1)

## Status

**M1 – Locked contract**

This document defines the canonical *StreamWalk* contract for AJIS.
Once accepted, this contract MUST remain stable across implementations and versions.

---

## 1. Purpose

StreamWalk is a **streaming, allocation-aware traversal** of an AJIS document.

It provides:

* deterministic event order
* zero or near-zero allocations
* UTF-8–first processing
* separation of parsing from object materialization

StreamWalk is the **foundational processing mode** of AJIS. All higher-level operations (DOM, canonicalization, ATP, binary attachment handling) are built on top of it.

---

## 2. Design Principles

1. **AJIS is canonical**

   * JSON is a strict subset of AJIS
   * AJIS features may be disabled for JSON compatibility, but not the other way around

2. **Streaming first**

   * No object tree is required
   * Consumers process data incrementally via events

3. **Allocation awareness**

   * No string allocation is required for correctness
   * Textual data is exposed as UTF-8 slices

4. **Determinism**

   * Same input → same event sequence
   * Independent of buffer sizes or input source (span vs stream)

---

## 3. StreamWalk Model

StreamWalk traverses an AJIS document and emits a sequence of **visitor events**.

The traversal is:

* depth-first
* single-pass
* forward-only

The visitor observes structure and values, but **does not own the data**.

---

## 3.1 .NET configuration mapping

In the .NET reference implementation, `AjisStreamWalkOptions` may be derived from
`Afrowave.AJIS.Core.Configuration.AjisSettings` via `AjisStreamWalkOptions.FromSettings`.
This mapping applies core settings (mode, limits, directives/comments, identifiers) to
StreamWalk defaults.

The `AjisStreamWalkRunner.Run(ReadOnlySpan<byte>, IAjisStreamWalkVisitor, AjisSettings, ...)`
overload uses this mapping internally to run StreamWalk without specifying options manually.

The `AjisStreamWalkRunner.RunWithDirectives` overload parses document directives first
and applies them to settings before running StreamWalk.

---

## 4. Event Sequence Rules

### 4.1 Document lifecycle

Every StreamWalk MUST follow this structure:

1. zero or more structural / value events
2. `OnEndDocument`

Notes:

* `OnStartDocument` is optional in M1 and not emitted by the .NET reference implementation.
* `OnEndDocument` MUST be called exactly once if and only if the document is syntactically valid.

---

### 4.2 Objects

For an object:

```ajis
{
  "a": 1,
  "b": 2
}
```

The event sequence is:

1. `OnStartObject`
2. `OnPropertyName("a")`
3. value event for `1`
4. `OnPropertyName("b")`
5. value event for `2`
6. `OnEndObject`

Rules:

* `OnPropertyName` MUST always be followed by exactly one value event
* Property order MUST be preserved

---

### 4.3 Arrays

For an array:

```ajis
[1, 2, 3]
```

The event sequence is:

1. `OnStartArray`
2. value event for `1`
3. value event for `2`
4. value event for `3`
5. `OnEndArray`

---

### 4.4 Values

The following value events exist:

* `OnNull`
* `OnBool(bool)`
* `OnNumber(AjisSliceUtf8)`
* `OnString(AjisSliceUtf8)`

Rules:

* Numeric and string values are delivered as **raw UTF-8 slices**
* Parsing or decoding is the responsibility of the visitor

---

## 5. UTF-8 Slices

## Slice semantics (M1)

In **M1**, all textual slices returned by StreamWalk are raw UTF-8 token slices.

* **PropertyName**: slice contains the name bytes without surrounding quotes.
* **String values**: slice contains the string bytes without surrounding quotes.
* **Number values**: slice is the exact UTF-8 token text (no normalization), e.g. `-12.34e+5` or `0xFF` when base prefixes are enabled.
* **Literals** (`true`, `false`, `null`): slice is empty in the M1 reference implementation.

Rationale: this keeps StreamWalk allocation-free and makes the test-suite a precise, deterministic oracle.

### 5.1 Slice lifetime

* A slice is valid **only during the visitor callback**
* The slice MUST NOT be stored or accessed after the callback returns

Note: the .NET reference implementation currently copies slices into owned memory for test tracing,
but callers MUST NOT rely on this behavior.

Violating this rule results in undefined behavior.

---

### 5.2 Slice contents

A slice represents the **raw textual representation** as it appears in the input:

* no unescaping is performed
* no normalization is applied
* encoding is always UTF-8

When base prefixes are enabled, numeric slices preserve the original prefix (`0x`, `0b`, `0o`).

Flags may indicate:

* presence of escape sequences
* presence of non-ASCII characters
* AJIS-specific syntax forms (e.g., base-prefixed numbers via `AjisSliceFlags`)
* string/name characteristics (escapes, non-ASCII, identifier-style)

---

## 6. Comments and Directives

AJIS may support comments and directives as extended syntax.

When enabled by options, the .NET reference implementation emits:

* `COMMENT` events for comment tokens
* `DIRECTIVE` events for directive tokens

Lax mode notes:

* Unterminated strings and block comments are tolerated in Lax mode
* Trailing commas and missing end delimiters are tolerated in Lax mode
* Leading plus, NaN, and Infinity number literals are tolerated in Lax mode
* Identifier tokens are emitted as `IDENTIFIER` in Lax mode
* Unquoted property names are allowed when identifiers are enabled in Lax mode
* Identifier tokens and unquoted property names are allowed in AJIS mode when identifiers are enabled
* Typed literals (e.g., `T1707489221`) are emitted as `NUMBER` with raw lexeme slices

StreamWalk behavior:

* Comments and directives MAY be emitted as events
* Emission is controlled via reader options
* When disabled, they MUST be skipped entirely

Their presence MUST NOT affect structural or value event ordering.

---

## 7. Error Handling

If a syntax or constraint error occurs:

* Traversal MUST stop immediately
* `OnEndDocument` MUST NOT be called
* A deterministic error result MUST be returned

Errors include (but are not limited to):

* invalid syntax
* unexpected end of input
* exceeded maximum depth
* exceeded maximum token size

Errors MUST NOT be reported via partial or ambiguous event sequences.

---

## 8. Span vs Stream Parity

The following inputs MUST produce identical event sequences:

* `ReadOnlySpan<byte>` input
* `Stream` input (any buffer size)

Buffer boundaries MUST NOT affect:

* event order
* event count
* slice contents

---

## 9. JSON Compatibility Mode

When operating in JSON compatibility mode:

* AJIS extensions MUST be rejected or ignored according to options
* Only JSON-valid constructs are permitted
* Event semantics remain unchanged

JSON compatibility affects **input validation only**, not StreamWalk behavior.

---

## 10. Non-Goals (M1)

The following are explicitly OUT OF SCOPE for StreamWalk M1:

* object materialization (DOM)
* canonical output generation
* binary attachment processing
* signature or cryptographic handling

These features MUST be built as layers on top of StreamWalk.

---

## 11. Stability Guarantee

This StreamWalk contract is considered **stable and normative** for AJIS M1.

Any future extension MUST:

* preserve existing event semantics
* maintain deterministic traversal
* avoid breaking existing visitors

---

*End of document.*
