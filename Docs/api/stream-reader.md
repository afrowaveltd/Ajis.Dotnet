# AJIS Stream Reader (M1)

## Status

**M1 – Locked contract**

This document defines the canonical streaming reader wrapper used by AJIS StreamWalk.
It covers buffering, refill behavior, and parity requirements relative to span-based parsing.

---

## 1. Purpose

The Stream Reader adapts a byte stream (`Stream`, file, socket, pipe) into the same semantic model as the span-based reader.

It MUST:

* deliver deterministic tokenization independent of buffer sizes
* support very large inputs
* minimize allocations
* allow incremental processing

---

## 2. Inputs

A Stream Reader consumes a sequential byte source.

Requirements:

* bytes are interpreted as UTF-8 encoded text
* the stream may return partial reads
* the stream may be non-seekable

The Stream Reader MUST NOT require `Length` or `Position`.

---

## 3. Buffer Model

### 3.1 Internal buffer

The Stream Reader maintains an internal byte buffer:

* `buffer` (byte array or equivalent)
* `start` (index of first unread byte)
* `end` (index one past last available byte)

Unread data is `buffer[start..end]`.

### 3.2 Refill

When additional data is needed:

1. unread bytes MAY be compacted to the beginning of the buffer
2. more bytes are read from the stream into the free region

Refill MUST preserve the exact byte sequence.

---

## 4. Token and Slice Parity

### 4.1 Deterministic event stream

For any valid input, StreamWalk events MUST be identical for:

* span input
* stream input (any buffer size)

Buffer boundaries MUST NOT affect:

* event order
* event count
* token boundaries
* slice bytes

### 4.2 Slice backing storage

Slices delivered to the visitor MUST refer to contiguous bytes.

If a token spans a buffer boundary, the Stream Reader MUST provide a contiguous representation.
Allowed strategies:

* use a secondary scratch buffer to assemble the token
* use a growable token buffer (bounded by options)

When token assembly is used:

* slice lifetime rules still apply
* the slice MUST remain valid only during the callback

---

## 5. Lookahead and Consumption Semantics

The Stream Reader MUST support:

* peeking the current byte
* consuming/advancing by one or many bytes
* detecting end-of-input

Lookahead MUST be transparent to the visitor:

* no bytes may be skipped
* no bytes may be duplicated

---

## 6. Maximum Sizes and Safety Limits

The Stream Reader MUST enforce configurable limits (via options):

* `MaxTokenBytes`

  * maximum size of any single token (string, number, comment, directive)

* `MaxDepth`

  * maximum nesting depth observed by the parser

* `MaxDocumentBytes` (optional)

  * maximum total bytes read before aborting

If a limit is exceeded:

* parsing MUST stop deterministically
* `OnEndDocument` MUST NOT be emitted
* a deterministic error MUST be returned

---

## 7. End-of-Stream and Partial Tokens

If the stream ends while inside an incomplete token:

* parsing MUST fail with a deterministic error (unexpected end of input)

This includes:

* unterminated string
* unfinished escape sequence
* incomplete number token
* unclosed object/array

---

## 8. Performance Guidance (normative constraints)

M1 does not require a specific algorithm, but implementations MUST:

* avoid per-byte allocations
* avoid copying unless necessary (boundary-spanning tokens)
* keep refill deterministic

Recommended:

* compact rather than allocate new buffers
* keep token scratch buffer reused between tokens

---

## 9. JSON Compatibility Mode

JSON compatibility mode affects validation rules, not streaming mechanics.

The Stream Reader MUST still:

* maintain parity with span parsing
* preserve token slices
* enforce the same limits

---

## 10. Compliance Checklist

A compliant Stream Reader MUST:

* work with non-seekable streams
* tolerate partial reads
* produce identical StreamWalk events as span-based parsing
* assemble boundary-spanning tokens into contiguous slices
* enforce token/depth limits deterministically
* treat unexpected end-of-stream as a deterministic error

---

*End of document.*
