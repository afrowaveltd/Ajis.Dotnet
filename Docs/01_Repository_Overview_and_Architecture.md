# AJIS .NET – Repository Overview & Architecture

> High‑level map of the AJIS .NET repository, packages, and stability boundaries

---

## 1. Purpose of this document

This document answers three fundamental questions:

1. **What lives where?**
2. **Which parts are stable vs experimental?**
3. **How do the pieces fit together conceptually?**

It is intentionally written **before implementation**, to prevent architectural drift.

---

## 2. Repository top‑level layout

```
Ajis.Dotnet/
│
├─ src/
│  ├─ Ajis.Core/
│  ├─ Ajis.Streaming/
│  ├─ Ajis.Serialization/
│  ├─ Ajis.Mapping/
│  ├─ Ajis.Tools/
│  └─ Ajis.ATP/
│
├─ tests/
│  ├─ Ajis.Core.Tests/
│  ├─ Ajis.Streaming.Tests/
│  ├─ Ajis.Serialization.Tests/
│  ├─ Ajis.Tools.Tests/
│  └─ Ajis.Common.TestData/
│
├─ benchmarks/
│  └─ Ajis.Benchmarks/
│
├─ Docs/
│  ├─ 01_Repository_Overview_and_Architecture.md
│  ├─ 02_Design_Goals.md
│  ├─ 03_AJIS_Overview.md
│  ├─ 04_Text_Format_Deep_Dive.md   (planned)
│  └─ ...
│
└─ README.md
```

---

## 3. Package responsibilities

### 3.1 Ajis.Core (stable)

**Responsibilities**:

* `AjisSettings`
* exceptions & diagnostics
* localization abstraction
* logging abstraction
* event / progress infrastructure

**Rules**:

* no IO
* no reflection-heavy logic
* no streaming assumptions

This package must remain **small, stable, and dependency‑light**.

---

### 3.2 Ajis.Streaming (stable core)

**Responsibilities**:

* UTF‑8 reader primitives
* text scanning (strings, numbers, comments)
* `ParseSegments` implementation
* `AjisSegment` model

**Rules**:

* single‑pass only
* bounded memory
* no object materialization

This is the **performance heart** of AJIS.

---

### 3.3 Ajis.Serialization (stable)

**Responsibilities**:

* segment serializer
* high‑level `AjisSerializer` facade
* async + stream APIs

**Rules**:

* must be able to operate purely on segments
* must not require random access

---

### 3.4 Ajis.Mapping (stable, optional)

**Responsibilities**:

* AJIS ↔ C# object mapping
* naming policies
* attributes
* converters

**Rules**:

* opt‑in package
* mapping must not affect streaming parser

Mapping is a **layer above** serialization, not a requirement for AJIS itself.

---

### 3.5 Ajis.Tools (stable, user‑facing)

**Responsibilities**:

* file operations (validate, stats, select, patch)
* streaming rewrite pipelines
* CLI entry point (`ajis`)

**Rules**:

* never corrupt input files
* always rewrite to new output
* progress & diagnostics enabled by default

---

### 3.6 Ajis.ATP (experimental → stable)

**Responsibilities**:

* ATP container reader/writer
* AJIS text → binary stream handoff
* checksums / signatures (hooks)

**Status**:

* initially experimental
* becomes stable once ATP spec is frozen

---

## 4. Stability levels

| Level            | Meaning                                      |
| ---------------- | -------------------------------------------- |
| **Stable**       | Public API frozen except additive changes    |
| **Preview**      | API usable but may change                    |
| **Experimental** | Internal or opt‑in, no compatibility promise |

Initial targets:

* Core / Streaming / Serialization → **Stable**
* Mapping / Tools → **Stable after v1**
* ATP → **Experimental initially**

---

## 5. Architectural flow

Conceptual data flow:

```
Input (Stream / Bytes)
   ↓
Text Scanner
   ↓
Segment Parser (AjisSegment stream)
   ↓
[Optional Transforms / Tools]
   ↓
Segment Serializer
   ↓
Output (Stream / File)
```

Object mapping, tools, and ATP all **sit on top** of this backbone.

---

## 6. Design invariants (must never break)

* AJIS text parsing is always streaming‑first
* No feature may require full document materialization
* Tools must work on multi‑GB files
* Diagnostics must be precise and localizable
* Performance optimizations must not leak into API semantics

---

## 7. Relationship to other documents

This document provides context for:

* Docs/16_Pipelines_and_Segments.md
* Docs/17_Transform_Recipes.md
* Docs/18_Implementation_Roadmap.md

It should be read **before** starting implementation work.

---

## 8. Status

Stable.

This document defines architectural intent and boundaries.
