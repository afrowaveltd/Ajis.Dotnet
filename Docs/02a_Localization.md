# 02a – Localization (AJIS LOC v1)

## 1. Purpose

AJIS localization is designed to be:

* lightweight and optional
* streaming-friendly
* safe for automated translation
* independent of platform-specific resource systems

Localization is primarily used for diagnostics, errors, warnings, and user-facing messages emitted by AJIS parsers, serializers, and tools.

The **Core AJIS package includes only English (en)** localization. Additional languages are provided via separate NuGet packages.

---

## 2. Packaging Strategy

### 2.1 Core Package

`Afrowave.AJIS.Core` contains:

* localization interfaces and contracts
* AJIS LOC loader and dictionary logic
* built-in **en** dictionary

### 2.2 Satellite Language Packages

Additional languages are distributed as separate NuGet packages:

* `Afrowave.AJIS.Locales.cs`
* `Afrowave.AJIS.Locales.de`
* `Afrowave.AJIS.Locales.fr`
* etc.

Each package contains only:

* one or more `.loc` files
* a small integration helper (DI or provider registration)

This keeps the core package minimal and allows consumers to install only the languages they need.

---

## 3. AJIS LOC v1 File Format

### 3.1 File Extension and Encoding

* File extension: `.loc`
* Encoding: UTF-8 (no BOM required)

### 3.2 Line-Based Processing Rules

* Localization files are processed **line by line**
* Any line **not starting with a double quote (`"`) is ignored**

  * comments
  * empty lines
  * metadata

### 3.3 Record Syntax

Each localization entry is defined on a single line:

```
"KEY":"VALUE"   optional comment
```

Rules:

* the first string is the localization key
* the second string is the localized value
* content after the closing value string is ignored
* whitespace around `:` is allowed

Only the first four unescaped double quotes on the line are relevant.

---

## 4. String Rules

Both keys and values follow identical string rules:

* UTF-8 characters allowed directly
* supported escape sequences:

  * `\"` – double quote
  * `\\` – backslash
  * `\n`, `\r`, `\t`
* string ends at the first **non-escaped** `"`

This makes `.loc` parsing trivial and extremely fast.

---

## 5. Placeholders and Formatting

### 5.1 Placeholder Syntax

AJIS localization uses **.NET-style placeholders**:

* `{0}`, `{1}`, `{2}`
* `{0:format}`

Escaping:

* `{{` → literal `{`
* `}}` → literal `}`

### 5.2 Translation-Safe Handling

To support automated translation without breaking placeholders:

* before translation, placeholders are temporarily replaced with protected tokens
* example:

  * `{0}` → `⟦AJIS_ARG0⟧`
* after translation, tokens are restored to original placeholders

This ensures placeholders are never altered by translation engines.

---

## 6. Fallback Resolution

Localization lookup follows this order:

1. user-provided overrides
2. current UI language (if available)
3. built-in English (`en`)
4. missing-key behavior

Missing-key behavior is configurable:

* return key
* return `[missing:KEY]`
* emit diagnostic

---

## 7. Integration with Diagnostics

All diagnostics, exceptions, and warnings use localization keys internally.

Formatting parameters are applied **after** localization lookup.

This guarantees:

* consistent error codes
* localized messages
* stable machine-readable diagnostics

---

## 8. Automation and Tooling Readiness

AJIS LOC v1 is intentionally designed to be:

* easily generated
* safely machine-translated
* diff-friendly
* stream-processable

This enables future tooling such as:

* automatic translation pipelines
* localization validation tools
* missing-key analyzers

---

## 9. Versioning

* This document defines **AJIS LOC v1**
* Future extensions must remain backward compatible
* Any breaking changes require a new LOC version
