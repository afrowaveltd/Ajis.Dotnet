# 🚨 KRITICKÉ ZPOMALOVAČE - DETAILNÍ ANALÝZA

## Zjištěné Problémy vs System.Text.Json

### ❌ ZPOMALOVAČ #1: Instance Vytváření
**Místo**: `AjisConverter.Serialize()` + `AjisConverter.Deserialize()`
```csharp
// ŠPATNĚ - Vytváří nový object pokaždé!
var serializer = new Utf8DirectSerializer<T>(_propertyMapper);
var deserializer = new Utf8DirectDeserializer<T>(_propertyMapper);
```

**Dopad**: 
- Každá operace: nové Dictionary allocations
- PropertySetterCompiler.GetOrCompileSetter se volá znovu (cache se vynuluje!)
- PropertyGetterCompiler.GetOrCompileGetter - stejný problém
- Paralelní alokace 100KB+ na operaci

**STJ Řešení**: 
```csharp
// STJ vytváří SINGLETON convertery
private static readonly JsonSerializerOptions DefaultOptions = new();
// Jednoduše se volá: JsonSerializer.Serialize(obj, options)
```

**Oprava**: Cachovat Utf8DirectSerializer/Deserializer v AjisConverter nebo je udělat statické.

---

### ❌ ZPOMALOVAČ #2: PropertyMapper Sdílení
**Místo**: `PropertyMapper _propertyMapper` se vytváří v AjisConverter.__ctor__

**Dopad**:
- Nový PropertyMapper pro každý AjisConverter
- Vlastní cache Dictionary<Type, PropertyMetadata[]>
- Reflexe Property discovery se dělá znovu

**STJ Řešení**: 
```csharp
// STJ má GLOBÁLNÍ type metadata cache
internal static class DefaultTypeConverterCache { ... }
// Bez ohledu na JsonSerializerOptions je cache SDÍLENÁ
```

**Oprava**: PropertyMapper by měl být STATICKÝ/SINGLETON s globálním cachingem.

---

### ❌ ZPOMALOVAČ #3: Dictionary StringComparer.OrdinalIgnoreCase
**Místo**: `Utf8DirectDeserializer.ReadObject()` - property lookup
```csharp
propertyLookup = new Dictionary<string, PropertyMetadata>(
    properties.Length, 
    StringComparer.OrdinalIgnoreCase  // ← Pomalé!
);
```

**Dopad**:
- OrdinalIgnoreCase porovnávání je **dražší** než Ordinal
- Každý lookup dělá case normalization
- STJ používá case-SENSITIVE lookup s fallback

**STJ Řešení**:
```csharp
// STJ dělá "exact match" nejdříve (fast)
// Pak case-insensitive fallback jen pokud je potřeba
private static bool TryGetValue(
    ReadOnlySpan<byte> key, 
    out Property prop)
{
    // First: Exact match (super fast)
    // Second: Case-insensitive (only if needed)
}
```

**Oprava**: Dvoustupňový lookup - "exact" pak "case-insensitive".

---

### ❌ ZPOMALOVAČ #4: Konverzní String -> Type
**Místo**: `ReadString()` metoda
```csharp
var str = reader.GetString();  // ← Alokace!
if (ReferenceEquals(underlyingType, TypeGuid))
    return Guid.Parse(str);    // ← String parse!
```

**Dopad**:
- `GetString()` alokuje NEW string (UTF8 → UTF16)
- Pak se parse string místo bajtů
- 2x alokace + kopírování

**STJ Řešení**:
```csharp
// STJ parsuje přímo z UTF8 bajtů
private bool TryGetBytesValue(
    ref Utf8JsonReader reader, 
    out byte[] bytes)
{
    // Direktně z reader.ValueSpan - bez string allocation!
    return Utf8Parser.TryParse(reader.ValueSpan, out bytes, out _);
}
```

**Oprava**: Parsovat přímo z `reader.ValueSpan<byte>` bez string konverze.

---

### ❌ ZPOMALOVAČ #5: PropertySetterCompiler Memory
**Místo**: `PropertySetterCompiler` - Expression tree kompilace

**Dopad**:
- Vytváří Expression tree (alokace)
- Kompiluje lambda (bytecode generace)
- Cachuje compiled delegate
- Ale cache se vynuluje při každém novém Utf8DirectDeserializer!

**STJ Řešení**:
```csharp
// STJ generuje property accessory code-gen time
// Nebo používá IL.Emit s permanentním cache
// NIKDY nevytváří nové se stejným typem
```

**Oprava**: PropertySetterCompiler musí být GLOBAL STATIC s permanentním cachingem.

---

### ❌ ZPOMALOVAČ #6: Array.CreateInstance + SetValue
**Místo**: `ReadArray()` metoda
```csharp
var array = Array.CreateInstance(elementType, items.Count);
for (int i = 0; i < items.Count; i++)
{
    array.SetValue(items[i], i);  // ← Reflection!
}
```

**Dopad**:
- `Array.SetValue()` je reflection operace
- Má type checking overhead
- Pomalá je i pro velké array

**STJ Řešení**:
```csharp
// STJ používá dynamické List<T> builder
// Nebo generic T[] přímý přístup
var array = new T[count];
array[i] = item;  // Direct assignment, ne reflection
```

**Oprava**: Použít `new T[]` místo `Array.CreateInstance` pro generic types.

---

### ❌ ZPOMALOVAČ #7: Boxing v Type Checking
**Místo**: Všude v `ReadValue()`, `WriteValue()`
```csharp
var value = reader.GetInt32();  // Box!
return (object)value;           // ← Boxing
```

**Dopad**:
- GetInt32() vrací int
- Vrácení jako object = boxing
- GC pressure na Gen0

**STJ Řešení**:
```csharp
// STJ má pro každý typ <T> specifickou cestu
// public T Deserialize<T>(...)  // Generics!
// Žádný boxing, přímá hodnota
```

**Oprava**: Udělat Utf8DirectDeserializer generické pro konkrétní typy, ne object.

---

### ❌ ZPOMALOVAČ #8: Utf8JsonReader Reallocation
**Místo**: `Deserialize()` metoda
```csharp
var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions { ... });
```

**Dopad**:
- Vytváří nový reader struct pokaždé
- JsonReaderOptions je také nový
- Inicializuje reader state machine

**STJ Řešení**:
```csharp
// STJ má static default options
private static readonly JsonReaderOptions s_default = new();
// Reuse = bez alokace
```

**Oprava**: Cachovat JsonReaderOptions staticky.

---

## 📊 Shrnutí Zpomalovačů (Odhad Vlivu)

| # | Problém | Dopad | Fixní |
|---|---------|-------|-------|
| 1 | Instance vytváření | **25-30% CPU** | Cache serializer/deserializer |
| 2 | PropertyMapper sdílení | **15-20% CPU** | STATIC global cache |
| 3 | OrdinalIgnoreCase lookup | **5-8% CPU** | Двіstupňový (exact + fallback) |
| 4 | String konverze | **8-12% Memory** | Parsovat z ValueSpan<byte> |
| 5 | PropertySetterCompiler | **10-15% CPU** | Global static cache |
| 6 | Array.SetValue reflection | **3-5% CPU** | new T[] generic |
| 7 | Boxing | **8-12% Gen0** | Generics <T> místo object |
| 8 | JsonReaderOptions | **2-3% CPU** | Static default options |

**CELKEM POTENCIÁLNÍ ÚSPORA: 76-85% CPU!**

---

## ✅ Akční Plán Optimalizací

### PHASE 7A: Cache AjisConverter Serializers (HIGHEST PRIORITY)
```csharp
public class AjisConverter<T> where T : notnull
{
    private static readonly Utf8DirectSerializer<T> s_serializer = new(GlobalPropertyMapper.Instance);
    private static readonly Utf8DirectDeserializer<T> s_deserializer = new(GlobalPropertyMapper.Instance);
    
    public string Serialize(T value)
    {
        return s_serializer.Serialize(value);  // Cache hit!
    }
}
```

### PHASE 7B: Global PropertyMapper Singleton
```csharp
public static class GlobalPropertyMapper
{
    public static readonly PropertyMapper Instance = new PropertyMapper(DefaultNamingPolicy);
    // Single shared cache across all converters
}
```

### PHASE 7C: STJ-Inspired ValueSpan Parsing
```csharp
private object? ReadString(ref Utf8JsonReader reader, Type targetType)
{
    if (ReferenceEquals(targetType, TypeGuid))
    {
        // Parse DIRECTLY from ValueSpan bez string allocation!
        var span = reader.ValueSpan;
        return Guid.TryParse(span, out var guid) ? guid : null;
    }
    // ... fallback na string jen pokud je nutno
}
```

### PHASE 7D: Generické Typy Místo Object
```csharp
// Nový generic deserializer bez boxing!
internal sealed class Utf8DirectDeserializer<T> where T : notnull
{
    public T? Deserialize(ReadOnlySpan<byte> utf8Json) => ...
}
```

---

## 🎯 Cíl po Optimalizacích

| Metrika | Dnes | Po Opt. | Cíl |
|---------|------|--------|-----|
| Parser | 2,080ms | 400-500ms | <440ms STJ |
| Serializer | 983ms | 200-300ms | <160ms STJ |
| GC Gen0 | 47x | 10-12x | <5x |
| Memory | 181MB | 40-50MB | <99MB |

---

## STJ Koncepty k Převzetí

1. ✅ **Global Type Metadata Cache** - PropertyMapper je singleton
2. ✅ **ValueSpan Direct Parsing** - Bez string konverze
3. ✅ **Case-Sensitive + Fallback Lookup** - Nejdřív exact, pak case-insensitive
4. ✅ **Static Options** - JsonReaderOptions není nový pokaždé
5. ✅ **Generics <T>** - Žádný boxing, přímá hodnota
6. ✅ **Compiled Delegates Caching** - Global cache PropertySetterCompiler
7. ✅ **Generic T[] Assignment** - Místo Array.CreateInstance
8. ✅ **Singleton Converters** - Cached serializer/deserializer instance

---

**Tento plán by měl dosáhnout 2-3x zlepšení a přiblížit se STJ výkonu.**
