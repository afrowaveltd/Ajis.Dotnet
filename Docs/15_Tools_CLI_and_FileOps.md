# AJIS Tools – CLI & File Operations (No Full Load)

> Specification of tooling, file-based operations, and streaming CRUD over AJIS

---

## 1. Scope

This document defines the **Tools layer** for AJIS .NET:

* file operations that avoid full-document loading
* streaming transforms and patches
* query / search / select operations at parser level
* CLI tooling for developers and automation

Tools are optional packages built on top of AJIS Core + Streaming.

---

## 2. Goals

Tools must be:

* **safe** for multi-GB files
* **streaming-first** (bounded memory)
* **configurable** via `AjisSettings`
* **script-friendly** (exit codes, JSON/AJIS output)
* **portable** across OS

---

## 3. Tool categories

### 3.1 FileOps

Operations over files:

* validate
* normalize (canonical formatting)
* select (extract subset)
* transform (rewrite with filter)
* patch (apply changes)

### 3.2 Query

Read-only discovery:

* find path
* list keys
* count objects/items
* compute statistics

### 3.3 CRUD

Streaming modifications:

* insert / update / delete
* array item patching
* merge / overlay

CRUD must not require loading the entire document.

---

## 4. Path model

Tools require a path addressing model.

Path must support:

* object properties
* array indices
* wildcards (optional)

Conceptual examples:

* `$.users[0].name`
* `$.config.theme`
* `$.items[*].id`

Path evaluation must integrate with streaming segments.

---

## 5. Streaming rewrite pattern

Most file modifications are implemented as:

```
Input → ParseSegments → Transform → SerializeSegments → Output
```

Key requirement:

* **rewrite only** (no in-place edits)

Rationale:

* in-place edits are unsafe for variable-length formats
* rewrite is deterministic and crash-safe

---

## 6. Select / Extract

### 6.1 Select by path

`Select` extracts a subtree to a new AJIS document.

Rules:

* selection must preserve valid AJIS output
* selection may optionally wrap extracted node into a root object

### 6.2 Select by predicate

Selection may be predicate-based:

* select array items where a field equals value
* select objects matching key presence

Predicates must run on streaming segments.

---

## 7. Patch operations

Patch is an ordered list of operations.

Conceptual patch operations:

* `Set(path, value)`
* `Remove(path)`
* `Insert(path, value)`
* `Replace(path, value)`

Patch input format should support:

* AJIS patch document
* JSON patch document (optional)

Patch execution must be:

* deterministic
* idempotent where possible
* streaming-safe

---

## 8. Merge / Overlay

Tools may support merge strategies:

* overlay object keys
* append arrays
* replace arrays

Merge must be explicit and configurable.

---

## 9. Indexing (optional advanced)

For repeated queries on huge files, Tools may provide optional indexes.

Indexing goals:

* allow fast path lookup
* avoid full scan when possible

Index format must be:

* separate from the AJIS file
* rebuildable
* portable

---

## 10. CLI tool

A CLI tool should be provided:

* `ajis`

### 10.1 CLI principles

* stable commands
* clear exit codes
* progress output optional
* can output AJIS or JSON

### 10.2 Command groups

* `ajis validate <file>`
* `ajis stats <file>`
* `ajis select <file> --path <path>`
* `ajis patch <file> --patch <patchFile> --out <outFile>`
* `ajis merge <base> <overlay> --out <outFile>`
---

## Test data generator (benchmarks)

The benchmark project includes a simple payload generator for stress testing.

Usage:

```
Ajis.Benchmarks generate <outputPath> [userCount] [addressesPerUser]
Ajis.Benchmarks benchmark <inputPath>
```

Examples:

```
Ajis.Benchmarks generate test_data_ajis/big/users_150k.json 150000 3
Ajis.Benchmarks benchmark test_data_ajis/big/users_150k.json
```

---

## 11. Safety and crash behavior

Tools must:

* always write to a new file
* support atomic replace option:

  * write `file.tmp`
  * fsync
  * rename

Tools must never corrupt the original file if interrupted.

---

## 12. Integration with AjisSettings

Tools must accept:

* parsing mode (Auto/BigFiles)
* comment support
* number formats
* naming policies for mapping (when relevant)
* diagnostics and localization

---

## 13. Relationship to other documents

Tools rely on:

* Streaming Parser Algorithm
* Streaming & Pipelines
* Public API (DX)

Tools are the practical layer that makes AJIS usable for real workflows.

---

## 14. Status

This document is stable as a tooling specification.

Implementation should proceed incrementally:

* validate / stats first
* select / transform next
* patch / merge next
* optional indexing last
