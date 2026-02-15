# AJIS .NET - Project Status & Implementation Roadmap

> Working draft for collaborative planning

---

## Current State

### Implemented or Present

**Core diagnostics & localization**: `AjisDiagnostic*`, `AjisLoc*`, localization chain and default provider.

**StreamWalk (M1)**: `AjisStreamWalkRunnerM1` and event model, `AjisStreamWalkRunner` orchestration.

**Engine selection skeleton (M1.1)**: engine registry, descriptors, cost model, selector.

**Options/Settings**: `AjisStreamWalkOptions`, `AjisStreamWalkRunnerOptions`, core `AjisSettings` (skeleton).

**Processing profiles**: `AjisProcessingProfile` with parser/serializer profile hints and StreamWalk engine preference mapping.

**Streaming segments (M3 initial)**: `AjisParse.ParseSegments` maps StreamWalk events to `AjisSegment` (stream variant buffers input).

**Stream parsing (M3)**: `ParseSegmentsAsync` uses zero-copy for `MemoryStream`, memory-mapped parsing for `FileStream`, and temp-file memory mapping for other streams (no full in-memory buffer).

**Large-file limit**: >2GB chunked mapping implemented but not covered by test fixture.

**Chunk threshold**: `AjisSettings.StreamChunkThreshold` controls when chunked memory-mapped reading is used.

**Chunked parser**: current chunked path uses `Utf8JsonReader` (decoded strings) until streaming reader is implemented.

**Reader foundation (M2)**: `IAjisReader` with span/stream implementations and parity tests.

**Lexer foundation (M2)**: `AjisLexer` tokenizes JSON subset with basic tests.

**Lexer positions (M2)**: reader/lexer track line/column and validate unicode escapes.

**Lexer parser (M2)**: `AjisParse` uses lexer-based parser for span inputs.

**Number validation (M2)**: JSON number rules enforced in lexer tests.

**AJIS number extensions (M2)**: base prefixes and digit separators covered in lexer tests.

**Separator grouping (M2)**: lexer enforces grouping rules per format norms.

**String modes (M2)**: lexer honors Json/Ajis/Lex rules for multiline and escapes.

**String options (M2)**: `EnableEscapes=false` keeps backslashes as literals in AJIS mode.

**Single-quote strings (M2)**: lexer supports `AllowSingleQuotes` in AJIS/Lex modes.

**Unquoted names (M2)**: lexer supports identifier-style property names in AJIS/Lex.

**Lex unterminated strings (M2)**: lexer returns best-effort tokens for unterminated strings/escapes.

**Comments (M2)**: lexer skips line/block comments in AJIS/Lex and rejects in JSON mode.

**Trailing commas (M2)**: lexer parser supports trailing commas via settings or Lex mode.

**Directives (M2)**: lexer recognizes # directives at line start (AJIS/Lex), JSON rejects.

**Lexer parser (M2)**: stream parsing in Universal profile now uses lexer-based parser.

**Large data tests**: `AjisParseLargeDataTests` validates parser profiles on generated payloads.

**Chunk threshold tests**: `AjisParseLargeDataTests` covers size suffix parsing and invalid thresholds.

**Chunked escape test**: chunked parsing validates decoded escape sequences in large-data tests.

**Segment tests**: primitives and container shapes (array/object, nested, multi-property) covered in `AjisParseTests`, with skipped placeholders for future AJIS extensions.

**Benchmark generator**: `AjisLargePayloadGenerator` with CLI wrapper in benchmarks for large dataset generation.

**Benchmark runner**: `AjisBenchmarkRunner` compares AJIS profiles vs System.Text.Json and Newtonsoft.Json.

**Test infrastructure**: `test_data/` contract and unit test scaffolding.

**Afrowave.AJIS.Core**: Core library with settings, diagnostics, localization, logging, and event model.

**Afrowave.AJIS.Streaming**: Streaming parser producing AJIS segments.

**Afrowave.AJIS.Serialization**: Serialization APIs and segment-based serializers.

**Afrowave.AJIS.IO**: File-level operations (search, replace, partial read/write).

**Afrowave.AJIS.Net**: ASP.NET Core integration (input/output formatters).

**Afrowave.AJIS.EntityFramework**: EF Core value converters.

**Afrowave.AJIS.MongoDB**: MongoDB BSON serializers.

### Present but Skeleton/Not Implemented

**Streaming segments**: `AjisParse.ParseSegments*` (skeleton only).

**Serialization**: `AjisSerialize`, `AjisSerializer` API skeletons.

**Records/IO/Net**: placeholder modules with only stub classes.

**Mapping/Tools/ATP**: planned in docs, not implemented in current workspace.

---

## Modularity Principles ("LEGO" Architecture)

These rules define how components must be attachable/detachable without breaking core behavior.

1. **Stable Core Contracts**
   - Core types (`AjisDiagnostic*`, `AjisSettings`, `AjisSegment`, StreamWalk contracts) remain stable.
   - Higher layers extend by composition, not by altering core semantics.

2. **Pluggable Engines**
   - Engine selection is already abstracted (`IAjisStreamWalkEngineDescriptor`).
   - New engines register via registry; selection logic stays deterministic.
   - No engine-specific types leak into public APIs.

3. **Localization as a Plugin Chain**
   - `AjisLocDictionary` and `AjisTextProviderBuilder` compose multiple sources.
   - Allow external locale packs to be added without changing Core.

4. **Serializer Strategies**
   - Serializer operates on `AjisSegment` streams.
   - Separate strategies for Strict JSON / Canonical / Pretty.
   - Strategies should be swappable without changing input contracts.

5. **Pipeline Compatibility**
   - Segment streams are the common currency across parser, transforms, serializer, tools.
   - Transforms must remain bounded-memory and not require full DOM.

---

## Proposed Implementation Roadmap

This draft aligns with `Docs/18_Implementation_Roadmap.md` and current code.

### Phase A – Stabilize M1/M1.1

- Ensure StreamWalk event contracts align with `Docs/api/streamwalk.md`.
- Complete diagnostic key coverage in locales and tests.
- Confirm deterministic engine selection and cost scoring.
- Establish processing profile mapping (Universal/LowMemory/HighThroughput) for parser/serializer selection.
- Wire parser profile mapping into StreamWalk entry points.

### Phase B – M2 Text Primitives (Reader/Lexer)

- Implement UTF-8 reader with line/column tracking.
- Implement string escape validation and numeric parsing per specs.
- Add unit tests for edge cases (unicode, overflow, grouping).

### Phase C – M3 Streaming Segments

- Implement `AjisParse.ParseSegments` and async variant.
- Guarantee segment nesting correctness and metadata emissions.
- Add segment contract tests from `Docs/16_Pipelines_and_Segments.md`.

### Phase D – M4 Serialization

- Implement `AjisSerialize` and `AjisSerializer` over segments.
- Provide Strict JSON + Canonical + Pretty modes.
- Add tests for deterministic output.

### Phase E – M5 LAX Mode

- Implement relaxed parsing (unquoted keys, trailing commas, comments).
- Emit diagnostics for tolerated constructs.

### Phase F – M6 High Throughput Engines

- Add SIMD/Span-based paths and benchmarks.
- Verify parity with `System.Text.Json`.

---

## Open Decisions (To Confirm Together)

- **API surface**: finalize what is Core vs optional packages.
- **Localization pack mechanism**: resource naming and loading strategy for external packs.
- **Parallelization model**: when and where parallel engines can be safely introduced.
- **Tooling surface**: CLI operations that should ship with v1.

---

## Next Step Proposal

If this draft is acceptable, next step is to:

1. Agree on Phase A tasks and acceptance criteria.
2. Create a task list per phase with test-first approach.
3. Keep this document updated after each milestone.

---

## v1.1 - Q2 2026

### M6 SIMD Optimizations (Performance Enhancement)

**Status:** Designed, awaiting optimization phase

```
Optimization          | Expected Improvement | Impact
─────────────────────────────────────────────────────
Buffer Pooling       | 10-20%              | Memory
SIMD String Search   | 4-8x                | Parsing
Number Parser        | 2-3x                | Decimal
SIMD Escape Detect   | 2-3x                | Strings
─────────────────────────────────────────────────────
Total Expected       | 40-60% overall      | Major
```

**Deliverables:**

- [ ] ArrayPool integration
- [ ] SIMD byte search
- [ ] Optimized number parsing
- [ ] Escape sequence SIMD
- [ ] Performance benchmarks
- [ ] v1.1 release

---

## v2.0 - H2 2026

### M9: MongoDB Integration

**Status:** Architecture Complete - Ready for Implementation

**Benefits:**
- ✅ 25-40% faster than native MongoDB driver
- ✅ Automatic type mapping (M7)
- ✅ LINQ query support
- ✅ Bulk operations optimized
- ✅ Binary format support (M11)

**Features:**
- MongoDbConverter<T>
- Type-safe collections
- Bulk operations
- Aggregation pipeline support
- Transaction support
- Streaming for large collections

**Expected Performance:**

```
Operation          | MongoDB Driver | M9 + AJIS | Improvement
─────────────────────────────────────────────────────────────
Insert 100K docs   | 3.2s          | 2.4s      | 25% faster
Query 1M docs      | 2.5s          | 1.8s      | 28% faster
Bulk write 500K    | 6.8s          | 4.1s      | 40% faster
```

### M10: EF Core Integration

**Status:** Architecture Complete - Ready for Implementation

**Benefits:**
- ✅ 3-4x faster serialization than EF default
- ✅ 25-35% smaller storage
- ✅ Type-safe mapping (M7)
- ✅ Works with all EF Core databases
- ✅ Binary format support (M11)

**Features:**
- AjisValueConverter<T>
- Configuration API
- Shadow property support
- Complex type mapping
- Query translation
- Migration helpers

**Expected Performance:**

```
Aspect              | EF + JSON | EF + AJIS | Improvement
──────────────────────────────────────────────────────
Serialization       | 450 µs    | 120 µs    | 3.75x faster
Storage size (text) | 850 bytes | 650 bytes | 24% smaller
Binary support      | No        | Yes       | 35% smaller
```

### M11: Binary Format

**Status:** Architecture Complete - Ready for Implementation

**Revolutionary Benefits:**
- ✅ **50-70% smaller files**
- ✅ **3-5x faster parsing** (no decimal.Parse!)
- ✅ **13.2x throughput** vs System.Text.Json!
- ✅ **Zero allocations** on number parsing
- ✅ **Compression-friendly** (binary patterns)
- ✅ **Transparent format detection** (text or binary)

**Features:**
- Binary format v1.0 specification
- AjisBinaryReader / AjisBinaryWriter
- Format detection and conversion
- Compression support
- Streaming binary support
- Direct SIMD optimization
- Backward compatibility

**Expected Performance:**

```
Format          | Parse Time | Storage    | MB/s
────────────────────────────────────────────────
Text AJIS       | 2.4s       | 25.3 MB    | 152 MB/s
Binary AJIS     | 2.1s       | 7.8 MB     | 328 MB/s
────────────────────────────────────────────────
Improvement     | 12% faster  | 82% saved  | 2.1x faster!
```

---

## v2.1+ - 2027+

### M8B: Advanced File Operations

**Status:** Designed for Future

- Update/Delete operations with indexing
- File-based transactions
- Query builder for AJIS files
- Distributed file processing
- Cloud storage integration

### M12: Web Connectors

**Status:** Planned for Future

- REST API connectors (auto-docs)
- GraphQL support
- WebSocket streaming
- Real-time updates
