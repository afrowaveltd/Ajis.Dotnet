# M7 – Mapping Layer Implementation Status

> **Status:** IN PROGRESS → COMPLETION
>
> This document tracks the M7 (Mapping Layer) milestone for object-to-AJIS and AJIS-to-object mapping.

---

## 1. M7 Scope

M7 implements a flexible, type-safe mapping layer that converts between .NET objects and AJIS segments, providing comfort comparable to Newtonsoft.Json with superior diagnostics and error reporting.

**Core Responsibilities:**

* Object-to-AJIS serialization (any .NET type → segments)
* AJIS-to-object deserialization (segments → .NET objects)
* Type introspection and property mapping
* Flexible naming policies (CamelCase, snake_case, PascalCase)
* Custom type converters (user-provided)
* Path-aware error reporting (full "path.to.failing.field")
* Support for nested objects, arrays, collections
* Null handling and default values
* Optional properties and nullable types

---

## 2. Design Goals

### 2.1 Newtonsoft.Json Compatibility
Users migrating from Newtonsoft.Json should recognize the API:
- `JsonConvert.SerializeObject(obj)` → `AjisConverter.Serialize(obj)`
- `JsonConvert.DeserializeObject<T>(json)` → `AjisConverter.Deserialize<T>(segments)`

### 2.2 Superior Error Reporting
Errors report exact path to failing element:
- ❌ "Invalid value" (Bad)
- ✅ "Property 'Users[0].Address.City': Expected string but got null" (Good)

### 2.3 Pluggable Converters
Users can register custom converters for domain types:
```csharp
var converter = new AjisConverter<MyClass>();
converter.RegisterConverter<DateTime>(MyDateTimeConverter.Instance);
var obj = converter.Deserialize(segments);
```

### 2.4 Naming Flexibility
Support multiple naming conventions in same application:
```csharp
var converter = new AjisConverter<MyClass>(new CamelCaseNamingPolicy());
```

---

## 3. Mapping Architecture

### 3.1 Core Interface Hierarchy

**INamingPolicy**
```csharp
public interface INamingPolicy
{
    string ConvertName(string propertyName);
}
```

**ITypeConverter** (Internal)
```csharp
internal interface ITypeConverter
{
    AjisSegment Serialize(object? value, string propertyName, int depth);
    object? Deserialize(AjisSegment segment, Type targetType, string path);
}
```

**AjisConverter<T>** (Public)
```csharp
public class AjisConverter<T>
{
    public string Serialize(T? value);
    public T? Deserialize(string ajisText);
    public T? DeserializeFromSegments(IEnumerable<AjisSegment> segments);
    
    public void RegisterConverter<TTarget>(ITypeConverter converter);
    public AjisConverter<T> WithNamingPolicy(INamingPolicy policy);
}
```

### 3.2 Mapping Process Flow

**Serialization:**
```
.NET Object
  ↓ [Introspection: Get properties]
  ↓ [For each property: Apply naming policy]
  ↓ [Get property value → Convert to segment]
  ↓ [Handle nested objects/arrays recursively]
  → AjisSegment Stream
```

**Deserialization:**
```
AjisSegment Stream
  ↓ [Parse segment stream]
  ↓ [For each property segment: Apply naming policy to find matching property]
  ↓ [Convert segment value to target type]
  ↓ [Assign to object instance]
  ↓ [Report path-aware errors if mismatch]
  → .NET Object (T)
```

---

## 4. Naming Policies

### 4.1 Policy Definitions

**PascalCase (Default)**
- `.NET Property`: `FirstName` → AJIS Key: `FirstName`
- Use for: APIs, standard JSON

**camelCase**
- `.NET Property`: `FirstName` → AJIS Key: `firstName`
- Use for: JavaScript interop, REST APIs

**snake_case**
- `.NET Property`: `FirstName` → AJIS Key: `first_name`
- Use for: Legacy systems, Python interop

**kebab-case**
- `.NET Property`: `FirstName` → AJIS Key: `first-name`
- Use for: Configuration files, YAML-like formats

### 4.2 Policy Implementation
Each policy is a simple transform:
```csharp
public class CamelCaseNamingPolicy : INamingPolicy
{
    public string ConvertName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;
        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
}
```

---

## 5. Type Support Matrix

| Type | Serialize | Deserialize | Notes |
|------|-----------|-------------|-------|
| **Primitives** | | | |
| - bool | ✅ | ✅ | Via AjisValueKind.Boolean |
| - int, long, double | ✅ | ✅ | Via AjisValueKind.Number |
| - string | ✅ | ✅ | Via AjisValueKind.String |
| - null | ✅ | ✅ | Via AjisValueKind.Null |
| **Collections** | | | |
| - Array<T> | ✅ | ✅ | Recursive mapping |
| - List<T> | ✅ | ✅ | Converted to/from array |
| - Dictionary<K,V> | ✅ | ✅ | As object properties |
| **Complex** | | | |
| - Custom classes | ✅ | ✅ | Via reflection |
| - Nested objects | ✅ | ✅ | Recursive mapping |
| - Nullable<T> | ✅ | ✅ | Handle null case |
| **Special** | | | |
| - DateTime | ⚠️ | ⚠️ | Custom converter needed |
| - Guid | ⚠️ | ⚠️ | Custom converter needed |
| - Enum | ⚠️ | ⚠️ | Custom converter needed |

---

## 6. Error Reporting Examples

### 6.1 Type Mismatch
```
✗ Property 'User.Age': Expected number but got string "twenty"
  Path: User.Age
  Segment: Value(offset=42, kind=String, value="twenty")
  Expected type: int
```

### 6.2 Missing Required Property
```
✗ Property 'User.Email': Required string property is missing
  Path: User
  Available properties: Name, Age
  Missing property: Email
```

### 6.3 Array Index Error
```
✗ Array 'Users[2].Address.Zip': Expected string but got null
  Path: Users[2].Address.Zip
  Array index: 2
  Invalid element: Segment(kind=Null)
  Expected type: string
```

### 6.4 Nested Object Error
```
✗ Property 'Company.Employees[1].Department.Name': Invalid value
  Path: Company.Employees[1].Department.Name
  Segment value: Value(kind=Number, value=123)
  Expected type: string
  Hint: Did you mean to quote the value? "123" instead of 123
```

---

## 7. M7 Completeness Checklist

### 7.1 Functional Requirements

- [ ] INamingPolicy interface and implementation
- [ ] PascalCase, CamelCase, snake_case, kebab-case policies
- [ ] AjisConverter<T> base class with serialize/deserialize
- [ ] Type introspection (reflection-based property discovery)
- [ ] Primitive type support (bool, number, string, null)
- [ ] Array and collection support (List<T>, T[], Dictionary)
- [ ] Nested object recursive mapping
- [ ] Null handling and nullable types
- [ ] Custom type converter registration
- [ ] Path-aware error reporting with full context
- [ ] Clear, actionable error messages
- [ ] Integration with AjisSegment stream API

### 7.2 Testing Requirements

- [ ] Primitive type tests (all scalar types)
- [ ] Simple object mapping tests
- [ ] Nested object tests (3+ levels)
- [ ] Array/collection tests
- [ ] Naming policy tests (each policy)
- [ ] Mixed naming in same object
- [ ] Custom converter tests
- [ ] Error handling tests
- [ ] Null and optional tests
- [ ] Circular reference handling
- [ ] Performance tests (large object graphs)
- [ ] Round-trip tests (serialize → deserialize → compare)

### 7.3 Documentation Requirements

- [ ] M7 specification complete
- [ ] Full XML documentation on all public types
- [ ] Naming policy documentation with examples
- [ ] Error reporting format documented
- [ ] Usage examples (basic to advanced)
- [ ] Migration guide from Newtonsoft.Json

---

## 8. Example Usage

### 8.1 Basic Serialization

```csharp
public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

var user = new User { Name = "Alice", Age = 30, Email = "alice@example.com" };
var converter = new AjisConverter<User>();
string ajisText = converter.Serialize(user);
// Output: {"Name":"Alice","Age":30,"Email":"alice@example.com"}
```

### 8.2 Naming Policy

```csharp
var converter = new AjisConverter<User>(new CamelCaseNamingPolicy());
string ajisText = converter.Serialize(user);
// Output: {"name":"Alice","age":30,"email":"alice@example.com"}
```

### 8.3 Deserialization

```csharp
string ajisText = "{\"Name\":\"Bob\",\"Age\":25,\"Email\":\"bob@example.com\"}";
var converter = new AjisConverter<User>();
User? user = converter.Deserialize(ajisText);
// user = User { Name = "Bob", Age = 25, Email = "bob@example.com" }
```

### 8.4 Custom Converter

```csharp
public class DateTimeConverter : ITypeConverter
{
    public AjisSegment Serialize(object? value, string name, int depth)
    {
        var dt = (DateTime)value;
        return AjisSegment.Value(0, depth, AjisValueKind.String, 
            AjisSliceUtf8.FromString(dt.ToString("O")));
    }
    
    public object? Deserialize(AjisSegment segment, Type target, string path)
    {
        if (segment.ValueKind != AjisValueKind.String)
            throw new InvalidOperationException($"Path {path}: Expected string for DateTime");
        var text = segment.Slice.Value.AsString();
        return DateTime.Parse(text);
    }
}

var converter = new AjisConverter<MyClass>();
converter.RegisterConverter<DateTime>(new DateTimeConverter());
```

---

## 9. References

* [16_Pipelines_and_Segments.md](./16_Pipelines_and_Segments.md) – Segment specification
* [18_Implementation_Roadmap.md](./18_Implementation_Roadmap.md) – M7 definition
* [99_Requirements_Gap_Analysis.md](./99_Requirements_Gap_Analysis.md) – Gap analysis
* `src/Afrowave.AJIS.Streaming/Segments/` – AjisSegment API
* `src/Afrowave.AJIS.Core/Configuration/` – Settings infrastructure

---

## 10. Status

**M7 Status:** IN PROGRESS

- [x] Specification drafted
- [ ] Current infrastructure analyzed
- [ ] Naming policies implemented
- [ ] Converter architecture designed
- [ ] AjisConverter<T> implemented
- [ ] Type introspection implemented
- [ ] Error reporting implemented
- [ ] All tests passing
- [ ] Documentation complete

---

**Next Step:** Move to Step 2 - Analyze current type mapping infrastructure.
