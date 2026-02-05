# AJIS Test Data Manifest

This document defines the purpose and stability guarantees of all datasets under `test_data/`.

All datasets listed here are considered **shared contracts** across AJIS implementations and languages.

---

## test_data/streamwalk/

**Purpose:**
Deterministic StreamWalk parser validation using `.case` files.

**Scope:**

* Token walking
* Deterministic trace output
* Diagnostic codes and offsets

**Used In Milestones:**

* M1 (StreamWalk Reference)
* M1.1 (Engine Selection Skeleton)

**Stability:**

* FIXED
* Changes require explicit agreement

---

## test_data_legacy/common/

**Origin:**
Imported from the legacy AJIS_ATP project as semantic reference data.

**Purpose:**
Document‑level AJIS and JSON samples used for:

* Parser correctness
* Serializer correctness
* Canonicalization
* JSON compatibility validation

**Notes:**

* These datasets MAY contain AJIS features not present in strict JSON
* Semantics are governed exclusively by the current AJIS.Dotnet specification
* Legacy binary formats are explicitly ignored

**Used In Milestones:**

* M2 (Text Primitives)
* M3 (Streaming Parser)
* M4 (Serializer)
* M5 (LAX Parser)
* M6 (Benchmarks)

**Stability:**

- LEGACY / NON-NORMATIVE (for now)
- Files may be added
- Existing files MUST NOT be edited in a way that changes meaning
- Promotion to `test_data/common/` requires explicit agreement

---

## test_data/generated/ (future)

**Purpose:**
Generated large‑scale datasets for stress testing and benchmarking.

**Examples:**

* Large user/address object arrays
* AJIS vs Strict JSON equivalents

**Used In Milestones:**

* M3 (Memory safety)
* M6 (Performance parity)

**Stability:**

* NON‑CANONICAL
* Regenerated as needed

---

## Governance Rules

* Any modification to FIXED datasets requires explicit approval
* Generated datasets MUST NOT be used as semantic references
* Cross‑language implementations MUST treat these datasets identically

---

**This manifest is authoritative for test data usage.**
