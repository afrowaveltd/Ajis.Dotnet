# AJIS Settings & Mapping (C# / .NET)

> Normative design document for AjisSettings and object mapping behavior

---

## 1. Scope

This document defines:

* **AjisSettings** (configuration model)
* naming policies (camelCase, PascalCase, etc.)
* property mapping between AJIS and C#
* extensibility points for custom mapping

This document is focused on the **.NET implementation**, but concepts should remain portable.

---

## 2. Goals

AjisSettings must be:

* **easy to use** with sensible defaults
* **configurable** without heavy boilerplate
* **safe** (avoid surprising lossy conversions)
* **fast** (mapping must not dominate parse time)
* **predictable** (deterministic behavior)

---

## 3. AjisSettings – conceptual structure

AjisSettings groups configuration into small nested groups:

* `Format` (AJIS textual features)
* `Parsing` (mode, limits)
* `Serialization` (formatting, canonical)
* `Naming` (policies, case)
* `Mapping` (object model binding)
* `Diagnostics` (errors, localization)
* `Events` (progress / debug)

Settings must be immutable once an operation starts.

---

## 4. Naming policies

AJIS property names are strings.

When mapping to/from C# properties, AJIS may apply a **NamingPolicy**.

Supported policies:

* Identity (no change)

* CamelCase

* PascalCase
  n
  Rules:

* naming policy affects mapping only

* AJIS textual output is not rewritten unless serializer is configured to do so

### 4.1 Custom naming policy

Developers may provide a custom naming policy:

* `string ConvertName(string input)`

Custom policies must be deterministic.

---

## 5. Property mapping model

AJIS mapping must support:

* direct property name match
* naming policy match
* explicit override via attributes
* contract / resolver style mapping

Mapping must work in both directions:

* AJIS → object (deserialization)
* object → AJIS (serialization)

---

## 6. Attribute-based mapping

The library should provide attributes analogous to common JSON libraries.

Conceptual examples:

* `[AjisPropertyName("user_id")]`
* `[AjisIgnore]`
* `[AjisRequired]`
* `[AjisNumberFormat(AjisNumberStyle.Hex)]`

Attributes must be optional.

---

## 7. Resolver-based mapping (contract model)

For advanced scenarios, mapping should support a resolver similar to Newtonsoft:

* enumerates serializable members
* provides name mapping
* provides converters

Resolver must allow caching and be thread-safe.

---

## 8. Converters

AJIS must allow custom converters for:

* primitives
* special types (DateTime, Guid, TimeSpan)
* domain types

Converters may be registered:

* globally (settings)
* per-type
* per-property

---

## 9. Performance strategy

To keep mapping fast:

* reflection results must be cached
* property access should prefer compiled delegates
* optional source generation may be provided

### 9.1 Source generator option

A source generator may emit:

* per-type metadata
* fast read/write code

Generator must be optional and produce the same semantics as runtime mapping.

---

## 10. Mapping in streaming scenarios

In BigFiles mode:

* mapping may be performed per-item without buffering the whole array
* consumers may request `IAsyncEnumerable<T>` for arrays of objects
* partial failures may be handled according to settings

Mapping must integrate with:

* progress events
* diagnostics

---

## 11. Configuration examples (conceptual)

### 11.1 CamelCase mapping

* C# property: `UserId`
* AJIS key: `userId`

NamingPolicy: CamelCase

### 11.2 Explicit name

* C# property: `UserId`
* AJIS key: `user_id`

Attribute: `[AjisPropertyName("user_id")]`

---

## 12. Status

This document is stable as a design target.

Concrete API names may evolve,
but the configuration groups and mapping principles must remain consistent.
