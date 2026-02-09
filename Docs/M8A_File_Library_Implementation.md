# M8A – AJIS File Library (File-Based CRUD) Implementation Status

> **Status:** IN PROGRESS → COMPLETION
>
> This document tracks the M8A (File Library) milestone for file-based AJIS operations without full in-memory loading.

---

## 1. M8A Scope

M8A implements file-based CRUD operations for AJIS documents, enabling processing of arbitrarily large files with bounded memory usage. This is a **unique differentiator** vs System.Text.Json which requires full DOM materialization.

**Core Responsibilities:**

* Sequential reading of AJIS segments from files
* Sequential writing of AJIS segments to files
* Create operations (append to file)
* Read operations (stream from position)
* Update operations (replace in place or rebuild)
* Delete operations (rebuild without deleted items)
* Optional: File indexing for fast lookups
* Optional: Query/search in files
* Memory-bounded processing
* Transaction-safe operations

---

## 2. Design Goals

### 2.1 Zero-Copy Where Possible
Users should be able to work with multi-gigabyte AJIS files without loading them into memory.

### 2.2 Simple API
Familiar patterns similar to System.IO.File and LINQ:
```csharp
// Read all objects from file
var users = AjisFile.ReadAll<User>("users.ajis");

// Append object to file
AjisFile.Append<User>("users.ajis", newUser);

// Update object at index
AjisFile.Update<User>("users.ajis", index, updatedUser);

// Delete object at index
AjisFile.Delete("users.ajis", index);

// Stream objects without loading all
foreach (var user in AjisFile.EnumerateObjects<User>("users.ajis"))
{
    // Process user
}
```

### 2.3 Streaming Processing
Seamless integration with AJIS streaming parser for memory-bounded access:
```csharp
// Process large file with streaming
using (var reader = new AjisFileReader("large_data.ajis"))
{
    await foreach (var segment in reader.ReadSegmentsAsync())
    {
        // Process segment (memory bounded)
    }
}
```

### 2.4 Transaction Safety
Operations should be atomic (all-or-nothing) to prevent file corruption.

---

## 3. File Format

AJIS files are valid AJIS text (JSON-compatible). The M8A library treats them as sequential streams of segments, without requiring special binary headers or format extensions.

### 3.1 Array-Based Storage (Primary Pattern)

Most common: Array of objects in file
```ajis
[
  {"id": 1, "name": "Alice", "email": "alice@example.com"},
  {"id": 2, "name": "Bob", "email": "bob@example.com"},
  {"id": 3, "name": "Charlie", "email": "charlie@example.com"}
]
```

### 3.2 Object-Based Storage (Alternative)

Single object with array property:
```ajis
{
  "version": 1,
  "items": [
    {"id": 1, "name": "Alice"},
    {"id": 2, "name": "Bob"}
  ]
}
```

---

## 4. API Design

### 4.1 High-Level Static API (AjisFile class)

```csharp
public static class AjisFile
{
    // Read operations
    public static List<T> ReadAll<T>(string path);
    public static IAsyncEnumerable<T> ReadAllAsync<T>(string path);
    public static T? ReadAt<T>(string path, int index);
    public static IEnumerable<T> Enumerate<T>(string path);
    public static IAsyncEnumerable<T> EnumerateAsync<T>(string path);
    
    // Write operations
    public static void Create<T>(string path, IEnumerable<T> items);
    public static Task CreateAsync<T>(string path, IAsyncEnumerable<T> items);
    public static void Append<T>(string path, T item);
    public static Task AppendAsync<T>(string path, T item);
    
    // Update operations
    public static void Update<T>(string path, int index, T item);
    public static Task UpdateAsync<T>(string path, int index, T item);
    public static void Replace<T>(string path, Func<T, bool> predicate, T replacement);
    
    // Delete operations
    public static void Delete(string path, int index);
    public static void DeleteWhere<T>(string path, Func<T, bool> predicate);
    
    // Query operations (with index)
    public static T? Find<T>(string path, Func<T, bool> predicate);
    public static List<T> Where<T>(string path, Func<T, bool> predicate);
    public static int Count<T>(string path);
}
```

### 4.2 Low-Level Reader API (AjisFileReader class)

```csharp
public class AjisFileReader : IAsyncDisposable
{
    public AjisFileReader(string path);
    
    // Read segments
    public IAsyncEnumerable<AjisSegment> ReadSegmentsAsync();
    
    // Seek to position
    public Task<long> SeekAsync(long byteOffset);
    
    // Get file info
    public long FileSize { get; }
    public bool IsAtEnd { get; }
}
```

### 4.3 Low-Level Writer API (AjisFileWriter class)

```csharp
public class AjisFileWriter : IAsyncDisposable
{
    public AjisFileWriter(string path, FileMode mode = FileMode.Create);
    
    // Write segments
    public Task WriteSegmentAsync(AjisSegment segment);
    public Task WriteSegmentsAsync(IAsyncEnumerable<AjisSegment> segments);
    
    // Flush and finalize
    public Task FlushAsync();
    public Task FinalizeAsync();
}
```

### 4.4 File Indexing API (Optional Advanced)

```csharp
public class AjisFileIndex
{
    public void BuildIndex<T>(string filePath);
    public IndexEntry? FindEntry(int objectIndex);
    public List<IndexEntry> SearchByProperty(string propertyName, object value);
}

public struct IndexEntry
{
    public int ObjectIndex { get; }
    public long ByteOffset { get; }
    public long ByteLength { get; }
}
```

---

## 5. Completeness Checklist

### 5.1 Functional Requirements

- [ ] AjisFileReader for sequential segment reading
- [ ] AjisFileWriter for sequential segment writing
- [ ] Create operation (write new file)
- [ ] Append operation (add to end)
- [ ] Read operation (stream from file)
- [ ] Update operation (replace at index)
- [ ] Delete operation (rebuild without item)
- [ ] Enumeration (streaming without full load)
- [ ] Error handling (corrupted files, missing files)
- [ ] Transaction safety (atomic operations)
- [ ] Temporary files for safe updates
- [ ] Memory-bounded processing guaranteed

### 5.2 Testing Requirements

- [ ] Single object append tests
- [ ] Multiple appends tests
- [ ] Read sequential tests
- [ ] Update in place tests
- [ ] Delete and rebuild tests
- [ ] Large file tests (>1GB)
- [ ] Concurrent read tests
- [ ] Error handling tests (missing file, corrupted data)
- [ ] Round-trip tests (write → read → verify)
- [ ] Performance tests (streaming vs full load)
- [ ] Index building tests (optional)

### 5.3 Documentation Requirements

- [ ] M8A specification complete
- [ ] Full XML documentation on all public types
- [ ] Usage examples (basic to advanced)
- [ ] Best practices guide
- [ ] Performance characteristics documented
- [ ] Limitations documented

---

## 6. Integration Points

### 6.1 With M7 (Mapping Layer)
- AjisFile<T> uses AjisConverter<T> for object mapping
- Seamless type-safe operations
- Attribute-based customization applies

### 6.2 With M3 (Streaming Parser)
- AjisFileReader uses ParseSegmentsAsync
- Zero-copy streaming
- Memory-bounded processing

### 6.3 With M4 (Serialization)
- AjisFileWriter uses serialization API
- Produces valid AJIS text
- Compatible with all modes (Compact, Pretty, Canonical)

---

## 7. Example Use Cases

### 7.1 Append-Only Log
```csharp
// Append events to a log file without loading entire file
AjisFile.Append<Event>("events.ajis", new Event { Timestamp = DateTime.Now, Message = "Login" });
```

### 7.2 Large Data Processing
```csharp
// Process 10GB file with constant memory usage
await foreach (var user in AjisFile.EnumerateAsync<User>("users.ajis"))
{
    // Process user (memory is bounded)
    await ProcessUserAsync(user);
}
```

### 7.3 Data Updates
```csharp
// Find and update a record
AjisFile.Update<User>("users.ajis", 42, new User { Name = "Updated" });
```

### 7.4 Filtering and Searching
```csharp
// Find all users from a city
var pragueUsers = AjisFile.Where<User>("users.ajis", u => u.City == "Prague");
```

---

## 8. References

* [16_Pipelines_and_Segments.md](./16_Pipelines_and_Segments.md) – Segment specification
* [18_Implementation_Roadmap.md](./18_Implementation_Roadmap.md) – M8A definition
* `src/Afrowave.AJIS.Streaming/` – Parser and serializer APIs
* `src/Afrowave.AJIS.IO/` – (Stub) Placeholder for file I/O implementations

---

## 9. Status

**M8A Status:** IN PROGRESS

- [x] Specification drafted
- [ ] Current infrastructure analyzed
- [ ] AjisFileReader designed and implemented
- [ ] AjisFileWriter designed and implemented
- [ ] CRUD operations implemented
- [ ] File indexing (optional) implemented
- [ ] Comprehensive tests added
- [ ] All tests passing
- [ ] Documentation complete

---

## 10. Performance Targets

- **Memory usage:** Constant regardless of file size (streaming bounded)
- **Read performance:** >100MB/s for sequential reads
- **Write performance:** >50MB/s for streaming writes
- **Append:** O(1) operation (no file rebuild needed)
- **Update:** O(filesize) worst-case (may require rebuild)
- **Delete:** O(filesize) worst-case (rebuild without deleted items)

---

**Next Step:** Move to Step 2 - Analyze current file I/O infrastructure.
