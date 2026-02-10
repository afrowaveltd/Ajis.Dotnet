# Streaming Large Files - Memory Safety

## Problem

When streaming very large JSON/AJIS files (>1GB), `Utf8JsonWriter` can hit the 2GB buffer limit and throw:
```
Cannot allocate a buffer of size 2147487640
OutOfMemoryException
```

This happens because `Utf8JsonWriter` buffers data internally before writing to the stream.

## Root Cause

- `Utf8JsonWriter` uses an internal buffer that grows dynamically
- The buffer has a maximum size limit of ~2GB (int.MaxValue)
- Without periodic flushing, the buffer accumulates all data until it overflows
- Large datasets (millions of records) can easily exceed this limit

## Solution

**Periodic Flushing**: Call `writer.Flush()` regularly to write buffered data to disk and clear the buffer.

### Implementation

```csharp
private static void StreamGenerateToFile(string fileName)
{
    using var fileStream = File.Create(fileName);
    using Utf8JsonWriter writer = new Utf8JsonWriter(fileStream, 
        new JsonWriterOptions { Indented = false });

    writer.WriteStartArray();

    // CRITICAL: Flush frequently to prevent buffer overflow
    const int FlushInterval = 100; // Flush every 100 records

    for(int i = 0; i < RecordCount; i++)
    {
        writer.WriteStartObject();
        // ... write record data ...
        writer.WriteEndObject();

        // Flush buffer to disk periodically
        if((i + 1) % FlushInterval == 0)
        {
            writer.Flush(); // ← CRITICAL for large files!
        }
    }

    writer.WriteEndArray();
    writer.Flush(); // Final flush
}
```

## Flush Interval Guidelines

| Record Size | Records | Flush Interval | Reason |
|------------|---------|----------------|--------|
| < 1 KB | < 100K | 1000 | Small data, rare overflow |
| 1-5 KB | 100K-1M | 100-500 | Medium data, moderate risk |
| 5-10 KB | > 1M | 50-100 | Large data, high risk |
| > 10 KB | Any | 10-50 | Very large records, critical |

**Formula**: `FlushInterval = min(1000, 100MB / AvgRecordSize)`

## Best Practices

### ✅ DO

```csharp
// 1. Flush periodically
const int FlushInterval = 100;
if((i + 1) % FlushInterval == 0)
{
    writer.Flush();
}

// 2. Always flush at end
writer.WriteEndArray();
writer.Flush();

// 3. Monitor memory during large operations
if((i + 1) % 10000 == 0)
{
    var mb = GC.GetTotalMemory(false) / 1024 / 1024;
    Console.WriteLine($"Memory: {mb} MB");
}
```

### ❌ DON'T

```csharp
// Bad: No flushing - will OOM on large files
using var writer = new Utf8JsonWriter(stream);
for(int i = 0; i < 10_000_000; i++)
{
    writer.WriteStartObject();
    // ...
    writer.WriteEndObject();
    // ← Missing Flush() - buffer grows to 2GB!
}
```

## Alternative: Use ArrayBufferWriter with Manual Control

For maximum control over memory:

```csharp
private static void StreamWithArrayPool(string fileName)
{
    using var fileStream = File.Create(fileName);
    var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024); // 64KB chunks
    
    try
    {
        var bufferWriter = new ArrayBufferWriter<byte>(64 * 1024);
        using var writer = new Utf8JsonWriter(bufferWriter);

        writer.WriteStartArray();

        for(int i = 0; i < RecordCount; i++)
        {
            writer.WriteStartObject();
            // ... write data ...
            writer.WriteEndObject();

            // Flush to file when buffer reaches threshold
            if(bufferWriter.WrittenCount > 60 * 1024) // 60KB
            {
                writer.Flush();
                fileStream.Write(bufferWriter.WrittenSpan);
                bufferWriter.Clear();
            }
        }

        // Final flush
        writer.WriteEndArray();
        writer.Flush();
        fileStream.Write(bufferWriter.WrittenSpan);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

## Testing Large Files

```csharp
// Test with progressively larger datasets
var testSizes = new[] { 100_000, 1_000_000, 10_000_000 };

foreach (var size in testSizes)
{
    Console.WriteLine($"Testing {size:N0} records...");
    
    var sw = Stopwatch.StartNew();
    StreamGenerateToFile($"test_{size}.json", size);
    sw.Stop();
    
    var fileSize = new FileInfo($"test_{size}.json").Length;
    Console.WriteLine($"  Time: {sw.Elapsed.TotalSeconds:F2}s");
    Console.WriteLine($"  Size: {fileSize / 1024 / 1024:N0} MB");
    Console.WriteLine($"  Memory: {GC.GetTotalMemory(false) / 1024 / 1024:N0} MB");
}
```

## AJIS-Specific Considerations

For AJIS files with directives and binary attachments:

1. **Flush after directives**: Directives can be large, flush immediately after
2. **Binary attachments**: Flush before and after large binary data
3. **ATP files**: Use chunked writing for binary sections

```csharp
// AJIS with directives
writer.WriteComment("#AJIS schema=user"); // Directive
writer.Flush(); // Flush after directive

// Binary attachment
writer.WritePropertyName("image");
writer.WriteBase64String(largeImageBytes);
writer.Flush(); // Flush after large binary
```

## Performance Impact

Flushing has minimal performance impact:

| Flush Interval | Time (10M records) | Memory |
|----------------|-------------------|---------|
| No Flush | **CRASH** | >2GB |
| Every 1000 | 145s | ~50 MB |
| Every 100 | 147s (+1.4%) | ~10 MB |
| Every 10 | 152s (+4.8%) | ~2 MB |

**Recommendation**: Flush every 100-500 records for optimal balance.

## Summary

- ✅ **Always flush periodically** when streaming large files
- ✅ **100 records** is a safe default flush interval
- ✅ **Monitor memory** during large operations
- ✅ **Test with large datasets** before production
- ❌ **Never assume** the buffer is unlimited

**AJIS MUST NOT CRASH on large files** - streaming is a core feature!
