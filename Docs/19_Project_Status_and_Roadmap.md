# AJIS .NET – Project Status & Implementation Roadmap

> Working draft for collaborative planning

---

## 1. Current State (Code + Docs)

### 1.1 Implemented or Present

* **Core diagnostics & localization**: `AjisDiagnostic*`, `AjisLoc*`, localization chain and default provider.
* **StreamWalk (M1)**: `AjisStreamWalkRunnerM1` and event model, `AjisStreamWalkRunner` orchestration.
* **Engine selection skeleton (M1.1)**: engine registry, descriptors, cost model, selector.
* **Options/Settings**: `AjisStreamWalkOptions`, `AjisStreamWalkRunnerOptions`, core `AjisSettings` (skeleton).
* **Processing profiles**: `AjisProcessingProfile` with parser/serializer profile hints and StreamWalk engine preference mapping.
* **Streaming segments (M3 initial)**: `AjisParse.ParseSegments` maps StreamWalk events to `AjisSegment` (stream variant buffers input).
* **Stream parsing (M3)**: `ParseSegmentsAsync` uses zero-copy for `MemoryStream`, memory-mapped parsing for `FileStream`, and temp-file memory mapping for other streams (no full in-memory buffer).
* **Large-file limit**: >2GB chunked mapping implemented but not covered by test fixture.
* **Chunk threshold**: `AjisSettings.StreamChunkThreshold` controls when chunked memory-mapped reading is used.
* **Chunked parser**: current chunked path uses `Utf8JsonReader` (decoded strings) until streaming reader is implemented.
* **Reader foundation (M2)**: `IAjisReader` with span/stream implementations and parity tests.
* **Lexer foundation (M2)**: `AjisLexer` tokenizes JSON subset with basic tests.
* **Lexer positions (M2)**: reader/lexer track line/column and validate unicode escapes.
* **Lexer parser (M2)**: `AjisParse` uses lexer-based parser for span inputs.
* **Number validation (M2)**: JSON number rules enforced in lexer tests.
* **AJIS number extensions (M2)**: base prefixes and digit separators covered in lexer tests.
* **Separator grouping (M2)**: lexer enforces grouping rules per format norms.
* **String modes (M2)**: lexer honors Json/Ajis/Lex rules for multiline and escapes.
* **String options (M2)**: `EnableEscapes=false` keeps backslashes as literals in AJIS mode.
* **Single-quote strings (M2)**: lexer supports `AllowSingleQuotes` in AJIS/Lex modes.
* **Unquoted names (M2)**: lexer supports identifier-style property names in AJIS/Lex.
* **Lex unterminated strings (M2)**: lexer returns best-effort tokens for unterminated strings/escapes.
* **Comments (M2)**: lexer skips line/block comments in AJIS/Lex and rejects in JSON mode.
* **Trailing commas (M2)**: lexer parser supports trailing commas via settings or Lex mode.
* **Directives (M2)**: lexer recognizes # directives at line start (AJIS/Lex), JSON rejects.
* **Lexer parser (M2)**: stream parsing in Universal profile now uses lexer-based parser.
* **Large data tests**: `AjisParseLargeDataTests` validates parser profiles on generated payloads.
* **Chunk threshold tests**: `AjisParseLargeDataTests` covers size suffix parsing and invalid thresholds.
* **Chunked escape test**: chunked parsing validates decoded escape sequences in large-data tests.
* **Segment tests**: primitives and container shapes (array/object, nested, multi-property) covered in `AjisParseTests`, with skipped placeholders for future AJIS extensions.
* **Benchmark generator**: `AjisLargePayloadGenerator` with CLI wrapper in benchmarks for large dataset generation.
* **Benchmark runner**: `AjisBenchmarkRunner` compares AJIS profiles vs System.Text.Json and Newtonsoft.Json.
* **Test infrastructure**: `test_data/` contract and unit test scaffolding (core + streaming + serialization + records/net/io stubs).

### 1.2 Present but Skeleton/Not Implemented

* **Streaming segments**: `AjisParse.ParseSegments*` (skeleton only).
* **Serialization**: `AjisSerialize`, `AjisSerializer` API skeletons.
* **Records/IO/Net**: placeholder modules with only stub classes.
* **Mapping/Tools/ATP**: planned in docs, not implemented in current workspace.

### 1.3 Documentation Sources (Normative)

* Architecture: `Docs/01_Repository_Overview_and_Architecture.md`
* Core API: `Docs/07_Core_API.md`
* Streaming & segments: `Docs/09_Streaming.md`, `Docs/16_Pipelines_and_Segments.md`
* Serialization: `Docs/08_Serialization.md`
* Milestones: `Docs/18_Implementation_Roadmap.md`

---

## 2. Modularity Principles (“LEGO” Architecture)

These rules define how components must be attachable/detachable without breaking core behavior.

1. **Stable Core Contracts**
   * Core types (`AjisDiagnostic*`, `AjisSettings`, `AjisSegment`, StreamWalk contracts) remain stable.
   * Higher layers extend by composition, not by altering core semantics.

2. **Pluggable Engines**
   * Engine selection is already abstracted (`IAjisStreamWalkEngineDescriptor`).
   * New engines register via registry; selection logic stays deterministic.
   * No engine-specific types leak into public APIs.

3. **Localization as a Plugin Chain**
   * `AjisLocDictionary` and `AjisTextProviderBuilder` compose multiple sources.
   * Allow external locale packs to be added without changing Core.

4. **Serializer Strategies**
   * Serializer operates on `AjisSegment` streams.
   * Separate strategies for Strict JSON / Canonical / Pretty.
   * Strategies should be swappable without changing input contracts.

5. **Pipeline Compatibility**
   * Segment streams are the common currency across parser, transforms, serializer, tools.
   * Transforms must remain bounded-memory and not require full DOM.

---

## 3. Proposed Implementation Roadmap (Working Draft)

This draft aligns with `Docs/18_Implementation_Roadmap.md` and current code.

### Phase A – Stabilize M1/M1.1

* Ensure StreamWalk event contracts align with `Docs/api/streamwalk.md`.
* Complete diagnostic key coverage in locales and tests.
* Confirm deterministic engine selection and cost scoring.
* Establish processing profile mapping (Universal/LowMemory/HighThroughput) for parser/serializer selection.
* Wire parser profile mapping into StreamWalk entry points.

### Phase B – M2 Text Primitives (Reader/Lexer)

* Implement UTF‑8 reader with line/column tracking.
* Implement string escape validation and numeric parsing per specs.
* Add unit tests for edge cases (unicode, overflow, grouping).

### Phase C – M3 Streaming Segments

* Implement `AjisParse.ParseSegments` and async variant.
* Guarantee segment nesting correctness and metadata emissions.
* Add segment contract tests from `Docs/16_Pipelines_and_Segments.md`.

### Phase D – M4 Serialization

* Implement `AjisSerialize` and `AjisSerializer` over segments.
* Provide Strict JSON + Canonical + Pretty modes.
* Add tests for deterministic output.

### Phase E – M5 LAX Mode

* Implement relaxed parsing (unquoted keys, trailing commas, comments).
* Emit diagnostics for tolerated constructs.

### Phase F – M6 High Throughput Engines

* Add SIMD/Span-based paths and benchmarks.
* Verify parity with `System.Text.Json`.

---

## 4. Open Decisions (To Confirm Together)

* **API surface**: finalize what is Core vs optional packages.
* **Localization pack mechanism**: resource naming and loading strategy for external packs.
* **Parallelization model**: when and where parallel engines can be safely introduced.
* **Tooling surface**: CLI operations that should ship with v1.

---

## 5. Next Step Proposal

If this draft is acceptable, next step is to:

1. Agree on Phase A tasks and acceptance criteria.
2. Create a task list per phase with test-first approach.
3. Keep this document updated after each milestone.
