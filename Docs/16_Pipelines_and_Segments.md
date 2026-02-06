# AJIS Pipelines & Segments

> Normative specification for `AjisSegment` streaming, transforms, and segment serialization

---

## 1. Scope

This document defines:

* `AjisSegment` (the atomic unit of streaming parse output)
* segment stream rules (ordering, nesting)
* transforms (filter/map/patch) operating on segments
* segment serializer (write AJIS text from segments)
* requirements for building file tools without full load

---

## 2. Design goals

* segments must support **single-pass** processing
* transforms must be **bounded-memory**
* the segment stream must be **self-describing enough** to reconstruct valid AJIS
* segmentation must preserve **precise positions** for diagnostics

---

## 3. AjisSegment model

`AjisSegment` is a discriminated union (tagged record) with variants:

### 3.1 Structural segments

* `ContainerStart(kind, frameId, parentFrameId)`
* `ContainerEnd(kind, frameId, parentFrameId)`

Where `kind` is `Object` or `Array`.

### 3.2 Object segments

* `PropertyName(frameId, name)`
* `PropertyValue(frameId, valueKind, value)`

Note: Value may be produced as:

* a primitive segment (see below), or
* a container start (nested value)

### 3.3 Primitive segments

* `Null(frameId)`
* `Bool(frameId, value)`
* `Number(frameId, rawOrCanonical, flags)`
* `String(frameId, sliceOrDecoded)`

### 3.4 Array segments

* `ArrayItemStart(frameId, index)` (optional)
* `ArrayItemValue(frameId, index, valueKind, value)`

Implementations may omit `ArrayItemStart` if index is carried by value segments.

### 3.5 Diagnostics / meta segments

* `Progress(bytesRead, percent?)`
* `Diagnostic(code, severity, position, messageKey, args)`

Meta segments must not affect AJIS reconstruction.

---

## 4. Segment stream rules

### 4.1 Nesting rules

* `ContainerStart` / `ContainerEnd` must be properly nested
* nested containers appear inline in the stream

Example:

* `PropertyName(users)`
* `ContainerStart(Array)`
* `ContainerStart(Object)`
* `PropertyName(id)`
* `Number(1)`
* `ContainerEnd(Object)`
* `ContainerEnd(Array)`

### 4.2 Object ordering rules

Within an object frame:

* `PropertyName` must appear before its corresponding value
* exactly one value must follow each property name

### 4.3 Array ordering rules

Within an array frame:

* values appear sequentially
* index may be derived by counting

---

## 5. String and number representation in segments

### 5.1 Strings

Strings may be represented as:

* decoded `string` (easy DX)
* raw UTF-8 slices (high-performance)

Settings determine representation.

String segments may include flags such as `HasEscapes` and `HasNonAscii`, and name segments may include `IsIdentifierStyle`.
Number segments may include flags such as hex/binary/octal to preserve AJIS base prefixes.

If slices are used:

* lifetime must be clearly defined
* consumers must not hold slices beyond allowed scope

### 5.2 Numbers

Numbers may be represented as:

* canonical numeric types
* raw token slices

For Tools, raw token preservation is often preferred.

---

## 6. Segment serializer

A segment serializer writes AJIS text by consuming segments.

Requirements:

* must produce valid AJIS
* must handle indentation/pretty/compact
* must apply canonicalization options
* must ignore meta segments

Serializer must enforce:

* commas and colons inserted correctly
* quotes and escaping correct

Serializer must not require random access.

---

## 7. Transforms

Transforms consume a segment stream and produce a segment stream.

### 7.1 Filter

Filter removes entire subtrees or values.

Must support:

* skip a property
* skip an array item
* skip a subtree by frameId

Skipping a subtree requires the transform to:

* detect its `ContainerStart`
* count nested depth until matching `ContainerEnd`

### 7.2 Map

Map rewrites:

* property names
* primitive values
* numbers canonicalization

### 7.3 Patch

Patch transforms apply operations:

* set/replace/remove

Patch may require limited buffering:

* buffer current property/value
  n
  Patch must remain bounded-memory and must not buffer unrelated parts.

---

## 8. Path evaluation on segments

Path evaluation is computed during streaming.

State for path evaluation:

* current container stack
* current property name
* current array index

Matching rules:

* path match fires when the segment corresponds to the target node

Wildcards:

* supported optionally
* must be deterministic

---

## 9. Progress and diagnostics

Progress segments:

* may be emitted periodically
* may include percent when total length known

Diagnostics segments:

* may be emitted for warnings
* fatal errors still throw exceptions

---

## 10. Minimal implementation sequence

Recommended implementation order:

1. `ParseSegments` producing structural + primitive segments
   * prefer profile-based module selection (Universal/LowMemory/HighThroughput)
2. Segment serializer that can reconstruct identical AJIS (round-trip)
3. Simple transforms: rename keys, drop property
4. Path evaluation
5. Patch operations

---

## 11. Status

This document is stable.

Segment variant set may expand,
but nesting and reconstruction semantics must remain consistent.
