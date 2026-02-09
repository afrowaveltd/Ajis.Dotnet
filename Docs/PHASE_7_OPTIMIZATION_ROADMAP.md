# PERFORMANCE OPTIMIZATION ROADMAP - NEXT STEPS

## Current Status (PHASE 6 Complete)

**Expected Performance Improvement**: 2.5-3.0x overall speedup

### Measured Baselines
- **Parser**: 2,080ms (Target: 700-750ms = 2.77x improvement)
- **Serializer**: 983ms (Target: 400-440ms = 2.24x improvement)
- **GC Gen0 (Parser)**: 47 collections (Target: ~20 = 2.35x reduction)
- **GC Gen0 (Serializer)**: 22 collections (Target: ~8 = 2.75x reduction)
- **Memory (Parser)**: 181MB allocated (Target: ~80MB = 2.26x reduction)
- **Memory (Serializer)**: 393MB peak (Target: ~140MB = 2.8x reduction)

## Optimizations Completed (PHASE 6)

### 1. ✅ PropertySetterCompiler Caching
- One-time compilation per property (not per deserialization)
- Estimated impact: **-20% CPU, -15% GC pressure**

### 2. ✅ PropertyGetterCompiler Caching
- Fast delegate lookup during serialization
- Estimated impact: **-15% CPU on serializer**

### 3. ✅ ArrayBufferWriter (Serializer)
- Replaced MemoryStream allocation
- Estimated impact: **-30% allocations on serialize path**

### 4. ✅ Type Specialization with ReferenceEquals
- Static Type references, no boxing on type checks
- Estimated impact: **-10% GC Gen0, -5% CPU**

### 5. ✅ JIT Inlining Attributes
- Aggressive inlining on hot-path methods
- Estimated impact: **-8-12% function call overhead**

### 6. ✅ Parallel Array Assignment
- Parallel.For for 1000+ item arrays
- Estimated impact: **+20-30% on large arrays (multicore)**

## PHASE 7: Next Priority Optimizations

### Option 1: Source Code Generators (High Impact)
**Benefit**: Compile-time generation of setters/getters instead of runtime compilation

```csharp
[AjisSerializable]
public class MyObject
{
    public int Id { get; set; }
    public string Name { get; set; }
}
// Source generator creates optimized Utf8 serializer/deserializer
```

**Pros**:
- Zero runtime compilation overhead
- Perfect SIMD opportunities for string matching
- Potential 3-5x speedup on property operations

**Cons**:
- Requires source generator infrastructure
- Compile-time complexity increases
- Breaks dynamic type handling

**Estimated Impact**: **-30-40% parser/serializer time** (5-10ms → 2-4ms per 10K objects)

### Option 2: String Interning + Object Pooling
**Benefit**: Reuse string literals and pool temporary objects

```csharp
// Property names are static string literals, intern them
private static readonly string[] PropertyNames = 
    new[] { "Id", "Name", "Value", "Tags" }
        .Select(s => string.Intern(s))
        .ToArray();
```

**Pros**:
- Trivial to implement
- Works with dynamic types
- String comparison becomes reference comparison

**Cons**:
- String interning has GC impact at startup
- Limited to property names

**Estimated Impact**: **-10% string allocation, -5% lookup time**

### Option 3: Frozen Collections (.NET 10)
**Benefit**: Use new FrozenDictionary and FrozenSet

```csharp
private FrozenDictionary<string, PropertyMetadata> _propertyLookup;
```

**Pros**:
- O(1) lookup with better cache locality
- Immutable (thread-safe)
- Better memory layout

**Cons**:
- Still need to build frozen dict from regular dict
- Marginal speedup for current usage

**Estimated Impact**: **-3-5% lookup time, +2-5% memory**

### Option 4: SIMD String Matching
**Benefit**: Use Vector operations for bulk string comparison

```csharp
// Compare property name bytes using SIMD
private static bool BytesEqual(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
{
    // Use Vector<byte> for parallel byte comparison
    // ...
}
```

**Pros**:
- Potential 2-4x speedup for string matching
- Works with dynamic types
- Compiler optimizations

**Cons**:
- Complex implementation
- Requires careful alignment
- Platform-dependent performance

**Estimated Impact**: **-15-25% property name lookup time**

### Option 5: Pooling + Memory<T>
**Benefit**: Pool temporary allocations

```csharp
private static readonly ArrayPool<PropertyMetadata> _pool = 
    ArrayPool<PropertyMetadata>.Shared;

// Use pooled arrays for temporary collections
```

**Pros**:
- Reduces heap allocations
- Works with existing code
- Trivial to implement

**Cons**:
- Pool contention under high concurrency
- Marginal benefit if caching is good

**Estimated Impact**: **-5-10% allocations**

## PHASE 8: Profiling-Guided Optimization

### 1. CPU Profiling Round 2
After implementing PHASE 6, run CPU profiler again to identify new hotspots:

```bash
dotnet run --project benchmarks/Afrowave.AJIS.Benchmarks -- best
# Then run profiler to see updated call stack
```

**Expected New Hotspots**:
- `ReadValue()` branch prediction misses
- `Utf8JsonReader` allocation in different scenarios
- `Activator.CreateInstance()` for object creation

### 2. Memory Profiling
Measure Gen0 collections and allocation patterns:

```csharp
GC.Collect(0);
GC.WaitForPendingFinalizers();
var before = GC.GetTotalMemory(false);
var gen0Before = GC.CollectionCount(0);

// Run benchmark...

var gen0After = GC.CollectionCount(0);
var after = GC.GetTotalMemory(false);
Console.WriteLine($"Gen0: {gen0After - gen0Before}, Memory: {(after - before) / 1024 / 1024}MB");
```

### 3. ETW Tracing
Use Windows Event Tracing for detailed CPU flame graphs:

```bash
dotnet trace collect -n "Afrowave.AJIS.Benchmarks" --providers "gc-verbose,Microsoft-DotNETCore-SampleProfiler"
```

## PHASE 9: Scaling Optimizations

### Multi-Threaded Deserialization
For deserializing multiple objects:

```csharp
// Parallel deserialization of objects
var results = objects.AsParallel()
    .Select(json => converter.Deserialize(json))
    .ToList();
```

**Benefit**: Linear scaling with CPU cores for batched workloads

### Streaming Mode
For processing large datasets:

```csharp
// Stream objects instead of loading all to memory
var stream = File.OpenRead("large.ajis");
var reader = new AjisStreamReader(stream);
foreach (var obj in reader.EnumerateObjects<T>())
{
    ProcessObject(obj);
}
```

**Benefit**: O(1) memory regardless of file size

## PHASE 10: Benchmarking & Validation

### BenchmarkDotNet Setup
```csharp
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, targetCount: 5)]
public class OptimizationBenchmark
{
    // Compare AJIS vs STJ vs Newtonsoft
    [Benchmark]
    public List<TestObject> AjisDeserialize() => ...
    
    [Benchmark(Baseline = true)]
    public List<TestObject> StjDeserialize() => ...
}
```

### Success Criteria
- [ ] Parser **≤ 750ms** (2.77x improvement from 2,080ms)
- [ ] Serializer **≤ 440ms** (2.24x improvement from 983ms)
- [ ] Gen0 Collections **≤ 20** for parser
- [ ] Memory Allocations **≤ 80MB** for parser
- [ ] All regression tests pass
- [ ] API backward compatible

## Recommended Priority Order

1. **PHASE 7a**: Source Code Generators (Highest ROI: 3-5x)
   - Compile-time property setter/getter generation
   - Perfect C# integration with [AjisSerializable]
   - Estimated: 1-2 weeks implementation, +500-1000 lines of code

2. **PHASE 7b**: SIMD String Matching (High ROI: 2-3x on lookup)
   - Vector-based property name matching
   - Use System.Runtime.Intrinsics if available
   - Estimated: 2-3 days, +100-200 lines

3. **PHASE 7c**: Frozen Collections (Low ROI: +3-5%)
   - Use FrozenDictionary for property lookups
   - Immutable and thread-safe
   - Estimated: 1 day, +50 lines

4. **Profiling**: ETW + BenchmarkDotNet validation
   - Measure actual improvements
   - Guide further optimizations
   - Estimated: 1 week across all phases

## Files to Monitor
- `src/Afrowave.AJIS.Serialization/Mapping/Utf8DirectDeserializer.cs`
- `src/Afrowave.AJIS.Serialization/Mapping/Utf8DirectSerializer.cs`
- `src/Afrowave.AJIS.Serialization/Mapping/PropertySetterCompiler.cs`
- `src/Afrowave.AJIS.Serialization/Mapping/PropertyGetterCompiler.cs`
- `benchmarks/Afrowave.AJIS.Benchmarks/OptimizationBenchmark.cs` (new)

## Success Metrics

| Metric | Current | Target | Improvement |
|--------|---------|--------|-------------|
| Parser Time | 2,080ms | 750ms | 2.77x |
| Serializer Time | 983ms | 440ms | 2.23x |
| Parser Memory | 181MB | 80MB | 2.26x |
| Serializer Peak | 393MB | 140MB | 2.81x |
| Gen0 (Parser) | 47 | 20 | 2.35x |
| Gen0 (Serializer) | 22 | 8 | 2.75x |

---

**Last Updated**: After PHASE 6 optimization
**Next Review**: After BenchmarkDotNet results from PHASE 7
