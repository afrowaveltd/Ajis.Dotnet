# 📦 LEGACY PARSERS INTEGRATION STATUS

> **Goal:** Integrate old parsers from Tools_extracted for comparison  
> **Status:** ⏸️ PARTIALLY COMPLETE (current benchmark works without them)  
> **Reason:** Namespace conflicts need manual resolution  

---

## ✅ COMPLETED

1. **Copied files** to `benchmarks/Afrowave.AJIS.Benchmarks/Legacy/OldParsers/`:
   - AjisUtf8Parser.cs
   - AjisParser.cs
   - AjisParallelParser.cs
   - AjisValue.cs
   - AjisValueType.cs
   - AjisParserOptions.cs
   - AjisParseException.cs
   - AjisException.cs

2. **Created PowerShell script** (FixNamespaces.ps1) to:
   - Change namespace to `Afrowave.AJIS.Benchmarks.Legacy.OldParsers`
   - Rename types with "Old" prefix (OldAjisValue, etc.)
   - Avoid conflicts with current implementation

3. **Created BestOfBreedBenchmark.cs** that tests:
   - ✅ Current FastDeserializer
   - ✅ System.Text.Json
   - ✅ Newtonsoft.Json
   - ⏸️ Legacy parsers (commented out for now)

---

## ⏸️ BLOCKED

**Problem:** Namespace changes didn't apply correctly

**Symptoms:**
- OldAjisValue not found
- OldAjisUtf8Parser not found
- Compilation errors

**Root cause:** PowerShell script may not have executed properly

---

## 🔧 TO FIX (Manual Steps)

### Option 1: Manual namespace fix
1. Open each file in `Legacy/OldParsers/`
2. Change `namespace Afrowave.AJIS;` → `namespace Afrowave.AJIS.Benchmarks.Legacy.OldParsers;`
3. Rename types:
   - `AjisValue` → `OldAjisValue`
   - `AjisValueType` → `OldAjisValueType`
   - `AjisParser` → `OldAjisParser`
   - `AjisUtf8Parser` → `OldAjisUtf8Parser`
   - etc.

### Option 2: Add Tools_extracted as project reference
```xml
<ProjectReference Include="..\..\src\Tools_extracted\Afrowave.AJIS.Core.csproj" />
```
Then use fully qualified names in benchmarks.

### Option 3: Skip for now
- Current benchmark works without legacy parsers
- Compare Phase 3 vs STJ vs Newtonsoft
- Add legacy parsers later if needed

---

## 🎯 CURRENT STATUS

**Working benchmark:**
```sh
dotnet run --project benchmarks/Afrowave.AJIS.Benchmarks best
```

**Tests:**
- Current FastDeserializer (Phase 3 opts)
- System.Text.Json (baseline)
- Newtonsoft.Json (baseline)
- Current AjisConverter serializer
- System.Text.Json serializer
- Newtonsoft serializer

**Scales:** 10K, 100K, 1M records

---

## 📊 PRIORITY

**Low priority** - we can already compare Phase 3 against industry standards (STJ/Newtonsoft).

If Phase 3 is competitive with STJ/Newtonsoft, we're DONE! ✅

If not, we can revisit legacy parsers for ideas.

---

**Recommendation:** Run current benchmark first, see results, then decide if legacy parser integration is worth the effort.
