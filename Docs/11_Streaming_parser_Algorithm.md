# AJIS Streaming Parser Algorithm (Single-Pass, Low-Memory)

> Normative description of the BigFiles streaming parser strategy

---

## 1. Scope of this document

This document specifies a **single-pass, low-memory parsing algorithm** for AJIS
textual data.

It is intended for:

* very large files (hundreds of MB to multi-GB),
* millions of items,
* constrained memory environments,
* streaming I/O (non-seekable input).

This algorithm is the reference behavior for **BigFiles mode**.

---

## 2. High-level idea

The streaming parser:

* reads input **byte-by-byte** or in small buffers,
* maintains a **stack of container frames** (object/array),
* tracks source position (**byteOffset, line, column**),
* recognizes tokens while respecting **string boundaries and escapes**,
* emits completed units as soon as they become complete:

  * completed array items,
  * completed property values,
  * completed subtrees.

The parser does not require full-document materialization.

---

## 3. Core state and invariants

### 3.1 Global state

The parser maintains:

* `byteOffset` (0-based)
* `line` (1-based)
* `column` (1-based)
* `depth` (0-based)
* `inString` (bool)
* `escapeActive` (bool)
* `stringDelimiter` (e.g. `"`)
* `tokenStartOffset` (for diagnostics)

### 3.2 Stack frames

Each container frame represents an object or array:

* `frameId` (monotonic integer)
* `parentFrameId` (nullable)
* `kind` (Object | Array)
* `state` (ExpectKey | ExpectColon | ExpectValue | ExpectCommaOrEnd)
* `currentKey` (optional, may be stored as slice/segment)
* `itemIndex` (arrays)

Invariants:

* stack depth equals current nesting depth
* top frame represents the active container

---

## 4. String handling (critical rule)

When inside a string:

* all characters including `{ } [ ] : ,` are treated as data,
* container depth must NOT be affected,
* the string terminates only on a **non-escaped delimiter**.

Escape rules:

* `\"` does not end the string
* `\\` toggles escape state appropriately

If invalid escape is encountered, a fatal diagnostic must be produced.

---

## 5. Tokenization strategy

The parser recognizes these token classes:

* structural: `{ } [ ] : ,`
* string: `" ... "`
* literals: `true`, `false`, `null`
* numbers: decimal, binary, octal, hex, with AJIS separators
* comments: if enabled by Format Norms
* whitespace: skipped outside strings

Tokenization must be:

* deterministic
* bounded-memory
* precise in error position

---

## 6. Container processing

### 6.1 Opening containers

When encountering `{` or `[` outside strings:

* create a new frame with `parentFrameId = currentFrameId`
* push it onto the stack
* increment `depth`

If this is the first container, it becomes the root container.

### 6.2 Closing containers

When encountering `}` or `]` outside strings:

* validate that it matches the top frame kind
* finalize the frame
* pop it
* decrement `depth`

Finalization may trigger **emission**.

---

## 7. Emission rules

The streaming parser must emit completed units as soon as possible.

### 7.1 Property completion

In an object:

* a property is complete once its value is complete
* completion occurs when:

  * a primitive value ends (comma or end brace), or
  * a child container closes

Upon completion, emit:

* `ObjectPropertyComplete` (key, value, parentFrameId)

### 7.2 Array item completion

In an array:

* an item is complete once its value is complete

Upon completion, emit:

* `ArrayItemComplete` (index, value, parentFrameId)

### 7.3 Subtree completion

When a frame closes:

* emit `ContainerComplete` for the frame

This allows consumers to process large subtrees without holding ancestors.

---

## 8. Value representation in streaming mode

In BigFiles mode, values may be represented as:

* immutable primitives (bool/null/number)
* lazy strings (slice + decode-on-demand)
* container references (frameId) + emitted children

Implementations may expose:

* token slices (`ReadOnlySpan<byte>` / `ReadOnlySequence<byte>`)
* or fully materialized values depending on API.

Values must never be mutated after emission.

---

## 9. Position tracking

Position tracking is mandatory.

Rules:

* `byteOffset` increments for every byte consumed
* `line` increments on LF (`\n`)
* `column` resets on LF, increments otherwise

CRLF handling:

* `\r\n` counts as a single line break
* column resets after `\n`

Position tracking must remain correct in buffered reads.

---

## 10. Error handling

Fatal errors include:

* unexpected token for current state
* mismatched closing brackets
* unterminated string
* invalid number format
* invalid escape

On fatal error:

* emit a diagnostic event
* throw a parsing exception
* stop consuming further bytes

Non-fatal issues may be warnings if supported.

---

## 11. Continuation after text section (ATP readiness)

When used inside ATP:

* the parser must stop exactly at the end of the text section
* the caller may continue reading binary payloads from the same stream

To support this:

* the parser must not over-read beyond the last meaningful byte
* buffering must be managed so that unread bytes remain accessible

---

## 12. Status of this specification

This algorithm is considered **stable**.

Implementations may optimize internals,
but must preserve:

* string boundary rules
* deterministic tokenization
* emission semantics
* accurate position reporting.
