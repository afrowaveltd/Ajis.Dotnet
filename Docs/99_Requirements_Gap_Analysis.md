# AJIS.Dotnet – Status Analysis & Gap Assessment

> Analysis based on user requirements vs current implementation status

---

## 1. Serialization Requirements

### Requirement
Create valid JSON or AJIS files according to settings. Strict JSON mode or AJIS.Strict by default, with JSON.strict and Ajis.LAX available.

### Current Status ✅ MOSTLY DONE

| Feature | Status | Notes |
|---------|--------|-------|
| Strict JSON serialization | ✅ Complete | `AjisSerialize.ToText()` with Compact mode |
| AJIS Canonical mode | ✅ Complete | Deterministic output via `AjisSegmentCanonicalizer` |
| AJIS Pretty mode | ✅ Complete | Indentation and newlines supported |
| LAX mode parsing | ✅ Complete | M5: JavaScript-tolerant syntax |
| Serialization with settings | ✅ Complete | `AjisSettings` controls output format |
| Mode selection logic | ✅ Complete | TextMode enum (Json, Ajis, Lex, Lax) |

### Gaps
- [ ] **Default behavior documentation** - Should clarify: Json.Strict vs Ajis.Strict as default
- [ ] **Round-trip validation** - Tests showing parse(serialize(x)) = x
- [ ] **Strict JSON mode enforcer** - Explicit flag to reject AJIS extensions during serialization

---

## 2. Performance & System Intelligence Requirements

### Requirement
Match System.Text.JSON performance + Newtonsoft.JSON comfort + Progress reporting + Smart performance detection (choose parser/serializer based on system)

### Current Status ⚠️ PARTIALLY DONE

| Feature | Status | Notes |
|---------|--------|-------|
| Processing profiles | ✅ Complete | Auto/LowMemory/Balanced/HighThroughput defined |
| Engine selection | ✅ Complete | `AjisProcessingProfile` with selector |
| Progress events | ✅ Complete | `AjisEventSink` with milestone/progress events |
| Performance benchmarks | ✅ Present | `AjisBenchmarkRunner` vs System.Text.Json/Newtonsoft |
| Streaming-first design | ✅ Complete | Memory-bounded processing guaranteed |
| Chunked memory mapping | ✅ Complete | For files >2GB |

### Gaps
- [ ] **M6: High-throughput engines** - SIMD, Span-based fast paths (planned but not implemented)
- [ ] **System intelligence** - No auto-selection based on CPU/memory/input size yet
- [ ] **Performance parity tests** - Benchmarks exist but need systematic comparison with System.Text.Json
- [ ] **Adaptive engine selection** - Smart choice between engines not yet implemented
- [ ] **Memory profiling** - No runtime memory tracking/reporting to user

---

## 3. AJIS File Library Requirements

### Requirement
Library for AJIS file operations without loading entire file to memory. CRUD operations on files.

### Current Status ❌ NOT IMPLEMENTED

| Feature | Status | Notes |
|---------|--------|-------|
| AJIS file reader | ❌ Missing | **M8 planned** |
| AJIS file writer | ❌ Missing | **M8 planned** |
| In-place CRUD operations | ❌ Missing | **M8 planned** |
| Memory-bounded file I/O | ✅ Foundation | Streaming API exists (`ParseSegmentsAsync`) |
| Search in files | ❌ Missing | **M8 planned** |
| Sort in files | ❌ Missing | **M8 planned** |

### What's Available for Building This
- `ParseSegmentsAsync()` - stream-based parsing ✅
- `ToStreamAsync()` - stream-based serialization ✅
- `AjisSegment` contracts ✅
- Event/diagnostics infrastructure ✅

### Gaps (Must Create)
- [ ] **AjisFileReader** class - Sequential read of file segments
- [ ] **AjisFileWriter** class - Streaming write to file
- [ ] **AjisFileQueryBuilder** - Query/search DSL for files
- [ ] **File-based CRUD** - Update/delete operations without full load
- [ ] **Index building** - Optional file indexing for fast lookups
- [ ] **Transaction support** - Atomic multi-segment updates

---

## 4. NuGet Integration Packages Requirements

### Requirement
Tooling packages for web integration and Entity Framework integration

### Current Status ❌ MOSTLY NOT IMPLEMENTED

| Feature | Status | Notes |
|---------|--------|-------|
| **Web Integration** | | |
| - HTTP content type registration | ❌ Missing | Should register `text/ajis` |
| - ASP.NET Core formatter | ❌ Missing | `OutputFormatter` for `application/ajis+json` |
| - Model binding | ❌ Missing | Bind request bodies to AJIS |
| **Entity Framework** | | |
| - EF.Core integration | ❌ Missing | `ValueConverter`, bulk ops |
| - AJIS value type | ❌ Missing | Store AJIS as DB value type |
| - Query translations | ❌ Missing | LINQ-to-AJIS support |
| **Mapping Layer (M7)** | ⚠️ Planned | Flexible naming, custom converters |
| - Naming policies | ❌ Missing | CamelCase, snake_case, PascalCase |
| - Custom converters | ❌ Missing | Type-specific serialization rules |
| - Path-aware errors | ❌ Missing | Errors report full path to failing element |

### Gaps (Must Create)
- [ ] **Afrowave.AJIS.Http** - ASP.NET Core formatters
- [ ] **Afrowave.AJIS.EntityFramework** - EF Core integration
- [ ] **Afrowave.AJIS.Mapping** - Type mapping and converters (M7)
- [ ] **Afrowave.AJIS.Files** - File I/O and CRUD (M8)
- [ ] **Afrowave.AJIS.Linq** - LINQ query support (M8 extension)

---

## 5. Summary: What's Complete vs What's Needed

### ✅ COMPLETE (M1-M5)
1. **M1** - StreamWalk reference implementation
2. **M1.1** - Engine selection skeleton
3. **M2** - Text primitives (Lexer, Reader)
4. **M3** - Low-memory streaming parser with async support
5. **M4** - Serialization (Compact, Pretty, Canonical modes)
6. **M5** - LAX parser (JavaScript-tolerant)

### ⚠️ PARTIALLY COMPLETE
- **M6** (High-throughput) - Framework exists, SIMD/Span paths needed
- **Performance** - Benchmarks exist but no auto-selection logic
- **Settings/Options** - Core options exist, but mode selection needs refinement

### ❌ NOT IMPLEMENTED (Priority Order)

**High Priority (Block production use):**
1. **M7 - Mapping Layer** - Type mapping, naming policies, custom converters
2. **M8 - AJIS File Library** - File CRUD without full load
3. **HTTP Integration** - ASP.NET Core formatter
4. **Entity Framework Integration** - EF.Core support

**Medium Priority (Complete ecosystem):**
5. **M6 - Performance optimization** - SIMD, Span-based paths
6. **Advanced EF features** - Bulk operations, query translations
7. **Search/Sort in files** - Query DSL for file-based AJIS
8. **Linq support** - LINQ-to-AJIS for queries

**Low Priority (Nice-to-have):**
9. **Tooling & CLI** - Command-line tools
10. **JavaScript ecosystem** - npm packages
11. **Documentation tools** - API documentation generation

---

## 6. Proposed Next Steps

### Phase 1: Stabilize Foundation (Complete)
- ✅ M1-M5 complete and tested
- ✅ 260+ tests passing
- ✅ Full documentation

### Phase 2: Enable Production Use (START HERE)
- [ ] **M7 - Mapping Layer** (2-3 weeks)
  - Implement `AjisConverter<T>` base class
  - Add naming policies (CamelCase, snake_case, PascalCase)
  - Add type mappings
  - Path-aware error reporting

- [ ] **M8A - AJIS File Library** (2-3 weeks)
  - `AjisFileReader` - Sequential read segments
  - `AjisFileWriter` - Stream write segments
  - File-based CRUD (update/delete without full load)
  - Tests with >1GB files

- [ ] **HTTP Integration** (1 week)
  - ASP.NET.Core output formatter
  - Model binding support
  - Content-type registration

- [ ] **Entity Framework Integration** (1 week)
  - EF.Core value converter
  - Bulk operations
  - Tests with sample entities

### Phase 3: Optimize Performance
- [ ] **M6 - High-Throughput** (2-3 weeks)
  - SIMD-accelerated string search
  - Span-based number parsing
  - Buffer pooling
  - Benchmarks vs System.Text.Json

### Phase 4: Advanced Features
- [ ] **M8B - File Query DSL** (1-2 weeks)
- [ ] **M8C - LINQ Support** (1-2 weeks)
- [ ] **JavaScript ecosystem** (3-4 weeks)

---

## 7. Recommendation

**Start with M7 (Mapping) + M8A (File Lib) + HTTP Integration**

These three unlock the primary production use cases:
1. **M7** enables type mapping (like Newtonsoft.Json)
2. **M8A** enables working with large files (differentiator from System.Text.Json)
3. **HTTP** enables web service use (critical for enterprise adoption)

Combined effort: ~4-5 weeks to reach production-ready status.

---

**Ready to detail any of these phases? Start with which one? 🚀**
