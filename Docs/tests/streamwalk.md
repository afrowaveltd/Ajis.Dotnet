# StreamWalk Test Cases (M1)

## Status

**M1 – Locked test case format**

This document defines the canonical, language-neutral test case format for AJIS StreamWalk.

It is designed to be:

* human-writable
* deterministic
* runnable in C and .NET

The authoritative behavior is the **.NET reference implementation**.

---

## 1. Test Case Structure

A StreamWalk test case consists of:

1. **Input**

   * AJIS/JSON text bytes (UTF-8)

2. **Options**

   * AJIS mode or JSON compatibility mode
   * feature flags
   * limits

3. **Expected Outcome**

   * either **Success** with expected event sequence
   * or **Failure** with expected error code and offset

---

## 2. Canonical Output: Event Trace

The expected event sequence is represented as a plain-text trace.

Each event is on its own line.

### 2.1 Event line forms

* `BEGIN_OBJECT`

* `END_OBJECT`

* `BEGIN_ARRAY`

* `END_ARRAY`

* `NAME <slice>`

* `STRING <slice>`

* `NUMBER <slice>`

* `TRUE`

* `FALSE`

* `NULL`

* `COMMENT <slice>`

* `DIRECTIVE <slice>`

* `END_DOCUMENT`

Notes:

* `BEGIN_DOCUMENT` is not required in M1.
* `END_DOCUMENT` MUST appear exactly once for success.

---

## 3. Slice Rendering for Tests

Slices in traces are rendered as:

* `b"..."` where `...` is a UTF-8 byte string escaped for readability

Escapes in the trace output:

* `\\` for backslash
* `\"` for quote
* `\n` for LF
* `\r` for CR
* `\t` for tab

The trace representation is **not** required to preserve original quoting style.
It must preserve the raw bytes of the slice.

If a test requires lexical preservation, use the **Roundtrip** test family (future M2).

---

## 4. Options Encoding

Options for each test are declared in a small header block.

Recommended textual representation:

* `MODE: AJIS | JSON`
* `COMMENTS: on | off`
* `DIRECTIVES: on | off`
* `IDENTIFIERS: on | off`
* `MAX_DEPTH: <n>`
* `MAX_TOKEN_BYTES: <n>`

Defaults (if omitted):

* `MODE: AJIS`
* `COMMENTS: off`
* `DIRECTIVES: off`
* `IDENTIFIERS: off`
* limits are implementation defaults

---

## 5. Expected Failure Encoding

Failures are encoded as:

* `ERROR_CODE: <AjisErrorCode>`
* `ERROR_OFFSET: <byte_offset>`

Optional:

* `ERROR_LINE:` and `ERROR_COLUMN:` may be asserted only when diagnostics are enabled

---

## 6. Test File Layout

Test files SHOULD be stored as:

* `test_data/streamwalk/<family>/<name>.case`

Each `.case` file contains:

1. header (options)
2. input block
3. expected block

---

## 7. Case File Format

A `.case` file uses three sections.

### 7.1 Example template

```
# OPTIONS
MODE: AJIS
COMMENTS: off
DIRECTIVES: off
IDENTIFIERS: off
MAX_DEPTH: 64
MAX_TOKEN_BYTES: 1048576

# INPUT
{ "a": 1, "b": [true, null] }

# EXPECTED
BEGIN_OBJECT
NAME b"a"
NUMBER b"1"
NAME b"b"
BEGIN_ARRAY
TRUE
NULL
END_ARRAY
END_OBJECT
END_DOCUMENT
```

Whitespace rules:

* header keys are case-sensitive as shown
* blank lines are allowed
* input block is raw UTF-8 text until `# EXPECTED`

---

## 8. Canonical Families (M1)

M1 requires at least these families:

* `valid/basic`

  * simple primitives, arrays, objects

* `valid/nesting`

  * deep nesting within limits

* `valid/strings`

  * escapes, unicode

* `valid/numbers`

  * number variations and bases allowed by AJIS options

* `invalid/syntax`

  * missing commas/brackets, bad tokens

* `invalid/strings`

  * bad escapes, unterminated strings

* `invalid/numbers`

  * invalid number forms

* `invalid/limits`

  * depth/token limits

* `compat/json`

  * strict JSON mode acceptance/rejection

---

## 9. Determinism Requirements

A runner MUST:

* emit the same trace for span and stream parsing
* emit the same errors for any stream buffer size

A test suite MUST include at least one run with:

* small buffer (e.g., 16 bytes)
* medium buffer (e.g., 4 KB)
* large buffer (e.g., 64 KB)

---

## 10. Compliance

An implementation is considered **StreamWalk-test compliant (M1)** if:

* all `valid/*` cases pass
* all `invalid/*` cases fail with correct code+offset
* all `compat/json/*` cases follow JSON compatibility rules
* parity requirements are satisfied

---

*End of document.*
