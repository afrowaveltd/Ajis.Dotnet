# AJIS Implementation Roadmap (Incremental)

> Step-by-step implementation plan for AJIS .NET with streaming-first focus

---

## 1. Purpose

This roadmap defines an **incremental build order** that:

* keeps the project always buildable
* grows features in coherent layers
* protects performance work from API churn
* produces usable tools early

---

## 2. Phases overview

1. Core foundations
2. Text lexer + token reader
3. Segment parser (streaming)
4. Segment serializer (reconstruction)
5. Transforms + path evaluation
6. Tools (validate/stats/select/normalize)
7. Patch + merge
8. Mapping (reflection/resolver)
9. Benchmarks + cross-language test_data discipline
10. ATP binary container

---

## 3. Phase 1 – Core foundations

### 3.1 Deliverables

* `AjisSettings` (minimal skeleton + defaults)
* exceptions:

  * `AjisException` (code + position)
  * `AjisFormatException` (parse)
* diagnostics model:

  * `AjisDiagnostic` (code, severity, position, messageKey)
* localization abstraction:

  * `IAjisTextProvider` (messageKey → localized string)
* logger abstraction (optional):

  * `IAjisLogger` (Warning+)
* event stream abstraction:

  * `IAjisEventSink` or channel-based sink

### 3.2 Tests

* settings defaults
* exception formatting
* localization fallback

---

## 4. Phase 2 – Text primitives (UTF-8 + spans)

### 4.1 Deliverables

* `AjisReader` (span/sequence reader utilities)
* whitespace + comments skipping (per AJIS spec)
* string scanning:

  * detect end quote with escapes
  * support UTF-8
* number token scanning:

  * bases (bin/oct/dec/hex)
  * `_` separators rules

### 4.2 Tests

* targeted token tests (tiny inputs)
* escape sequences
* unicode
* number variations (including invalid)

---

## 5. Phase 3 – Segment parser (ParseSegments)

### 5.1 Deliverables

* `ParseSegments(Stream)` → `IAsyncEnumerable<AjisSegment>`
* `ParseSegments(ReadOnlySequence<byte>)` (optional)
* frame stack tracking
* position tracking
* strict error reporting

### 5.2 Tests

* round-trip structure checks
* deep nesting
* invalid structure errors with location

---

## 6. Phase 4 – Segment serializer

### 6.1 Deliverables

* `SerializeSegments(Stream, IAsyncEnumerable<AjisSegment>)`
* pretty/compact formatting
* canonical formatting option
* ignore meta segments
* guarantee valid AJIS output

### 6.2 Tests

* parse → segments → serialize → parse (equivalence)
* canonicalization snapshot tests

---

## 7. Phase 5 – Transforms + paths

### 7.1 Deliverables

* transform primitives:

  * `DropProperty`
  * `RenameKeys`
  * `SelectSubtree`
* path evaluator:

  * minimal JSONPath-like subset ($.a.b[0])
* bounded buffering helper for per-item array filtering

### 7.2 Tests

* recipe tests from Docs/17
* huge array simulation with small item buffer

---

## 8. Phase 6 – Tools v1

### 8.1 Deliverables

* file validate
* stats
* normalize
* select

### 8.2 CLI

* `ajis validate`
* `ajis stats`
* `ajis normalize`
* `ajis select --path`

---

## 9. Phase 7 – Patch + merge

### 9.1 Deliverables

* patch operations:

  * set/replace/remove/insert
* patch document format (AJIS)
* merge (overlay strategies)

### 9.2 Tests

* deterministic output
* atomic write behavior

---

## 10. Phase 8 – Mapping & converters

### 10.1 Deliverables

* reflection mapping cache
* naming policy mapping
* attributes
* converters registry

### 10.2 Optional

* source generator package

---

## 11. Phase 9 – Benchmark discipline

Deliverables:

* standardized `test_data`
* benchmark runner that:

  * creates huge files only if missing
  * runs all modes
  * persists results

---

## 12. Phase 10 – ATP container

Deliverables:

* ATP header reader/writer
* stream handoff text → binary
* checksum/signature hooks

---

## 13. Notes about missing docs (01, 04)

This roadmap assumes those docs will be added:

* 01: repository overview & architecture map
* 04: AJIS text format deep dive

They can be written anytime, but are recommended before Phase 2 implementation.

---

## 14. Status

Stable.

Implementation may rearrange minor tasks,
but phases must preserve the streaming-first backbone.
