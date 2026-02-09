# M3 – Low-Memory Streaming Parser Implementation Status

> **Status:** IN PROGRESS → COMPLETION
>
> This document tracks the M3 (Low-Memory Streaming Parser) milestone and defines the completion strategy.

---

## 1. M3 Scope

M3 implements a streaming segment-based parser that can process arbitrarily large AJIS/JSON documents with guaranteed bounded memory usage.

**Core responsibilities:**

* Token-to-segment translation
* Single-pass streaming emission
* Bounded memory regardless of document size
* Proper nesting and ordering rules
* Exact position tracking for diagnostics
* Protection against malformed/hostile input

---

## 2. Design Principles

### 2.1 Streaming-First
- Values must be emitted as soon as they are complete
- No full-document materialization
- Enables processing of multi-gigabyte files

### 2.2 Bounded Memory
- Stack depth limits
- Token size limits (inherited from M2)
- Configurable memory protections
- Streaming readers with bounded buffers

### 2.3 Deterministic Ordering
- Segments appear in parse order
- Nesting properly maintained
- Position information precise

### 2.4 Self-Describing Output
- Segments contain enough information to reconstruct valid AJIS
- Comments/directives preserved when enabled
- Whitespace not needed for reconstruction

---

## 3. Current Implementation Status

### 3.1 Existing Components

#### AjisLexerParser (in-memory variant)
- [x] Token-to-segment conversion
- [x] Object/Array nesting
- [x] Property name tracking
- [x] Value emission
- [x] Comment/directive handling
- [x] Returns IReadOnlyList<AjisSegment>

**Limitation:** Materializes entire segment list in memory

---

#### AjisLexerParserStream
- [x] Wrapper for stream-based input
- [x] Uses configurable buffer size
- [x] Currently returns IReadOnlyList (materializes)

**Limitation:** Still materializes all segments before returning

---

### 3.2 Segment Types

**Implemented Variants:**

```csharp
AjisSegmentKind:
  - EnterContainer    (start of { or [)
  - ExitContainer     (end of } or ])
  - PropertyName      (object key)
  - Value             (primitive or container value)
  - Comment           (optional)
  - Directive         (optional)
```

**Segment Fields:**
- Kind: segment type
- Position: byte offset in input
- Depth: nesting depth (0 = root)
- ContainerKind: Object/Array (for Enter/Exit)
- ValueKind: Null/Boolean/Number/String (for Value)
- Slice: UTF-8 payload

---

### 3.3 Nesting Rules

**Current State:** Properly implemented in AjisLexerParser

- [x] Open containers (Enter) match closes (Exit)
- [x] Depth tracked correctly
- [x] Property names precede values in objects
- [x] Values appear in sequence

---

## 4. M3 Completeness Checklist

### 4.1 Functional Requirements

- [ ] ParseSegmentsStreamAsync emits segments one-at-a-time (not materialized)
- [ ] Memory usage bounded regardless of document size
- [ ] Stack depth validated and limited
- [ ] Large nested structures handled safely
- [ ] Streaming semantics preserved
- [ ] Position tracking accurate throughout

### 4.2 Testing Requirements

- [ ] Segment emission order tests (nesting rules)
- [ ] Stream memory safety tests
- [ ] Large file handling tests (>100MB virtual test)
- [ ] Segment completeness tests (can reconstruct AJIS)
- [ ] Edge cases: deep nesting, long arrays, large strings

### 4.3 Documentation Requirements

- [ ] M3 specification complete
- [ ] Streaming implementation documented
- [ ] Memory characteristics documented
- [ ] Test coverage recorded

---

## 5. Test Matrix

### 5.1 Segment Emission Order

**Tests Needed:**

- Simple object: `{ "a": 1 }` produces correct segment sequence
- Simple array: `[ true, false ]` produces correct segment sequence
- Nested object: properties and nesting depth tracked
- Nested array: array items at correct depth
- Mixed nesting: objects in arrays and vice versa
- Empty containers: `{}` and `[]` produce only Enter/Exit
- Property names appear before values
- Array values appear in order

### 5.2 Nesting Rules

**Tests Needed:**

- Maximum depth enforcement (configurable limit)
- Depth counter accuracy
- Proper matching of Enter/Exit pairs
- Invalid nesting rejected

### 5.3 Memory Safety

**Tests Needed:**

- Process large flat array (test vector allocation)
- Process deeply nested structure (test stack)
- Process large strings (test string buffer)
- Process high-complexity document (combinatorics)
- Memory usage stays bounded

### 5.4 Streaming Behavior

**Tests Needed:**

- Segments emitted eagerly (not batched)
- IAsyncEnumerable properly implements streaming
- Cancellation respected
- Events emitted during parse

### 5.5 Segment Reconstruction

**Tests Needed:**

- Segment stream can reconstruct original document
- Serializer produces valid AJIS from segments
- All value types present
- All structure preserved

---

## 6. Implementation Strategy

### 6.1 Phase 1: Async Streaming API
Convert `ParseSegmentsStreamAsync` to emit segments via `IAsyncEnumerable<AjisSegment>` instead of materializing.

**Target:** One-at-a-time emission with bounded buffer.

### 6.2 Phase 2: Memory Bounds
Add configurable protections:
- Max stack depth (default: 128)
- Max array length (default: 1M items)
- Max string length (inherited from M2)
- Monitor and reject oversized inputs

### 6.3 Phase 3: Testing & Validation
- Comprehensive test suite
- Memory profiling
- Large file stress tests
- Document completeness

---

## 7. Key Metrics

### Memory Characteristics

**Target (Post-M3):**
- O(max_depth) stack usage
- O(max_token_size) token buffer
- O(1) segment emission (streaming)
- Total: independent of document size

**Current (Pre-M3):**
- O(document_size) list materialization
- Must complete parse before yielding

---

## 8. References

* [16_Pipelines_and_Segments.md](./16_Pipelines_and_Segments.md) – Segment specification
* [18_Implementation_Roadmap.md](./18_Implementation_Roadmap.md) – M3 definition
* `src/Afrowave.AJIS.Streaming/Segments/` – Implementation
* `src/Afrowave.AJIS.Streaming/Reader/AjisLexerParser.cs` – Current segment emitter

---

## 9. Status

**M3 Status:** IN PROGRESS

- [x] Specification drafted
- [ ] Current implementation analyzed
- [ ] Tests designed
- [ ] Async streaming implemented
- [ ] Memory bounds enforced
- [ ] All tests passing
- [ ] Documentation complete

---

**Next Step:** Move to Step 2 - Analyze current implementation in detail.
