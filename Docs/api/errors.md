# AJIS Errors (M1)

## Status

**M1 – Locked contract**

This document defines the canonical error model for AJIS parsing and StreamWalk.

---

## 1. Purpose

AJIS errors must be:

* deterministic
* machine-readable
* portable across languages
* independent of diagnostics settings

The same input + options MUST produce the same error code at the same failure point.

---

## 2. Error Transport

Implementations SHOULD expose errors as a struct/object (not as exceptions).

Conceptual model:

* `AjisError`

  * `Code` (enum)
  * `Offset` (byte offset from start, 0-based)
  * `Line` (optional)
  * `Column` (optional)
  * `Message` (optional human text)
  * `TokenPreview` (optional, bounded)

Rules:

* `Code` and `Offset` MUST always be available.
* `Line/Column` MAY be omitted unless enabled.
* `Message` MUST NOT be used for program logic.

---

## 3. Success vs Failure Semantics

### 3.1 Visitor completion

* On success: `OnEndDocument` MUST be emitted exactly once.
* On failure: `OnEndDocument` MUST NOT be emitted.

### 3.2 Partial events

On failure, the visitor MAY have already received a partial event stream.
This is expected.
Consumers must treat the document as invalid.

---

## 4. Canonical Error Codes

`AjisErrorCode` is an enum.

Implementations MUST support at least the following codes.

### 4.1 I/O and infrastructure

* `None`

  * no error

* `IoError`

  * underlying stream read error

* `OutOfMemory`

  * allocation failure (rare, but must be representable)

### 4.2 Structural / syntax

* `UnexpectedEndOfInput`

  * input ended while inside an unfinished token or structure

* `UnexpectedToken`

  * token is not valid in the current parser state

* `InvalidCharacter`

  * byte is not allowed at this position

* `InvalidEscapeSequence`

  * invalid escape form encountered

* `InvalidUnicodeEscape`

  * unicode escape is malformed (if validated)

* `InvalidNumber`

  * numeric token is syntactically invalid

* `InvalidLiteral`

  * invalid keyword/literal (true/false/null or AJIS literals)

* `TrailingGarbage`

  * extra non-whitespace data after a complete document

### 4.3 Limits

* `MaxDepthExceeded`

* `MaxTokenBytesExceeded`

* `MaxDocumentBytesExceeded`

* `MaxStringBytesExceeded` (optional)

* `MaxPropertyNameBytesExceeded` (optional)

### 4.4 Mode / compatibility

* `NotAllowedInJsonMode`

  * AJIS extension used while JsonCompatibility is enabled

* `FeatureDisabled`

  * a feature flag is disabled and the implementation rejects it

---

## 5. Offset Semantics

`Offset` is the byte index (0-based) where the error was detected.

Rules:

* for invalid bytes: offset points at the offending byte
* for unexpected end: offset equals total bytes consumed
* for trailing garbage: offset points at the first non-whitespace garbage byte

Offsets must be stable across:

* span parsing
* stream parsing (any buffer size)

---

## 6. Line and Column (optional)

When enabled, line/column are computed using:

* `\n` as line break
* `\r\n` treated as a single line break

Columns are measured in bytes of the current line (UTF-8 bytes), not Unicode scalar count.

This keeps the model portable and cheap.
Consumers may compute display columns differently if needed.

---

## 7. Token Preview (optional)

A token preview is a short bounded slice of nearby bytes for human diagnostics.

Rules:

* MUST be bounded by `MaxPreviewBytes` (implementation constant or option)
* MUST NOT allocate unbounded memory
* MUST NOT change `Code` or `Offset`

Preview bytes MAY be escaped for display.

---

## 8. Determinism Rules

* Error codes MUST be stable.
* Buffer sizes MUST NOT change the error code.
* Optional diagnostics MUST NOT affect code/offset.

---

## 9. Compliance Checklist

A compliant implementation MUST:

* provide `Code` + `Offset`
* not emit `OnEndDocument` on failure
* implement the canonical error codes (or a strict superset)
* keep offsets stable across span/stream parsing

---

*End of document.*
