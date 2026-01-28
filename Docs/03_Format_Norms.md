# AJIS Format Norms

> Normative specification overview for the AJIS format family

---

## 1. Scope of this document

This document defines the **normative structure of the AJIS format**, independent of any specific implementation.

It describes:

* the conceptual layers of AJIS,
* which features are mandatory vs. optional,
* how textual, extended, and binary layers relate to each other,
* what is considered valid AJIS input.

Implementation details, performance strategies, and language-specific APIs are intentionally out of scope.

---

## 2. AJIS as a layered format

AJIS is defined as a **layered data format**, not a single monolithic syntax.

Each layer builds on the previous one and may be implemented independently.

### 2.1 Layers overview

1. **Textual AJIS (Core Layer)**
   Human-readable, JSON-like structured text.

2. **Extended AJIS (Feature Layer)**
   Optional syntax extensions enabled by mode or configuration.

3. **Binary & Transport Layer**
   Hybrid containers combining AJIS text with binary payloads.

An implementation may support only a subset of layers, but must clearly declare which layers are supported.

---

## 3. Textual AJIS (Core Layer)

### 3.1 Purpose

The textual AJIS layer provides:

* a familiar JSON-like structure,
* deterministic grammar,
* streamable parsing,
* precise error localization.

This layer is the **mandatory foundation** for all AJIS implementations.

---

### 3.2 Core structural elements

Textual AJIS is composed of:

* objects (key–value maps),
* arrays (ordered lists),
* primitive values (string, number, boolean, null).

Every AJIS document represents exactly **one root value**.

---

### 3.3 Strings

AJIS strings:

* are UTF-8 encoded,
* support standard escape sequences,
* may support **multi-line string forms** as defined by the specification.

Multi-line strings are designed to:

* reduce excessive escaping,
* preserve human readability,
* remain stream-parseable.

---

### 3.4 Comments

Textual AJIS **may support comments**, depending on enabled mode.

Comments:

* are ignored by the data model,
* must not affect parsing of values,
* are intended for configuration, documentation, and diagnostics.

The presence of comments does not change the semantic meaning of the document.

---

### 3.5 Numbers

AJIS supports numeric literals with extended capabilities.

The following numeric forms are part of the **Textual AJIS Core Layer** and are therefore **mandatory for core compliance**:

* **Decimal numbers** (integers and floating-point)
* **Binary numbers**
* **Octal numbers**
* **Hexadecimal numbers**

#### 3.5.1 Numeric bases

Numeric literals may be expressed using different bases:

* Decimal (base 10)
* Binary (base 2)
* Octal (base 8)
* Hexadecimal (base 16)

The base of a number is determined by its literal form as defined by the specification.

---

#### 3.5.2 Digit group separators

AJIS allows the underscore character (`_`) as a **digit group separator** to improve human readability.

Separator rules:

* **Decimal and octal numbers**: separator after every **3 digits**
* **Hexadecimal numbers**: separator after **2 or 4 digits**, but **not mixed** within a single literal
* **Binary numbers**: separator after **4 digits**

The first (most significant) group may contain fewer digits than the defined group size
(e.g. `1_000`, `0b1_0000`).

---

#### 3.5.3 Separator placement rules

* Separators **must not** appear:

  * at the beginning or end of a number,
  * consecutively,
  * immediately before or after a base prefix,
  * in the fractional (decimal) part of a number.

* For floating-point numbers:

  * separators are allowed **only in the integer part**,
  * the fractional part must be written without separators.

---

#### 3.5.4 Hexadecimal grouping consistency

For hexadecimal numbers:

* grouping size must be either **2 digits** or **4 digits**,
* the chosen grouping size must be used consistently throughout the literal,
* mixing 2-digit and 4-digit grouping in a single literal is invalid.

---

### 3.6 Whitespace and formatting

Whitespace:

* is allowed between tokens,
* has no semantic meaning,
* must not affect parsing outcome.

Formatting is considered a presentation concern, not part of the data model.

---

## 4. Extended AJIS (Feature Layer)

The Extended Layer defines **optional syntax and semantic extensions**.

These features are not required for basic compliance.

Examples include:

* comments,
* multi-line string variants,
* extended numeric formats,
* metadata annotations,
* integrity markers.

Implementations must clearly document which extensions are supported.

---

## 5. Binary & Transport Layer

### 5.1 Motivation

Some use cases require:

* large binary payloads,
* efficient streaming of mixed data,
* integrity and authenticity guarantees.

AJIS addresses this via a hybrid container approach.

---

### 5.2 AJIS + Binary association

AJIS text may:

* reference external binary data,
* describe binary segments,
* define structure and meaning of binary payloads.

The textual AJIS part always acts as the **authoritative descriptor**.

---

### 5.3 Afrowave Transport Package (ATP)

ATP is a planned container format combining:

* a textual AJIS section,
* one or more binary sections,
* optional integrity and signature metadata.

ATP allows:

* single-file transport of complex structured data,
* streaming-friendly processing,
* partial access without full materialization.

---

## 6. Validity and compliance

An AJIS document is considered valid if:

* it conforms to the grammar of the declared layer,
* all required structural rules are met,
* unsupported extensions are either rejected or explicitly ignored.

Compliance levels must be declared by implementations.

---

## 7. Forward compatibility

AJIS is designed for long-term evolution.

Rules:

* unknown extensions must not break core parsing,
* future layers must not invalidate existing documents,
* strict and permissive modes may coexist.

---

## 8. Relationship to implementations

This document defines **what AJIS is**.

Implementation documents define:

* how AJIS is parsed,
* how it is serialized,
* how errors and diagnostics are reported.

No implementation may redefine the core semantics described here.

---

## 9. Status of this specification

This document is a living normative specification.

* The Textual Core Layer is considered stable.
* Extended and Transport Layers are under active design.

Changes will be versioned and documented explicitly.
