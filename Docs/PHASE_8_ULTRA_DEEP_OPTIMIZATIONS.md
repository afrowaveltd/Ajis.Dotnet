# 🎯 PHASE 8 - ULTRA DEEP OPTIMIZATIONS

## Benchmark Výsledky Analýza (1M records)

### Současný Stav
```
PARSER:
  AJIS: 2,111ms | 182MB | Gen0=62 | Gen1=19
  STJ:    713ms |  99MB | Gen0=14 | Gen1=6
  Gap:    2.96x slower, 1.84x more memory, 4.43x more GC

SERIALIZER:
  AJIS: 1,206ms | 387MB | Gen0=22 | Gen1=0
  STJ:    414ms | 384MB | Gen0=0  | Gen1=0 ← ZERO GC!
  Gap:    2.91x slower, 1.01x memory, INFINITE GC ratio
```

---

## 🚨 NOVÉ KRITICKÉ ZPOMALOVAČE

### ❌ ZPOMALOVAČ #9: Constructor.Invoke() Reflection
**Místo**: `Utf8DirectDeserializer.ReadObject()`
```csharp
var instance = ctor.Invoke(null);  // ← REFLECTION!
```

**Dopad**:
- Reflection pro každý objekt creation
- ~10-20ns overhead per object
- Pro 1M objects = 10-20ms overhead

**STJ Řešení**:
```csharp
// STJ používá compiled delegate nebo IL.Emit
private static readonly Func<object> _activator = 
    () => new T();  // Direct instantiation!
```

**Oprava**: Compiled activator delegate místo ConstructorInfo.Invoke

---

### ❌ ZPOMALOVAČ #10: Encoding.UTF8.GetBytes() v Deserialize
**Místo**: `AjisConverter.Deserialize(string)`
```csharp
var utf8Bytes = Encoding.UTF8.GetBytes(ajisText);  // ← ALLOCATION!
return DeserializeFromUtf8(utf8Bytes);
```

**Dopad**:
- Alokuje celý nový byte[] (65MB pro 1M records!)
- Kopíruje string → bytes
- GC pressure

**STJ Řešení**:
```csharp
// STJ deserializuje přímo z string nebo ReadOnlySpan<char>
// Nebo používá ArrayPool<byte> pro dočasný buffer
public T Deserialize(string json) => 
    DeserializeCore(json.AsSpan());  // Bez allocation!
```

**Oprava**: ArrayPool<byte> nebo přímá deserializace z ReadOnlySpan<char>

---

### ❌ ZPOMALOVAČ #11: PropertyMapper.GetProperties() Per-Instance Cache Miss
**Místo**: `Utf8DirectDeserializer.ReadObject()`
```csharp
if (!_propertyLookupCache.TryGetValue(targetType, out var propertyLookup))
{
    var properties = _propertyMapper.GetProperties(targetType).ToArray();
    // ...
}
```

**Dopad**:
- Každý Utf8DirectDeserializer instance má vlastní cache
- Cache miss při prvním použití nové instance
- Reflexe GetProperties() se volá znovu

**STJ Řešení**:
```csharp
// STJ má GLOBAL static cache
private static readonly ConcurrentDictionary<Type, PropertyMetadata[]> s_cache = new();
// Cache je sdílený across all serializer instances
```

**Oprava**: Static global cache místo instance cache

---

### ❌ ZPOMALOVAČ #12: Dictionary<string, PropertyMetadata> Allocation
**Místo**: `Utf8DirectDeserializer.ReadObject()`
```csharp
propertyLookup = new Dictionary<string, PropertyMetadata>(
    properties.Length, 
    StringComparer.Ordinal);
```

**Dopad**:
- Nový Dictionary per type per deserializer instance
- 100+ bytes allocation per type
- GC pressure pro velké počty různých typů

**STJ Řešení**:
```csharp
// STJ používá FrozenDictionary<string, PropertyMetadata> (.NET 8+)
// Nebo static readonly Dictionary s permanent cache
private static readonly FrozenDictionary<string, PropertyMetadata> s_lookup = 
    properties.ToFrozenDictionary(p => p.Name);
```

**Oprava**: FrozenDictionary (.NET 10!) nebo static readonly cache

---

### ❌ ZPOMALOVAČ #13: ArrayBufferWriter Initial Size (Serializer)
**Místo**: `Utf8DirectSerializer.Serialize()`
```csharp
var bufferWriter = new ArrayBufferWriter<byte>(64 * 1024);  // 64KB
```

**Dopad**:
- Pro velké objekty (1M records = 65MB JSON) se buffer rozšiřuje 1000x
- Každé resize = nová alokace + kopírování
- GC pressure (22 Gen0 collections!)

**STJ Řešení**:
```csharp
// STJ používá ArrayPool<byte> s dynamic sizing
// Nebo IBufferWriter<byte> s recyclable buffers
private static readonly ArrayPool<byte> s_pool = ArrayPool<byte>.Shared;
var buffer = s_pool.Rent(estimatedSize);  // Pooled!
```

**Oprava**: ArrayPool<byte> místo ArrayBufferWriter nebo lepší initial size estimation

---

### ❌ ZPOMALOVAČ #14: FirstOrDefault() LINQ Query (Case-Insensitive Fallback)
**Místo**: `Utf8DirectDeserializer.ReadObject()`
```csharp
property = props.FirstOrDefault(p => 
    p.AjisKey.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
```

**Dopad**:
- LINQ query alokuje enumerator
- Iteruje všechny properties lineárně
- ~O(n) complexity pro každý property lookup miss

**STJ Řešení**:
```csharp
// STJ má precomputed case-insensitive lookup dictionary
private static readonly Dictionary<string, PropertyMetadata> s_caseInsensitive = 
    properties.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
// O(1) lookup i pro case-insensitive
```

**Oprava**: Druhý Dictionary s OrdinalIgnoreCase místo LINQ query

---

### ❌ ZPOMALOVAČ #15: List<object?> Boxing v ReadArray
**Místo**: `Utf8DirectDeserializer.ReadArray()`
```csharp
var itemsList = new List<object?>(capacity: 16);
// ...
itemsList.Add(item);  // Boxing pokud item je value type!
```

**Dopad**:
- Každý value type item se boxuje při Add()
- Pro array s miliony int/long = obrovský GC pressure
- Gen0 collections explode!

**STJ Řešení**:
```csharp
// STJ používá generic List<T> kde T je známý compile-time
// Nebo ArrayPool<T> pro dočasné buffers
if (elementType == typeof(int))
{
    var list = new List<int>();  // No boxing!
    list.Add(reader.GetInt32());
}
```

**Oprava**: Type-specific List<T> místo List<object?>

---

## 📊 Odhad Dopadu Nových Zpomalovačů

| # | Zpomalovač | CPU Dopad | Memory Dopad | Priority |
|---|-----------|----------|-------------|----------|
| 9 | Constructor.Invoke | 5-10% | - | HIGH |
| 10 | Encoding.UTF8.GetBytes | 10-15% | 15-20% | **CRITICAL** |
| 11 | Per-Instance Cache | 8-12% | 10-15% | HIGH |
| 12 | Dictionary Allocation | 3-5% | 5-8% | MEDIUM |
| 13 | ArrayBufferWriter Resize | 15-20% | 20-25% | **CRITICAL** |
| 14 | FirstOrDefault LINQ | 5-8% | 2-3% | MEDIUM |
| 15 | List<object?> Boxing | 12-18% | **30-40%** | **CRITICAL** |

**CELKEM**: ~58-88% CPU + ~82-111% Memory improvement potential

---

## ✅ PHASE 8 Optimalizační Plán

### 8A: Compiled Object Activator (HIGH PRIORITY)
```csharp
private static class ActivatorCache<T> where T : new()
{
    public static readonly Func<T> Create = () => new T();
}

// Použití
var instance = ActivatorCache<TTarget>.Create();  // No reflection!
```

### 8B: ArrayPool<byte> pro Deserialize String → UTF8 (CRITICAL)
```csharp
public T? Deserialize(string ajisText)
{
    var byteCount = Encoding.UTF8.GetByteCount(ajisText);
    var buffer = ArrayPool<byte>.Shared.Rent(byteCount);
    try
    {
        var written = Encoding.UTF8.GetBytes(ajisText, buffer);
        return DeserializeFromUtf8(buffer.AsSpan(0, written));
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

### 8C: Global Static Property Cache (HIGH PRIORITY)
```csharp
internal static class GlobalPropertyCache
{
    private static readonly ConcurrentDictionary<Type, PropertyMetadata[]> s_properties = new();
    private static readonly ConcurrentDictionary<Type, FrozenDictionary<string, PropertyMetadata>> s_lookupExact = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyMetadata>> s_lookupInsensitive = new();
    
    public static PropertyMetadata[] GetProperties(Type type) => 
        s_properties.GetOrAdd(type, PropertyMapper.DiscoverProperties);
}
```

### 8D: FrozenDictionary Property Lookup (.NET 10!)
```csharp
// Místo Dictionary, použij FrozenDictionary
using System.Collections.Frozen;

propertyLookup = properties.ToFrozenDictionary(
    p => p.AjisKey, 
    StringComparer.Ordinal);  // Immutable, faster lookup!
```

### 8E: ArrayPool<byte> místo ArrayBufferWriter (CRITICAL)
```csharp
public string Serialize(T value)
{
    var estimatedSize = EstimateSize(value);  // Smart estimation
    var buffer = ArrayPool<byte>.Shared.Rent(estimatedSize);
    try
    {
        var writer = new Utf8JsonWriter(new ArrayBufferWriter<byte>(buffer));
        WriteValue(writer, value, ...);
        return Encoding.UTF8.GetString(buffer, 0, writer.BytesCommitted);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

### 8F: Type-Specific List<T> místo List<object?> (CRITICAL)
```csharp
private object? ReadArray(ref Utf8JsonReader reader, Type targetType)
{
    var elementType = GetElementType(targetType);
    
    // Fast paths s generic List<T>
    if (ReferenceEquals(elementType, typeof(int)))
        return ReadArray<int>(ref reader);
    if (ReferenceEquals(elementType, typeof(string)))
        return ReadArray<string>(ref reader);
    // ... atd
    
    // Generic method
    private T[] ReadArray<T>(ref Utf8JsonReader reader)
    {
        var list = new List<T>();  // No boxing!
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add((T)ReadValue(ref reader, typeof(T)));
        }
        return list.ToArray();
    }
}
```

### 8G: Precomputed Case-Insensitive Lookup
```csharp
// Místo FirstOrDefault LINQ
private static readonly Dictionary<string, PropertyMetadata> s_caseInsensitiveLookup = 
    properties.ToDictionary(p => p.AjisKey, StringComparer.OrdinalIgnoreCase);

// Fast lookup
if (!propertyLookup.TryGetValue(propertyName, out var property))
{
    s_caseInsensitiveLookup.TryGetValue(propertyName, out property);
}
```

---

## 🎯 Očekávaný Výkon Po PHASE 8

| Metrika | Dnes | Po PHASE 8 | STJ | Gap |
|---------|------|-----------|-----|-----|
| **Parser** | 2,111ms | **800-900ms** | 713ms | **1.12-1.26x** |
| **Serializer** | 1,206ms | **450-550ms** | 414ms | **1.09-1.33x** |
| **Gen0 (Parser)** | 62x | **15-18x** | 14x | **1.07-1.29x** |
| **Gen0 (Serializer)** | 22x | **2-4x** | 0x | Still gap |
| **Memory (Parser)** | 182MB | **100-120MB** | 99MB | **1.01-1.21x** |
| **Memory (Serializer)** | 387MB | **390-400MB** | 384MB | **1.02-1.04x** |

**Cíl**: Dostat se na **1.1-1.3x gap** vs STJ (acceptable pro AJIS features!)

---

## 💡 Klíčové Poznatky

1. **STJ má 0 Gen0 na serializeru** → ArrayPool perfection
2. **List<object?> boxing je OBROVSKÝ problém** → 30-40% memory!
3. **ArrayBufferWriter resizing** → 20-25% overhead
4. **Constructor.Invoke reflection** → každý object creation
5. **Per-instance cache** → místo global static cache

**PHASE 8 by měla dostat AJIS velmi blízko STJ výkonu!**

---

**Status**: Analýza hotova, ready for implementation
**Priority**: CRITICAL optimizations first (#10, #13, #15)
**Expected**: 2.96x → 1.1-1.3x gap closing
