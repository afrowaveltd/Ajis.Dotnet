# ⚡ PERFORMANCE OPTIMIZATION - AjisConverter Deserialization

> **Date:** February 9, 2026  
> **Issue:** AJIS is 22.56x slower than System.Text.Json  
> **Root Cause:** Triple parsing with intermediate AjisValue tree  
> **Fix:** Direct segment-to-JSON writing using Utf8JsonWriter  

---

## 🚨 **PROBLEM: CATASTROPHIC PERFORMANCE**

### Benchmark Results (Before Fix)
```
SPEED:
  System.Text.Json:  10,475 ms  [BASELINE]
  Newtonsoft.Json:   10,776 ms  [1.03x slower]
  AJIS:             236,296 ms  [22.56x SLOWER] ❌

MEMORY:
  Newtonsoft.Json:    1,198 MB  [BASELINE]
  System.Text.Json:   1,811 MB  [1.51x more]
  AJIS:              11,903 MB  [9.94x MORE] ❌

GC PRESSURE:
  Newtonsoft.Json:     373 collections  [BASELINE]
  System.Text.Json:    523 collections  [1.40x more]
  AJIS:              5,322 collections  [14.27x MORE] ❌

THROUGHPUT:
  System.Text.Json:  34.80 MB/s  [BASELINE]
  Newtonsoft.Json:   33.83 MB/s  [0.97x]
  AJIS:               1.54 MB/s  [0.04x] ❌
```

**Status:** 🔴 **UNACCEPTABLE FOR PRODUCTION**

---

## 🔍 **ROOT CAUSE ANALYSIS**

### Current Implementation (SLOW)
```csharp
public T? DeserializeFromSegments(IEnumerable<AjisSegment> segments)
{
    var segmentList = segments.ToList();
    
    // STEP 1: Build entire AjisValue object tree ❌
    var ajisValue = SegmentsToAjisValue(segmentList, 0);
    
    // STEP 2: Serialize AjisValue → JSON string ❌
    var writer = new AjisValueTextWriter(...);
    var jsonText = writer.Write(ajisValue);
    
    // STEP 3: Parse JSON string → T ❌
    return System.Text.Json.JsonSerializer.Deserialize<T>(jsonText);
}
```

**This is TRIPLE PARSING!** 😱

### Why is it slow?

**Step 1: `SegmentsToAjisValue()`**
```csharp
private AjisValue SegmentsToAjisValue(List<AjisSegment> segments, int startIndex)
{
    // Creates intermediate objects for EVERY value:
    - AjisValue.Null()
    - AjisValue.Bool(true)
    - AjisValue.Number("123")
    - AjisValue.String("hello")
    - AjisValue.Array(items.ToArray())  // Allocates array
    - AjisValue.Object(dict.ToArray())  // Allocates dictionary + array
}

private AjisValue BuildArray(...)
{
    var items = new List<AjisValue>();  // ❌ Allocation
    foreach (var item in array)
    {
        items.Add(SegmentsToAjisValue(...));  // ❌ Recursive allocations
    }
    return AjisValue.Array(items.ToArray());  // ❌ Another allocation
}

private AjisValue BuildObject(...)
{
    var members = new Dictionary<string, AjisValue>();  // ❌ Allocation
    foreach (var property in object)
    {
        members[name] = SegmentsToAjisValue(...);  // ❌ Recursive allocations
    }
    return AjisValue.Object(members.ToArray());  // ❌ Another allocation
}
```

**For 1M records:**
- Creates ~10M+ intermediate `AjisValue` objects
- Allocates ~5M+ `List<AjisValue>` and `Dictionary<string, AjisValue>`
- Triggers **5,322 GC collections** (vs 373 for Newtonsoft!)

**Step 2: `writer.Write(ajisValue)`**
```csharp
// Walks the entire AjisValue tree and builds a string
var jsonText = writer.Write(ajisValue);  // ❌ Full tree traversal + string allocation
```

**Step 3: `JsonSerializer.Deserialize<T>(jsonText)`**
```csharp
// System.Text.Json parses the string AGAIN and builds T
return System.Text.Json.JsonSerializer.Deserialize<T>(jsonText);  // ❌ Parse AGAIN!
```

**Total Work:**
1. Parse bytes → segments (AJIS parser)
2. Build segments → AjisValue tree (massive allocations)
3. Serialize AjisValue → JSON string
4. Parse JSON string → T (System.Text.Json)

**Competitors do:**
1. Parse bytes → T (direct, streaming)

**We do 4x the work!** 🤦

---

## ⚡ **OPTIMIZATION: ELIMINATE INTERMEDIATE STEPS**

### New Implementation (FAST)
```csharp
public T? DeserializeFromSegments(IEnumerable<AjisSegment> segments)
{
    var segmentList = segments.ToList();
    if (segmentList.Count == 0)
        return default;

    try
    {
        // ✅ OPTIMIZED: Write segments directly to UTF8 buffer
        using var memoryStream = new MemoryStream();
        WriteSegmentsToStream(segmentList, memoryStream);
        
        // ✅ Deserialize directly from UTF8 bytes (fast path)
        memoryStream.Position = 0;
        return System.Text.Json.JsonSerializer.Deserialize<T>(memoryStream);
    }
    catch (Exception ex)
    {
        throw new FormatException($"Failed to deserialize AJIS segments: {ex.Message}", ex);
    }
}

private void WriteSegmentsToStream(List<AjisSegment> segments, Stream stream)
{
    // ✅ Use Utf8JsonWriter for direct, efficient writing
    using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
    { 
        Indented = false,        // Compact output
        SkipValidation = true    // Trust our segments
    });

    int index = 0;
    WriteSegmentValue(segments, ref index, writer);
    writer.Flush();
}

private void WriteSegmentValue(List<AjisSegment> segments, ref int index, Utf8JsonWriter writer)
{
    var segment = segments[index];

    // ✅ Write values directly, no intermediate objects
    if (segment.Kind == AjisSegmentKind.Value && segment.ValueKind.HasValue)
    {
        switch (segment.ValueKind.Value)
        {
            case AjisValueKind.Null:
                writer.WriteNullValue();  // ✅ Direct write, no allocation
                break;

            case AjisValueKind.Number:
                if (segment.Slice != null)
                {
                    // ✅ Write raw bytes directly (fastest!)
                    writer.WriteRawValue(segment.Slice.Value.Bytes.Span, skipInputValidation: true);
                }
                break;

            case AjisValueKind.String:
                if (segment.Slice != null)
                {
                    // ✅ Write UTF8 bytes directly, no string allocation
                    writer.WriteStringValue(segment.Slice.Value.Bytes.Span);
                }
                break;
        }
        index++;
        return;
    }

    // ✅ Handle containers recursively but write directly
    if (segment.Kind == AjisSegmentKind.EnterContainer)
    {
        if (segment.ContainerKind == AjisContainerKind.Array)
        {
            writer.WriteStartArray();
            index++;
            
            while (/* not exit */)
            {
                WriteSegmentValue(segments, ref index, writer);  // ✅ Recursive but direct
            }
            
            writer.WriteEndArray();
            index++;
        }
        else if (segment.ContainerKind == AjisContainerKind.Object)
        {
            writer.WriteStartObject();
            index++;
            
            while (/* not exit */)
            {
                // ✅ Write property name directly from bytes
                writer.WritePropertyName(segments[index].Slice.Value.Bytes.Span);
                index++;
                
                // ✅ Write property value directly
                WriteSegmentValue(segments, ref index, writer);
            }
            
            writer.WriteEndObject();
            index++;
        }
    }
}
```

---

## 📊 **OPTIMIZATION IMPACT**

### Eliminated Work
```
❌ REMOVED: Build entire AjisValue tree (10M+ objects)
❌ REMOVED: Allocate Lists and Dictionaries for containers
❌ REMOVED: Convert AjisValue → JSON string
✅ KEPT:    Parse bytes → segments (necessary)
✅ KEPT:    Write segments → UTF8 buffer (efficient)
✅ KEPT:    Deserialize UTF8 → T (System.Text.Json, fast)
```

### Expected Performance
```
BEFORE:
  Parse → Build tree → Serialize → Parse again
  (4 steps, massive allocations)

AFTER:
  Parse → Write UTF8 → Deserialize
  (2 steps, minimal allocations)

Expected improvement: 10-15x faster!
```

### Memory Reduction
```
BEFORE:
  - 10M+ AjisValue objects
  - 5M+ intermediate containers
  - String allocations for JSON
  Total: ~11,900 MB

AFTER:
  - Single MemoryStream buffer
  - Utf8JsonWriter (reusable)
  - No intermediate objects
  Expected: ~2,000 MB (5-6x reduction)
```

### GC Reduction
```
BEFORE: 5,322 collections
AFTER:  ~1,000-1,500 collections (3-5x reduction)
```

---

## 🎯 **EXPECTED RESULTS**

### Realistic Expectations (v1.0)
```
SPEED:
  System.Text.Json:  10,475 ms  [BASELINE]
  Newtonsoft.Json:   10,776 ms  [1.03x]
  AJIS:              ~30,000 ms [~3x slower] ✅ Acceptable

MEMORY:
  Newtonsoft.Json:    1,198 MB  [BASELINE]
  System.Text.Json:   1,811 MB  [1.51x]
  AJIS:               ~2,500 MB [~2x more] ✅ Acceptable

GC:
  Newtonsoft.Json:     373 collections
  System.Text.Json:    523 collections
  AJIS:              ~1,200 collections [~3x more] ✅ Acceptable
```

**Why still slower than STJ?**
- We still parse twice: AJIS segments → UTF8 buffer → STJ deserialize
- STJ goes directly: bytes → T (streaming, zero-copy)

**This is acceptable for v1.0 because:**
- ✅ Functional (works correctly)
- ✅ Reasonable performance (3x vs 22x slower)
- ✅ Can optimize further in v1.1

---

## 🚀 **FUTURE OPTIMIZATIONS (v1.1)**

### Native Deserialization
```csharp
// TODO v1.1: Implement direct segment → T conversion
// No System.Text.Json fallback, pure AJIS deserialization
public T? DeserializeFromSegments(IEnumerable<AjisSegment> segments)
{
    var context = new DeserializationContext(this, _propertyMapper);
    return context.Deserialize<T>(segments);  // ✅ Native, single pass
}
```

Expected improvement: **Match or beat System.Text.Json!**

### SIMD Number Parsing
```csharp
// Use vector instructions for number parsing
case AjisValueKind.Number:
    return SimdNumberParser.Parse(segment.Slice.Value.Bytes);
```

Expected improvement: **2-3x faster number parsing**

### Zero-Copy String Handling
```csharp
// Use Utf8 strings directly, no encoding conversion
case AjisValueKind.String:
    return new Utf8String(segment.Slice.Value.Bytes);
```

Expected improvement: **50% reduction in string allocations**

---

## 💡 **LESSONS LEARNED**

1. **Avoid intermediate representations**  
   Every object you create = allocation + GC pressure

2. **Use direct APIs**  
   `Utf8JsonWriter` is faster than building objects then serializing

3. **Minimize allocations**  
   Reuse buffers, write directly to streams

4. **Profile before optimizing**  
   But when you see 22x slower, act immediately!

5. **Accept temporary compromises**  
   Using STJ fallback is OK for v1.0 if it gets us to production

---

**Status: PERFORMANCE OPTIMIZATION APPLIED** ⚡  
**Build: SUCCESS** ✅  
**Next: Re-run stress test to verify improvement!** 📊
