# 🚀 AJIS Performance Optimization Plan

> **Goal:** Achieve 2-3x slower than System.Text.Json (or better!)  
> **Current:** 12.5x slower (UNACCEPTABLE)  
> **Method:** Systematic component-by-component optimization  

---

## 📊 CURRENT PERFORMANCE (1M records)

```
SPEED:
  System.Text.Json:   11,618 ms  [BASELINE]
  Newtonsoft.Json:    11,526 ms  [0.99x]
  AJIS:              144,547 ms  [12.54x SLOWER] ❌

MEMORY:
  Newtonsoft.Json:    1,198 MB  [BASELINE]
  System.Text.Json:   1,811 MB  [1.51x]
  AJIS:               8,159 MB  [6.81x MORE] ❌

GC PRESSURE:
  Newtonsoft.Json:     372 collections
  System.Text.Json:    528 collections
  AJIS:              3,206 collections [8.6x MORE] ❌
```

**VERDICT:** 🔴 **CRITICAL - NEEDS IMMEDIATE OPTIMIZATION**

---

## 🔍 ROOT CAUSE ANALYSIS

### 1. **Deserialization Bottleneck** (Primary Issue)

**Current Flow:**
```
Bytes → AJIS Parse → Segments → WriteSegmentsToStream → MemoryStream 
  → System.Text.Json Deserialize → T
```

**Problems:**
1. **Double parsing** - Parse once to segments, then STJ parses again
2. **String allocations** - `Encoding.UTF8.GetString()` called for EVERY value
3. **Intermediate buffer** - MemoryStream holds entire JSON copy

**Expected Impact:** **8-10x speedup if fixed!**

---

### 2. **Memory Allocations** (Secondary Issue)

**Hot Paths:**
```csharp
// In WriteSegmentValue() - CALLED MILLIONS OF TIMES:
case AjisValueKind.Boolean:
    var boolStr = Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);  // ❌ ALLOCATION!
    writer.WriteBooleanValue(boolStr == "true");

case AjisValueKind.String:
    writer.WriteStringValue(segment.Slice.Value.Bytes.Span);  // ✅ Good (no allocation)
```

**Problem:** Boolean parsing allocates string for comparison!

**Expected Impact:** **~15% speedup**

---

### 3. **Serialization Overhead** (Tertiary Issue)

**Current Flow:**
```
T → ObjectToAjisValue (builds tree) → AjisValueTextWriter → String
```

**Problems:**
1. Builds intermediate `AjisValue` tree (millions of objects)
2. Uses reflection for property mapping
3. String concatenation for output

**Expected Impact:** **2-3x speedup with direct writing**

---

## 🎯 OPTIMIZATION ROADMAP

### Phase 1: **Critical Bottlenecks** (Target: 5-6x faster)

#### 1.1 Fix WriteSegmentValue String Allocations
```csharp
// BEFORE (SLOW):
case AjisValueKind.Boolean:
    var boolStr = Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);
    writer.WriteBooleanValue(boolStr == "true");

// AFTER (FAST):
case AjisValueKind.Boolean:
    // Compare bytes directly, no allocation
    writer.WriteBooleanValue(
        segment.Slice.Value.Bytes.Span.SequenceEqual("true"u8));
```

**Expected:** ~15% speedup, ~20% memory reduction

#### 1.2 Optimize WriteSegmentsToStream
```csharp
// Current: Creates MemoryStream, then deserializes
// Better: Use ArrayPool for buffer reuse
// Best: Direct deserialization from segments (no JSON intermediate)
```

**Expected:** ~30% speedup

#### 1.3 Pool MemoryStream Buffers
```csharp
// Use ArrayPool<byte> to avoid GC pressure
private static readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

// Rent buffer instead of new MemoryStream
var buffer = _bufferPool.Rent(estimatedSize);
```

**Expected:** ~40% memory reduction, ~10% speedup

---

### Phase 2: **Eliminate Double Parsing** (Target: 8-10x faster)

#### 2.1 Direct Segment → T Deserialization
```csharp
// CURRENT (SLOW):
Segments → JSON → System.Text.Json → T

// TARGET (FAST):
Segments → T (single pass, no intermediate)
```

**Implementation:**
```csharp
public T? DeserializeFromSegments(IEnumerable<AjisSegment> segments)
{
    // NO System.Text.Json fallback!
    var context = new FastDeserializationContext<T>();
    return context.Deserialize(segments);
}

private class FastDeserializationContext<T>
{
    public T Deserialize(IEnumerable<AjisSegment> segments)
    {
        // Direct instantiation + property setting
        // Use IL Emit or source generators for maximum speed
        // Similar to System.Text.Json's approach
    }
}
```

**Expected:** **8-10x speedup!** (Eliminates entire JSON round-trip)

---

### Phase 3: **Serialization Optimization** (Target: 2-3x faster)

#### 3.1 Direct Object → Segments Writing
```csharp
// CURRENT (SLOW):
T → AjisValue tree → String

// TARGET (FAST):
T → Utf8JsonWriter (direct, no tree)
```

**Implementation:**
```csharp
public string Serialize(T value)
{
    using var buffer = new ArrayBufferWriter<byte>();
    using var writer = new Utf8JsonWriter(buffer);
    
    WriteObjectDirect(value, writer);  // No AjisValue intermediate
    writer.Flush();
    
    return Encoding.UTF8.GetString(buffer.WrittenSpan);
}

private void WriteObjectDirect(object obj, Utf8JsonWriter writer)
{
    // Use cached PropertyInfo, no reflection on hot path
    // Write properties directly to writer
}
```

**Expected:** 2-3x speedup, 50% memory reduction

---

### Phase 4: **Advanced Optimizations** (Target: Match or beat STJ)

#### 4.1 SIMD Number Parsing
```csharp
// Use Vector128<T> for fast number parsing
// Similar to System.Text.Json's Utf8Parser
```

**Expected:** 2x faster number parsing

#### 4.2 Source Generators
```csharp
// Generate serialization code at compile time
// No reflection, direct property access
[AjisSerializable]
public class User { ... }

// Generates:
// UserAjisSerializer with optimized code
```

**Expected:** 3-5x speedup, zero allocations

#### 4.3 Span-based APIs
```csharp
// Eliminate string allocations
public void Serialize(T value, Span<byte> destination);
public T Deserialize(ReadOnlySpan<byte> source);
```

**Expected:** 40% memory reduction

---

## 📈 EXPECTED RESULTS

### After Phase 1 (Quick Wins)
```
Speed:  144s → ~90s (1.6x faster)
Memory: 8.1GB → 4.8GB (1.7x reduction)
Status: Still not good enough
```

### After Phase 2 (Eliminate Double Parsing)
```
Speed:  90s → ~12s (7.5x faster!)
Memory: 4.8GB → 2.5GB (3.2x reduction)
Status: Competitive with STJ/Newtonsoft
```

### After Phase 3 (Optimize Serialization)
```
Speed:  12s → 8s (1.5x faster)
Memory: 2.5GB → 1.5GB (1.7x reduction)  
Status: Better than Newtonsoft, close to STJ
```

### After Phase 4 (Advanced Opts)
```
Speed:  8s → 6s (1.3x faster)
Memory: 1.5GB → 1.2GB (1.25x reduction)
Status: COMPETITIVE OR BETTER THAN STJ! 🎯
```

---

## 🧪 TESTING STRATEGY

### 1. **Micro-Benchmarks** (Isolated Components)
```csharp
- Lexer speed (tokens/second)
- Number parser (numbers/second)  
- String parsing (strings/second)
- Object property reading (objects/second)
```

### 2. **Component Benchmarks** (Integration)
```csharp
- Parse only (bytes → segments)
- Serialize only (object → bytes)
- Round-trip (object → bytes → object)
```

### 3. **Stress Tests** (Real-world Scale)
```csharp
- 10K records (small)
- 100K records (medium)
- 1M records (large)
- 10M records (extreme)
- 100M records (insane - GB scale)
```

### 4. **Memory Profiling**
```csharp
- Allocation tracking
- GC pressure analysis
- Peak memory measurement
- Object lifetime analysis
```

---

## 🛠️ IMPLEMENTATION PLAN

### Week 1: Phase 1 (Critical Bottlenecks)
- [ ] Day 1-2: Fix string allocations
- [ ] Day 3-4: Pool MemoryStream buffers
- [ ] Day 5: Benchmark & validate (~1.6x improvement)

### Week 2: Phase 2 (Eliminate Double Parsing)
- [ ] Day 1-3: Implement direct deserialization
- [ ] Day 4-5: Benchmark & validate (~7.5x improvement!)

### Week 3: Phase 3 (Optimize Serialization)
- [ ] Day 1-3: Direct object writing
- [ ] Day 4-5: Benchmark & validate (~1.5x improvement)

### Week 4: Phase 4 (Advanced)
- [ ] Day 1-2: SIMD number parsing
- [ ] Day 3-4: Source generators (if time)
- [ ] Day 5: Final benchmarks & release!

---

## 📝 SUCCESS CRITERIA

### Minimum (v1.0 Launch)
```
✅ Speed: Within 3x of System.Text.Json
✅ Memory: Within 2x of System.Text.Json  
✅ GC: Within 3x of System.Text.Json
✅ All tests passing
```

### Target (v1.0 Ideal)
```
🎯 Speed: Within 2x of System.Text.Json
🎯 Memory: Within 1.5x of System.Text.Json
🎯 GC: Within 2x of System.Text.Json
🎯 All features complete
```

### Stretch (v1.1+)
```
🚀 Speed: Match or beat System.Text.Json
🚀 Memory: Better than System.Text.Json
🚀 GC: Better than Newtonsoft
🚀 Zero-allocation APIs
```

---

## 💡 LESSONS & PRINCIPLES

1. **Measure before optimizing** - Profile, don't guess
2. **One thing at a time** - Validate each change
3. **Hot path focus** - 80% of time in 20% of code
4. **Allocation matters** - Every `new` hurts GC
5. **Span is your friend** - Zero-copy whenever possible
6. **Cache everything** - Reflection, metadata, buffers
7. **Benchmark continuously** - Regression tests
8. **No compromises** - Performance IS a feature

---

**Status: ROADMAP DEFINED** 🗺️  
**Next: Implement Phase 1 optimizations!** ⚡
