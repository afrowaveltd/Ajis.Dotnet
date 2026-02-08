# AJIS UTF-8 Slices (M1)

## Status

**M1 – Locked contract**

This document defines the canonical slice model used by AJIS StreamWalk.
Slices are the primary mechanism for zero-allocation delivery of textual tokens.

---

## 1. Purpose

AJIS exposes textual tokens as **UTF-8 slices** to:

* avoid allocating strings
* enable fast downstream parsing
* support low-level and streaming scenarios

Slices are used for:

* property names
* string values
* number tokens
* (optionally) comments and directives

---

## 2. Core Type

The canonical conceptual type is:

* `AjisSliceUtf8`

A slice represents a contiguous region of UTF-8 bytes.

A slice includes:

* pointer/offset
* length
* flags (metadata)

Implementations MAY represent this as:

* `(ReadOnlySpan<byte> bytes, AjisSliceFlags flags)`
* `(byte* ptr, int len, AjisSliceFlags flags)`
* equivalent safe/unsafe representation

---

## 3. Slice Lifetime

### 3.1 Callback-bound validity

**A slice is valid ONLY during the visitor callback that receives it.**

After the callback returns:

* the slice data MAY be overwritten
* the slice pointer/span MAY become invalid

### 3.2 Storage rule

If the consumer needs to retain the data beyond the callback, it MUST:

* copy the bytes, or
* decode into an owned string/buffer

Holding references to slices across callbacks is undefined behavior.

---

## 4. Slice Semantics

### 4.1 Raw token representation

A slice represents the token text as it appears in the input, with these rules:

* no normalization
* no unescaping
* no unicode normalization
* UTF-8 only

The slice content MUST be stable with respect to buffering:

* span input and stream input MUST produce identical slice bytes

---

## 5. Standard Slice Flags

`AjisSliceFlags` is a bitmask.

Implementations MUST support (at minimum) the following flags.

### 5.1 Flags (required)

* `None`

  * no special properties

* `HasEscapes`

  * the slice contains at least one escape sequence in source form

* `HasNonAscii`

  * the slice contains at least one byte >= 0x80

* `IsIdentifierStyle`

  * token uses AJIS identifier-style form (if AJIS permits unquoted keys or special name forms)
  * if the implementation does not support this AJIS feature, it MUST never set this flag

* `IsNumberHex`

  * numeric token is expressed in hexadecimal form (AJIS extension)

* `IsNumberBinary`

  * numeric token is expressed in binary form (AJIS extension)

* `IsNumberOctal`

  * numeric token is expressed in octal form (AJIS extension)

Note:

* For JSON compatibility mode, `IsNumberHex/IsNumberBinary/IsNumberOctal` MUST NOT occur.

In the .NET reference implementation, these flags are surfaced on `AjisSliceFlags` for string and name segments.

### 5.2 Optional flags (allowed)

Implementations MAY provide additional flags as explainable performance hints, such as:

* `IsSimpleAscii`
* `IsLikelyInt`
* `IsLikelyFloat`

Optional flags MUST NOT change semantics.

---

## 6. Slice Kinds and Boundaries

### 6.1 String slices

A string slice represents the **string contents**, not including surrounding quotes.

Rules:

* `OnString` delivers the UTF-8 bytes that correspond to the string body.
* Escape sequences remain in source form (if present), and `HasEscapes` MUST be set.
* If the string contains non-ASCII bytes, `HasNonAscii` MUST be set.

Example:

Input:

```ajis
"a\\n\u263a"
```

Delivered slice bytes (conceptual):

* `a\\n\u263a`

Flags:

* `HasEscapes`
* `HasNonAscii` MAY be false because bytes are ASCII even though the logical value may be non-ASCII.

Important distinction:

* `HasNonAscii` is about **source bytes**, not decoded scalar values.

---

### 6.2 Property name slices

Property name slices follow the same rules as string slices:

* delivered as string contents (no surrounding quotes)
* no unescaping

If AJIS supports identifier-style names, the slice MUST represent the raw identifier bytes.

---

### 6.3 Number slices

A number slice represents the **raw numeric token text**.

Rules:

* no conversion is performed
* no normalization (e.g., `+0`, `01`, `1.0`, `1e0` remain as written)

If AJIS supports base prefixes:

* the slice includes the full token spelling, including any prefix
* the corresponding base flag MUST be set

If AJIS supports typed literals:

* the typed literal flag MUST be set

The .NET M1 reference implementation preserves prefixed number spellings in segment text when base prefixes are enabled.
When using segments, these flags are surfaced as `AjisSegmentFlags` on number segments.

Examples (conceptual):

* `123` → flags: `None`
* `0xFF` → flags: `IsNumberHex`
* `0b1010` → flags: `IsNumberBinary`
* `0o755` → flags: `IsNumberOctal`
* `T170` → flags: `IsNumberTyped`

---

## 7. Decoding and Unescaping (consumer-side)

StreamWalk does not require decoding.

A consumer MAY choose to:

* unescape into an owned UTF-8 buffer
* decode into UTF-16 string (platform dependent)
* validate numeric formats

Because escape handling and numeric parsing policy can vary by use-case, these operations are **deliberately not enforced** by StreamWalk.

---

## 8. Security Notes

Consumers must treat slices as untrusted input.

Recommended safety rules:

* enforce maximum token sizes (reader options)
* avoid allocating unbounded buffers during unescape
* validate numeric ranges before conversion

Slice lifetime violations are a correctness and potential security risk.

---

## 9. Compliance Checklist

A compliant implementation MUST:

* deliver slices as UTF-8
* keep slice bytes identical between span and stream input
* keep slices valid only during callback
* not unescape or normalize slice contents
* set required flags correctly when applicable

---

*End of document.*
