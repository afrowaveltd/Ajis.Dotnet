# AJIS Options (M1)

## Status

**M1 – Locked contract**

This document defines the canonical options surface for AJIS parsing and StreamWalk.
Options control limits, feature flags, and compatibility modes.

---

## 1. Purpose

AJIS implementations must be configurable without changing semantics.
Options provide:

* safety limits
* performance tuning
* feature enablement (AJIS extensions)
* JSON compatibility behavior

Options MUST be deterministic: same input + same options => same event stream and results.

---

## 9. .NET configuration mapping

In the .NET reference implementation, `Afrowave.AJIS.Core.Configuration.AjisSettings`
provides a higher-level configuration surface. `AjisStreamWalkOptions.FromSettings`
maps these settings to StreamWalk defaults by applying:

* `TextMode` to StreamWalk mode
* `MaxDepth` and `MaxTokenBytes`
* `AllowDirectives` and comment options
* `AllowUnquotedPropertyNames` for identifier support

---

## 2. Option Profiles

Implementations SHOULD expose common profiles:

* `Default`

  * AJIS-first behavior
  * safe limits suitable for general use

* `StrictJson`

  * accepts only JSON-valid input
  * rejects AJIS-only extensions

* `Lenient`

  * accepts a broader range of AJIS extensions
  * still enforces safety limits

Profiles are convenience presets. The canonical contract is the individual options.

---

## 3. Safety Limits

### 3.1 MaxDepth

Maximum nesting depth for objects/arrays.

* Type: integer
* Default (recommended): 256
* If exceeded: deterministic error

### 3.2 MaxTokenBytes

Maximum size of a single token (string, number, literal, comment, directive).

* Type: integer
* Default (recommended): 8 MiB
* Required for stream reader boundary assembly
* If exceeded: deterministic error

### 3.3 MaxDocumentBytes (optional)

Maximum total bytes read.

* Type: integer (or null/disabled)
* Default: disabled
* If exceeded: deterministic error

### 3.4 MaxPropertyNameBytes (optional)

Maximum size for property name tokens.

* Type: integer (or null/disabled)
* Default: disabled

### 3.5 MaxStringBytes (optional)

Maximum size for string tokens.

* Type: integer (or null/disabled)
* Default: disabled

---

## 4. Feature Flags

### 4.1 AllowComments

If enabled, comments are accepted and emitted via visitor (`OnComment`).

* Default (recommended): true for AJIS Default
* In `StrictJson`: false

If disabled:

* comments MUST be rejected in strict mode, or
* MUST be skipped entirely in lenient mode

Implementations MUST document whether "disabled" means "reject" or "skip".
Recommended:

* Default: skip
* StrictJson: reject

### 4.2 AllowDirectives

If enabled, directives are accepted and emitted via visitor (`OnDirective`).

* Default (recommended): true for AJIS Default
* In `StrictJson`: false

Same reject/skip policy as comments.

### 4.3 AllowTrailingCommas

Allows trailing commas in arrays/objects (AJIS extension).

* Default: true for AJIS Default
* In `StrictJson`: false

### 4.4 AllowSingleQuotes

Allows strings delimited by single quotes (AJIS extension).

* Default: false (recommended)
* In `StrictJson`: false

### 4.5 AllowUnquotedPropertyNames

Allows identifier-style property names (AJIS extension).

* Default: true for AJIS Default
* In `StrictJson`: false

If enabled:

* visitor must receive name slices with `IsIdentifierStyle` flag where applicable.
* segment consumers should read `AjisSliceFlags.IsIdentifierStyle` for name segments.

### 4.6 AllowNumberBases

Allows non-decimal number forms (AJIS extension):

* hex, binary, octal

* Default: true for AJIS Default

* In `StrictJson`: false

If enabled:

* number slices MUST include full token spelling
* base flags MUST be set (`IsNumberHex`, etc.) and segment consumers should read `AjisSliceFlags`

### 4.7 AllowLeadingPlusOnNumbers

Allows `+123` numeric tokens (AJIS extension).

* Default: false (recommended)
* In `StrictJson`: false

### 4.8 AllowNaNAndInfinity

Allows `NaN`, `Infinity`, `-Infinity` (non-JSON extension).

* Default: false (recommended)
* In `StrictJson`: false

---

## 5. Compatibility and Mode

### 5.1 JsonCompatibility

When enabled, the parser MUST enforce JSON validity.

In JsonCompatibility mode:

* comments/directives MUST NOT be accepted
* trailing commas MUST NOT be accepted
* unquoted property names MUST NOT be accepted
* non-decimal number bases MUST NOT be accepted
* single quotes MUST NOT be accepted

Note:

* JSON validity here refers to syntax. Semantic constraints (duplicate keys) are not required.

### 5.2 AjisCompatibility (default)

When JsonCompatibility is disabled, AJIS extensions may be enabled individually.

Default behavior for the project:

* AJIS is the primary format
* JSON is accepted as a subset

---

## 6. Streaming Options

### 6.1 BufferSize

Internal buffer size for Stream Reader.

* Type: integer
* Default (recommended): 64 KiB

Must not affect semantics.

### 6.2 CompactThreshold

Threshold at which unread bytes should be compacted.

* Type: integer
* Default (recommended): BufferSize / 2

Must not affect semantics.

### 6.3 EnableScratchTokenBuffer

Allows assembling boundary-spanning tokens.

* Type: bool
* Default: true

If false:

* stream reader must fail deterministically on boundary-spanning tokens
* (not recommended)

---

## 7. Diagnostics Options

### 7.1 CaptureLineAndColumn (optional)

If enabled, reader tracks line/column for errors.

* Default: false

This MUST NOT affect event ordering or slice bytes.

### 7.2 IncludeTokenPreviewInErrors (optional)

If enabled, errors may contain a small preview snippet.

* Default: true
* Preview MUST be bounded by a max size

---

## 8. Determinism Rules

Options MUST NOT introduce nondeterminism.
In particular:

* buffer sizes must not change token slices or event ordering
* diagnostics must not change parsing results

---

## 9. Compliance Checklist

A compliant implementation MUST:

* expose MaxDepth and MaxTokenBytes
* provide a JSON compatibility mode
* ensure all options preserve determinism
* ensure buffer tuning options never change semantics

---

*End of document.*
