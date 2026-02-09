# M4 – Serialization Implementation Status

> **Status:** IN PROGRESS → COMPLETION
>
> This document tracks the M4 (Serialization) milestone for converting AJIS segments back to text format.

---

## 1. M4 Scope

M4 implements serialization engines that convert `AjisSegment` streams back into valid AJIS text with configurable formatting.

**Core responsibilities:**

* Segment-to-text conversion
* Multiple serialization modes (compact, pretty, canonical)
* Indentation and whitespace control
* Property/value separator handling
* Deterministic output for hashing
* Streaming output support
* Precise positioning and formatting

---

## 2. Serialization Modes

### 2.1 Compact Mode
- **Purpose:** Minimal size, no unnecessary whitespace
- **Use Case:** Wire transmission, storage optimization
- **Characteristics:**
  - No spaces after colons or commas
  - No newlines
  - Minimal output size
  - Single-line output

### 2.2 Pretty Mode
- **Purpose:** Human-readable with indentation
- **Use Case:** Debugging, log output, configuration files
- **Characteristics:**
  - Newlines between members
  - Indentation (configurable 2-4 spaces)
  - Spaces after colons/commas
  - Multi-line output

### 2.3 Canonical Mode
- **Purpose:** Deterministic output for hashing/diffing/caching
- **Use Case:** Content addressing, consistency verification
- **Characteristics:**
  - Properties sorted by key
  - Array elements in order
  - Consistent spacing
  - No floating-point rounding
  - Lexicographically ordered

---

## 3. Current Implementation Status

### 3.1 Completed Components

#### AjisSegmentTextWriter (Sync)
- [x] Basic segment emission
- [x] Container tracking (stack-based)
- [x] Property name formatting
- [x] Value serialization
- [x] Compact mode support
- [x] Pretty mode support
- [x] Spacing/indentation control

**Limitations:**
- [ ] Missing comprehensive XML documentation
- [ ] Limited formatting option validation
- [ ] No async streaming variant

---

#### AjisValueTextWriter
- [x] Serializes AjisValue to text
- [x] Quote/escape handling
- [x] Type-based dispatch (null, bool, number, string)

---

#### AjisSerializationFormattingOptions
- [x] Defines formatting parameters
- [x] Compact, Pretty, Canonical flags
- [x] IndentSize configuration

---

### 3.2 Test Coverage

**Existing Tests:** 25 serialization tests (all passing)

**Coverage Areas:**
- [x] Basic type serialization (null, bool, number, string)
- [x] Object serialization
- [x] Array serialization
- [x] Nested structures
- [x] Unicode handling
- [x] Number format preservation (hex, binary, octal)
- [x] Escape sequence handling

**Gaps:**
- [ ] Comprehensive pretty/compact mode validation
- [ ] Canonical mode determinism tests
- [ ] Round-trip correctness tests (parse → serialize → parse)
- [ ] Large document serialization tests
- [ ] Indentation edge cases
- [ ] Error handling and validation

---

## 4. M4 Completeness Checklist

### 4.1 Functional Requirements

- [ ] All serialization modes (compact, pretty, canonical) documented
- [ ] Formatting options validated
- [ ] Round-trip correctness verified
- [ ] Streaming async serializer implemented (optional)
- [ ] Quote/escape sequences correct for all value types
- [ ] Number formatting preserves intent (hex/binary/octal flags)
- [ ] Indentation properly handles nested structures
- [ ] Comments/directives handled correctly (skipped or preserved)

### 4.2 Testing Requirements

- [ ] Serialization mode tests (compact, pretty, canonical)
- [ ] Round-trip tests (parse → serialize → parse equals original)
- [ ] Edge case tests (deeply nested, long arrays, large strings)
- [ ] Formatting validation tests
- [ ] Unicode and escape sequence tests
- [ ] Number format preservation tests
- [ ] All tests passing with no regressions

### 4.3 Documentation Requirements

- [ ] M4 specification complete
- [ ] Full XML documentation on public/internal members
- [ ] Formatting mode documentation
- [ ] Segment conversion rules documented
- [ ] Test coverage recorded

---

## 5. Test Matrix

### 5.1 Serialization Mode Tests

**Compact Mode:**
- [ ] Object with minimal spacing
- [ ] Array with no newlines
- [ ] Nested structures compacted
- [ ] Output size is minimal

**Pretty Mode:**
- [ ] Object with indented properties
- [ ] Array with items on separate lines
- [ ] Nested structures properly indented
- [ ] Configurable indent size (2, 3, 4 spaces)

**Canonical Mode:**
- [ ] Properties sorted alphabetically by key
- [ ] Consistent spacing
- [ ] Deterministic output (same input → same output)
- [ ] Can be hashed for caching

### 5.2 Round-Trip Tests

**Sequence:** Parse → Serialize → Parse → Compare

**Test Cases:**
- [ ] Simple objects
- [ ] Complex nested structures
- [ ] Arrays with mixed types
- [ ] Unicode strings
- [ ] Numeric edge cases
- [ ] Empty containers
- [ ] Properties with special characters

### 5.3 Edge Cases

**Coverage:**
- [ ] Deeply nested structures (test max depth handling)
- [ ] Very long strings
- [ ] Very large arrays
- [ ] Mixed nesting patterns
- [ ] Properties requiring escaping
- [ ] Numbers in various bases

---

## 6. Implementation Notes

### 6.1 Key Design Patterns

1. **Stack-based Container Tracking**
   - Maintain stack of container contexts
   - Track whether container has values (for comma/newline placement)
   - Clean nesting validation

2. **StringBuilder Buffering**
   - Accumulate output in memory
   - Return as string or flush to stream
   - Efficient concatenation

3. **Formatting via Options**
   - Pass formatting options to writer
   - Control spacing/newlines via flags
   - Configurable indent size

### 6.2 Critical Implementation Details

**Property Name Handling:**
- Must always be quoted (even identifiers)
- Followed by colon
- Preceded by comma (except first property)

**Value Serialization:**
- Primitives: rendered directly
- Containers: recursively delegated
- Comments/directives: skipped

**Spacing Rules:**
- Compact: no spaces
- Pretty: newlines + indentation
- Canonical: consistent, deterministic

---

## 7. References

* [16_Pipelines_and_Segments.md](./16_Pipelines_and_Segments.md) – Segment specification
* [18_Implementation_Roadmap.md](./18_Implementation_Roadmap.md) – M4 definition
* `src/Afrowave.AJIS.Serialization/` – Implementation
* `tests/Afrowave.AJIS.Serialization.Tests/` – Test suite

---

## 8. Status

**M4 Status:** IN PROGRESS

- [x] Specification drafted
- [ ] Current implementation analyzed
- [ ] XML documentation added
- [ ] Comprehensive tests added
- [ ] Async serializer (if needed)
- [ ] All tests passing
- [ ] Documentation complete

---

**Next Step:** Move to Step 2 - Analyze current implementation in detail.
