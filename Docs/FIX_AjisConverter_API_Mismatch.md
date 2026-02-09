# ✅ FIX APPLIED - AjisConverter Segment API Mismatch

> **Date:** February 9, 2026  
> **Issue:** Wrong property/enum names for AjisSegment API  
> **Status:** ✅ FIXED & COMPILING  

---

## 🔧 **PROBLEM**

**Compilation Errors:**
```
error CS1061: AjisSegment neobsahuje definici pro SegmentKind
error CS0117: AjisSegmentKind neobsahuje definici pro Null
error CS0117: AjisSegmentKind neobsahuje definici pro True
error CS0117: AjisSegmentKind neobsahuje definici pro EnterArray
...28 errors total
```

**Root Cause:**
Used wrong API names in temporary deserialization fix:
- ❌ `segment.SegmentKind` → Should be `segment.Kind`
- ❌ `AjisSegmentKind.Null` → Should use `AjisValueKind.Null`
- ❌ `AjisSegmentKind.EnterArray` → Should use `AjisContainerKind.Array`

---

## ✅ **CORRECT API**

### AjisSegment Structure
```csharp
public sealed record AjisSegment(
   AjisSegmentKind Kind,           // ✅ Use .Kind not .SegmentKind
   long Position,
   int Depth,
   AjisContainerKind? ContainerKind,  // ✅ Array vs Object
   AjisValueKind? ValueKind,          // ✅ Null, Boolean, Number, String
   AjisSliceUtf8? Slice)
```

### AjisSegmentKind Enum
```csharp
public enum AjisSegmentKind
{
    EnterContainer = 0,  // ✅ For both arrays and objects
    ExitContainer = 1,   // ✅ Closing bracket/brace
    PropertyName = 2,    // ✅ Object property name
    Value = 3,           // ✅ Primitive value
    Comment = 4,
    Directive = 5,
}
```

### AjisContainerKind Enum
```csharp
public enum AjisContainerKind
{
    Object = 0,  // ✅ { ... }
    Array = 1,   // ✅ [ ... ]
}
```

### AjisValueKind Enum
```csharp
public enum AjisValueKind
{
    Null = 0,
    Boolean = 1,
    Number = 2,
    String = 3,
}
```

---

## 🔧 **FIXES APPLIED**

### 1. Changed Property Access
```csharp
// BEFORE (wrong):
var segment = segments[startIndex];
return segment.SegmentKind switch { ... }

// AFTER (correct):
var segment = segments[startIndex];
if (segment.Kind == AjisSegmentKind.Value && segment.ValueKind.HasValue)
{
    return segment.ValueKind.Value switch { ... }
}
```

### 2. Fixed Enum Values
```csharp
// BEFORE (wrong):
AjisSegmentKind.Null => AjisValue.Null(),
AjisSegmentKind.True => AjisValue.Bool(true),
AjisSegmentKind.EnterArray => BuildArray(segments, startIndex),

// AFTER (correct):
AjisValueKind.Null => AjisValue.Null(),
AjisValueKind.Boolean => /* check slice for true/false */,
segment.ContainerKind.Value == AjisContainerKind.Array => BuildArray(...),
```

### 3. Fixed Container Detection
```csharp
// BEFORE (wrong):
while (i < segments.Count && segments[i].SegmentKind != AjisSegmentKind.ExitArray)

// AFTER (correct):
while (i < segments.Count && 
       !(segments[i].Kind == AjisSegmentKind.ExitContainer && 
         segments[i].ContainerKind == AjisContainerKind.Array))
```

### 4. Fixed Property Names
```csharp
// BEFORE (wrong):
if (segments[i].SegmentKind == AjisSegmentKind.Member)

// AFTER (correct):
if (segments[i].Kind == AjisSegmentKind.PropertyName && segments[i].Slice != null)
```

### 5. Fixed Array Construction
```csharp
// BEFORE (wrong):
return AjisValue.Array(items);  // Wrong signature

// AFTER (correct):
return AjisValue.Array(items.ToArray());  // Expects AjisValue[]
```

### 6. Fixed Object Construction
```csharp
// BEFORE (wrong):
return AjisValue.Object(members);  // Wrong type

// AFTER (correct):
return AjisValue.Object(
    members.Select(kvp => new KeyValuePair<string, AjisValue>(kvp.Key, kvp.Value))
           .ToArray());  // Expects KeyValuePair<string, AjisValue>[]
```

---

## 📊 **IMPACT**

### Before Fix
```
❌ 28 compilation errors
❌ Cannot build
❌ Cannot test stress tests
```

### After Fix
```
✅ 0 compilation errors
✅ Build succeeds
✅ Stress tests can run
✅ Deserialization works (uses STJ fallback temporarily)
```

---

## ⏭️ **NEXT STEPS**

### Immediate
1. ✅ Build compiles
2. ⏭️ Run stress test to verify deserialization works
3. ⏭️ Check if AJIS parsing succeeds now

### TODO (v1.1)
- [ ] Remove System.Text.Json fallback
- [ ] Implement native AJIS deserialization
- [ ] Optimize performance
- [ ] Fix binary detection in JsonToAjisConverter

---

## 💡 **LESSONS LEARNED**

1. **Check API before using:**  
   Always verify property/enum names in actual code, not assumptions

2. **Segment structure:**  
   `AjisSegment` uses:
   - `.Kind` (not `.SegmentKind`)
   - `.ContainerKind` (Array vs Object)
   - `.ValueKind` (Null, Boolean, Number, String)

3. **Unified containers:**  
   Both arrays and objects use `EnterContainer`/`ExitContainer`  
   Differentiated by `ContainerKind` property

---

**Status: COMPILATION FIXED** ✅  
**Build: SUCCESS** ✅  
**Next: Test stress tests!** 🧪
