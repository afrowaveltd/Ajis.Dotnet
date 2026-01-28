# AJIS Public API – Developer Experience (DX)

> Developer-facing specification and guidance for AJIS .NET public API

---

## 1. Scope

This document defines the **public API shape** for AJIS .NET:

* primary entry points
* common usage patterns
* options via `AjisSettings`
* streaming-first workflows
* mapping to C# objects

This is a **DX document**: it must be readable by developers without requiring internal knowledge.

---

## 2. API design principles

* Familiar naming (inspired by System.Text.Json / Newtonsoft)
* Minimal cognitive load for basic scenarios
* Explicit configuration through `AjisSettings`
* Streaming APIs available without ceremony
* Advanced features are opt-in

---

## 3. Main entry point

The primary static facade is:

* `AjisSerializer`

Responsibilities:

* serialize/deserialize
* parse to token stream (advanced)
* provide convenience overloads

---

## 4. Common methods (conceptual)

### 4.1 Text serialization

* `Serialize<T>(T value, AjisSettings? settings = null)`
* `SerializeToBytes<T>(T value, AjisSettings? settings = null)`
* `SerializeAsync<T>(Stream destination, T value, AjisSettings? settings = null, CancellationToken ct = default)`

### 4.2 Text deserialization

* `Deserialize<T>(string ajis, AjisSettings? settings = null)`
* `Deserialize<T>(ReadOnlySpan<byte> utf8, AjisSettings? settings = null)`
* `DeserializeAsync<T>(Stream source, AjisSettings? settings = null, CancellationToken ct = default)`

---

## 5. Streaming-first APIs

### 5.1 Token / segment streaming

For BigFiles mode, the library should expose a stream of parse segments:

* `ParseSegments(Stream source, AjisSettings? settings = null)`

  * returns `IAsyncEnumerable<AjisSegment>`

Segments include:

* container start/end
* property name
* primitive values
* string slices

This enables pipelines:

* validate → filter → write

without materializing the whole document.

---

### 5.2 Array streaming to objects

For large arrays of objects:

* `DeserializeArrayAsync<T>(Stream source, AjisSettings? settings = null)`

  * returns `IAsyncEnumerable<T>`

This enables:

* processing millions of items
* early exit
* bounded memory

---

## 6. Mapping and naming

Mapping behavior is controlled by:

* `AjisSettings.Naming`
* `AjisSettings.Mapping`

Common configuration:

* camelCase AJIS keys ↔ PascalCase C# properties
* explicit attribute mapping for special cases

---

## 7. Error handling

Default behavior:

* fatal parse errors throw an exception with precise location

Optional behavior (settings):

* tolerant mode
* warnings via diagnostics stream

---

## 8. Localization and diagnostics

Diagnostics and exceptions must:

* be localizable
* include machine-readable codes
* include position details

Public API should expose:

* `AjisException` with structured fields
* `AjisDiagnostic` for non-fatal issues

---

## 9. Progress reporting

Progress is provided via the event/stream system:

* bytes processed
* percent estimates (when possible)

Progress must be:

* non-blocking
* disableable

---

## 10. Example patterns (conceptual)

### 10.1 Simple use

* `var obj = AjisSerializer.Deserialize<MyType>(text);`

### 10.2 Streaming a large array

* `await foreach (var item in AjisSerializer.DeserializeArrayAsync<MyType>(stream)) { ... }`

### 10.3 Filter and rewrite

* `await AjisSerializer.TransformAsync(input, output, segment => ...)`

---

## 11. Package layout

The API must remain consistent across packages:

* Core: common primitives, exceptions, settings
* Streaming: segment parser, pipelines
* Mapping: reflection/resolver, converters
* Tools: file operations, CLI

---

## 12. Status

This document is stable as a target API.

Concrete method signatures may evolve,
but the DX principles and conceptual entry points must remain consistent.
