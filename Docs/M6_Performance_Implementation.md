# M6 – Performance Optimization Implementation

> **Status:** IN PROGRESS → COMPLETION
>
> This document defines the M6 (High-Throughput) milestone for achieving System.Text.Json parity on performance.

---

## 1. M6 Scope

M6 optimizes AJIS parsing and serialization to match or exceed System.Text.Json performance while maintaining the enhanced functionality (streaming, LAX mode, type mapping, file I/O).

**Core Optimization Areas:**

* SIMD-accelerated string operations (UTF-8 byte search)
* Span<T>-based parsing to minimize allocations
* Optimized number parsing (replace decimal.Parse)
* Buffer pooling for large payloads
* Escape sequence detection acceleration
* Efficient Unicode handling
* Cache-friendly algorithms

---

## 2. Performance Goals

### 2.1 Parsing Performance

| Metric | Target | Current | Gap |
|--------|--------|---------|-----|
| Small object (1KB) | <100µs | TBD | - |
| Medium array (10KB) | <1ms | TBD | - |
| Large file (100MB) | >100MB/s | TBD | - |
| Memory per 1GB | <50MB | TBD | - |

### 2.2 Serialization Performance

| Metric | Target | Current | Gap |
|--------|--------|---------|-----|
| Object→AJIS (1KB) | <50µs | TBD | - |
| Array→AJIS (10KB) | <500µs | TBD | - |
| Large array (100MB) | >100MB/s | TBD | - |

### 2.3 Comparison Targets

**vs System.Text.Json:**
- ✅ Equal performance (within 10%) on standard cases
- ✅ Better on streaming (no full DOM required)
- ✅ Better on large files (bounded memory)

**vs Newtonsoft.Json:**
- ✅ 2-3x faster on typical workloads
- ✅ Lower memory usage
- ✅ No dynamic dispatch overhead

---

## 3. SIMD Optimization Strategy

### 3.1 String Operations

**Target:** UTF-8 byte sequence search using SIMD

```csharp
// Current: character-by-character search
// Optimized: SIMD parallel byte search
public static int FindByte(ReadOnlySpan<byte> haystack, byte needle)
{
    // Use Vector<byte> or Avx2 for parallel search
    // Compare 16-32 bytes at a time
    // SIMD speedup: 4-8x
}
```

**Applications:**
- Quote finding in strings
- Escape sequence detection
- Whitespace skipping
- Structural character location (`:`, `,`, `[`, `]`, `{`, `}`)

### 3.2 Escape Sequence Detection

**Target:** Optimize EscapeUtf8 validation

```csharp
// Current: validate each escape individually
// Optimized: batch validate escape sequences
public static bool ValidateEscapeSequences(ReadOnlySpan<byte> input)
{
    // SIMD pattern matching for \uXXXX sequences
    // Vectorized Unicode validation
    // SIMD speedup: 3-5x
}
```

### 3.3 Number Validation

**Target:** Replace decimal.Parse with span-based parsing

```csharp
// Current: new string(buffer) + decimal.Parse()
// Optimized: span-based parsing without allocation
public static bool TryParseNumber(ReadOnlySpan<byte> input, out decimal value)
{
    // Direct UTF-8 → number without string conversion
    // No allocation
    // SIMD digit validation
    // Speedup: 2-3x
}
```

---

## 4. Span<T> Based Optimizations

### 4.1 Zero-Copy Design

All parsing operates on `ReadOnlySpan<byte>` without copying:

```csharp
public IAsyncEnumerable<AjisSegment> ParseSegmentsAsync(
    ReadOnlySpan<byte> input)  // ← No copy
{
    // Parse directly from span
    // No intermediate buffers
    // Memory-safe with lifetime guarantees
}
```

### 4.2 Buffer Pooling

Use `ArrayPool<byte>` for temporary buffers:

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    // Use buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

### 4.3 MemoryPool Support

For very large buffers:

```csharp
using (var memPool = MemoryPool<byte>.Shared.Rent(1_000_000))
{
    var memory = memPool.Memory;
    // Use memory safely
}
```

---

## 5. Optimization Roadmap

### Phase 1: Low-Hanging Fruit (Week 1)
- [ ] Buffer pooling for temp arrays
- [ ] Span-based UTF-8 string extraction
- [ ] Remove decimal.Parse calls

### Phase 2: SIMD Optimization (Week 2)
- [ ] SIMD byte search for structural chars
- [ ] SIMD escape validation
- [ ] SIMD number parsing

### Phase 3: Cache Optimization (Week 3)
- [ ] CPU cache-friendly iteration
- [ ] Branch prediction optimization
- [ ] Minimize memory access patterns

---

## 6. Benchmarking Strategy

### 6.1 Test Scenarios

1. **Small Object** (1KB single object)
   - AJIS vs JSON vs Newtonsoft
   - Parse + serialize

2. **Medium Array** (10KB with 50 objects)
   - Array processing
   - Type mapping overhead

3. **Large File** (100MB+ streaming)
   - Memory usage
   - Throughput

4. **Deep Nesting** (100 levels)
   - Stack overhead
   - Recursive call cost

5. **Mixed Workload** (realistic data)
   - Various object shapes
   - Different string lengths

### 6.2 Metrics to Track

- **Throughput:** MB/s (higher is better)
- **Latency:** µs/ms (lower is better)
- **Memory:** MB (lower is better)
- **Allocations:** count (lower is better)
- **CPU Time:** percentage (lower is better)

---

## 7. Benchmark Comparison Framework

All benchmarks will compare:
- **AJIS** (ours)
- **System.Text.Json** (official baseline)
- **Newtonsoft.Json** (feature parity baseline)

Honest results showing:
- ✅ Where AJIS excels
- ❌ Where AJIS lags
- 📊 Trade-offs explained

---

## 8. Performance Testing Checklist

### 8.1 Unit Performance Tests
- [ ] SIMD byte search tests
- [ ] Span-based parsing tests
- [ ] Number parsing tests
- [ ] Escape validation tests

### 8.2 Integration Benchmarks
- [ ] vs System.Text.Json (all scenarios)
- [ ] vs Newtonsoft.Json (all scenarios)
- [ ] Memory allocation profiling
- [ ] Cache miss analysis

### 8.3 Real-World Tests
- [ ] Large JSON files (>100MB)
- [ ] Streaming processing
- [ ] Concurrent parsing
- [ ] Peak memory usage

---

## 9. Showcase TUI Benchmarking Application

### 9.1 Features

Interactive terminal application with:
- Menu system for test selection
- Real-time progress display
- Results comparison table
- Performance graphs (ASCII art)
- Export to CSV
- Save results for trending

### 9.2 Test Menu

```
╔══════════════════════════════════════════════════════╗
║         AJIS Performance Showcase Benchmarks          ║
╚══════════════════════════════════════════════════════╝

Select Test Scenario:
1. Small Object (1KB)
2. Medium Array (10KB)
3. Large File (100MB)
4. Deep Nesting (100 levels)
5. Mixed Workload
6. Run All Tests
7. View Previous Results
8. Export to CSV
Q. Quit

Your choice: _
```

### 9.3 Results Display

```
Small Object Benchmark (1KB)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

                    Time (µs)    Memory (KB)    Allocs
────────────────────────────────────────────────────
AJIS (Ours)         87 µs        2.1 KB        3
System.Text.Json    95 µs        2.5 KB        4
Newtonsoft.Json     450 µs       12.0 KB       15

Summary:
✅ AJIS is 1.09x faster than System.Text.Json
✅ AJIS uses 16% less memory than System.Text.Json
✅ AJIS has 25% fewer allocations than System.Text.Json
⚠️  Newtonsoft.Json is slower (older implementation)

Strengths:
• Lower memory footprint
• Fewer allocations
• Better for streaming scenarios

Areas for Improvement:
• First run has JIT overhead
• Cache line alignment could be improved
```

---

## 10. Transparency Statement

This benchmark is designed to be **honest and unbiased**:

### What We Show
- ✅ Real performance data
- ✅ Where we win
- ✅ Where we lose
- ✅ Trade-offs and reasons
- ✅ Methodology and disclaimers

### What We Don't Do
- ❌ Optimize just for benchmarks (no "benchmark games")
- ❌ Cherry-pick favorable scenarios
- ❌ Hide problematic results
- ❌ Use unfair comparison configurations

---

## 11. References

* M1-M5: Parsing foundation
* M7: Type mapping overhead analysis
* M8A: Streaming requirements
* System.Text.Json source code for patterns
* Benchmarks.NET for reliable measurement

---

## 12. Status

**M6 Status:** IN PROGRESS

- [ ] SIMD optimizations implemented
- [ ] Span-based parsing complete
- [ ] Buffer pooling integrated
- [ ] Benchmarks vs System.Text.Json
- [ ] Benchmarks vs Newtonsoft.Json
- [ ] Showcase TUI application complete
- [ ] All tests passing
- [ ] Documentation complete

---

**Next Step:** Implement SIMD optimizations and benchmarking.
