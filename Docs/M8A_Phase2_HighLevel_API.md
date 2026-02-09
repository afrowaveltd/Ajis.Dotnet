# M8A Phase 2 – High-Level CRUD API

> **Status:** IN PROGRESS → COMPLETION
>
> This document defines the high-level AjisFile static API for simple, intuitive file operations.

---

## 1. Design Goals

### 1.1 Simplicity
Users should be able to perform CRUD operations with minimal code:
```csharp
// Create
AjisFile.Create("users.ajis", new[] { user1, user2 });

// Read all
var users = await AjisFile.ReadAllAsync<User>("users.ajis");

// Append
AjisFile.Append("users.ajis", newUser);

// Update
AjisFile.Update("users.ajis", index, updatedUser);

// Delete
AjisFile.Delete("users.ajis", index);
```

### 1.2 Memory Efficiency
All operations use streaming where possible:
- Enumeration doesn't load entire file
- Updates only rebuild file once
- Deletes preserve streaming

### 1.3 Type Safety
Full integration with M7 (AjisConverter<T>):
- Automatic object-to-AJIS conversion
- Automatic AJIS-to-object conversion
- Type checking and validation

### 1.4 Flexibility
Support multiple file formats:
- Array-based: `[{item1}, {item2}, ...]`
- Object-based: `{"items": [{item1}, {item2}, ...]}`

---

## 2. AjisFile Static API

### 2.1 Create Operations

```csharp
/// <summary>
/// Creates a new AJIS file with an array of objects.
/// Overwrites if file exists.
/// </summary>
public static void Create<T>(string filePath, IEnumerable<T> items)
    where T : notnull;

/// <summary>
/// Creates a new AJIS file with an array of objects asynchronously.
/// Streams objects without materializing entire collection.
/// </summary>
public static Task CreateAsync<T>(string filePath, IAsyncEnumerable<T> items)
    where T : notnull;
```

### 2.2 Read Operations

```csharp
/// <summary>
/// Reads all objects from an AJIS file and returns them as a list.
/// WARNING: Materializes entire file in memory. Use EnumerateAsync for large files.
/// </summary>
public static List<T> ReadAll<T>(string filePath)
    where T : notnull;

/// <summary>
/// Reads all objects from an AJIS file asynchronously as an enumerable.
/// Does NOT materialize entire file - streams objects one at a time.
/// </summary>
public static IAsyncEnumerable<T> ReadAllAsync<T>(string filePath)
    where T : notnull;

/// <summary>
/// Reads a single object at a specific index from an AJIS file.
/// Requires index lookup (O(n) worst case without index).
/// </summary>
public static T? ReadAt<T>(string filePath, int index)
    where T : notnull;

/// <summary>
/// Reads a single object at a specific index asynchronously.
/// </summary>
public static Task<T?> ReadAtAsync<T>(string filePath, int index)
    where T : notnull;
```

### 2.3 Enumeration Operations

```csharp
/// <summary>
/// Enumerates all objects from an AJIS file synchronously.
/// Use this for processing large files efficiently.
/// </summary>
public static IEnumerable<T> Enumerate<T>(string filePath)
    where T : notnull;

/// <summary>
/// Enumerates all objects from an AJIS file asynchronously.
/// Streams objects without full materialization.
/// </summary>
public static IAsyncEnumerable<T> EnumerateAsync<T>(string filePath)
    where T : notnull;
```

### 2.4 Write/Append Operations

```csharp
/// <summary>
/// Appends a single object to the end of an AJIS array file.
/// Creates file if it doesn't exist.
/// </summary>
public static void Append<T>(string filePath, T item)
    where T : notnull;

/// <summary>
/// Appends a single object to an AJIS file asynchronously.
/// </summary>
public static Task AppendAsync<T>(string filePath, T item)
    where T : notnull;

/// <summary>
/// Appends multiple objects to an AJIS file.
/// </summary>
public static void AppendMany<T>(string filePath, IEnumerable<T> items)
    where T : notnull;

/// <summary>
/// Appends multiple objects to an AJIS file asynchronously.
/// </summary>
public static Task AppendManyAsync<T>(string filePath, IAsyncEnumerable<T> items)
    where T : notnull;
```

### 2.5 Update Operations

```csharp
/// <summary>
/// Replaces an object at a specific index in an AJIS file.
/// Rebuilds the file - O(n) operation.
/// </summary>
public static void Update<T>(string filePath, int index, T item)
    where T : notnull;

/// <summary>
/// Replaces an object at a specific index asynchronously.
/// </summary>
public static Task UpdateAsync<T>(string filePath, int index, T item)
    where T : notnull;

/// <summary>
/// Finds and replaces the first object matching a predicate.
/// Returns true if object was found and replaced.
/// </summary>
public static bool UpdateFirst<T>(string filePath, Func<T, bool> predicate, T replacement)
    where T : notnull;

/// <summary>
/// Finds and replaces all objects matching a predicate.
/// Returns count of replaced objects.
/// </summary>
public static int UpdateAll<T>(string filePath, Func<T, bool> predicate, T replacement)
    where T : notnull;
```

### 2.6 Delete Operations

```csharp
/// <summary>
/// Deletes an object at a specific index from an AJIS file.
/// Rebuilds the file - O(n) operation.
/// </summary>
public static void Delete<T>(string filePath, int index)
    where T : notnull;

/// <summary>
/// Deletes an object at a specific index asynchronously.
/// </summary>
public static Task DeleteAsync<T>(string filePath, int index)
    where T : notnull;

/// <summary>
/// Deletes the first object matching a predicate.
/// Returns true if object was found and deleted.
/// </summary>
public static bool DeleteFirst<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;

/// <summary>
/// Deletes all objects matching a predicate.
/// Returns count of deleted objects.
/// </summary>
public static int DeleteAll<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;

/// <summary>
/// Deletes all objects from an AJIS file (clears it).
/// </summary>
public static void Clear<T>(string filePath)
    where T : notnull;
```

### 2.7 Query Operations

```csharp
/// <summary>
/// Counts total objects in an AJIS file.
/// Streams through file - O(n).
/// </summary>
public static int Count<T>(string filePath)
    where T : notnull;

/// <summary>
/// Counts objects matching a predicate.
/// Streams through file - O(n).
/// </summary>
public static int CountWhere<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;

/// <summary>
/// Finds first object matching a predicate.
/// Returns null if not found.
/// </summary>
public static T? Find<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;

/// <summary>
/// Finds all objects matching a predicate.
/// Streams results without materializing entire file.
/// </summary>
public static IEnumerable<T> Where<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;

/// <summary>
/// Finds all objects matching a predicate asynchronously.
/// </summary>
public static IAsyncEnumerable<T> WhereAsync<T>(string filePath, Func<T, bool> predicate)
    where T : notnull;
```

### 2.8 Configuration

```csharp
/// <summary>
/// Sets the default AJIS settings for all AjisFile operations.
/// </summary>
public static void SetDefaultSettings(AjisSettings? settings);

/// <summary>
/// Sets the default converter factory for type T.
/// </summary>
public static void SetConverterFactory<T>(Func<AjisConverter<T>> factory)
    where T : notnull;
```

---

## 3. Implementation Strategy

### 3.1 Converter Caching
- Cache converters per type for performance
- Use default PascalCase naming policy
- Allow override via SetConverterFactory

### 3.2 File Format Detection
- Assume array format for most files: `[{...}, {...}]`
- Detect and handle object format if needed
- Provide explicit parameters for non-standard formats

### 3.3 Update/Delete Strategy
- Read file into memory (necessary for safe rebuild)
- Modify objects list
- Write to temporary file
- Atomic swap (rename temp → original)

### 3.4 Performance Considerations
- Enumeration: O(1) memory, O(n) time
- Read: O(n) memory, O(n) time
- Append: O(1) memory, O(1) time (just append)
- Update: O(n) memory, O(n) time (must rebuild)
- Delete: O(n) memory, O(n) time (must rebuild)

---

## 4. Error Handling

- **FileNotFoundException**: File doesn't exist
- **FormatException**: AJIS is malformed
- **InvalidOperationException**: Cannot parse objects
- **ArgumentOutOfRangeException**: Index out of bounds
- **IOException**: File access errors

All exceptions include clear, actionable messages with file paths and operations.

---

## 5. Integration with M7

The API uses AjisConverter<T> for all object mapping:

```csharp
// Internal implementation pattern
public static async IAsyncEnumerable<T> ReadAllAsync<T>(string filePath)
{
    var converter = new AjisConverter<T>();
    using (var reader = new AjisFileReader(filePath))
    {
        var stream = reader.OpenAsStream();
        var segments = AjisParse.ParseSegmentsAsync(stream);
        // Convert segments to objects via converter
        // Yield objects one at a time
    }
}
```

---

## 6. Completeness Checklist

### 6.1 API Methods
- [ ] Create (sync + async)
- [ ] ReadAll (sync + async)
- [ ] ReadAt (sync + async)
- [ ] Enumerate (sync + async)
- [ ] Append (single + many, sync + async)
- [ ] Update (at index, by predicate, sync + async)
- [ ] Delete (at index, by predicate, clear, sync + async)
- [ ] Count (all + where, sync)
- [ ] Find (single + where, sync + async)
- [ ] Configuration methods

### 6.2 Features
- [ ] Converter caching
- [ ] File format detection
- [ ] Safe atomic updates/deletes
- [ ] Error handling
- [ ] Streaming for large files
- [ ] Full XML documentation

### 6.3 Testing
- [ ] CRUD operation tests
- [ ] Enumeration tests
- [ ] Large file tests
- [ ] Error handling tests
- [ ] Integration with M7
- [ ] Performance tests

---

## 7. References

* M8A Phase 1 (File Reader/Writer)
* M7 (AjisConverter for object mapping)
* M3 (Streaming parser for segments)
* M4 (Serialization for writing objects)

---

**Next Step:** Implement AjisFile high-level API.
