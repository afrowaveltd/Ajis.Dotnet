# AJIS Core API

> Normative specification for the public AJIS programming interface

---

## 1. Scope of this document

This document defines the **Core API surface of AJIS**, intended for:

* application developers,
* library integrators,
* tooling and infrastructure components.

The Core API provides a stable, minimal, and composable interface
for parsing and serializing AJIS data across different modes and environments.

---

## 2. Design goals

The AJIS Core API is designed with the following goals:

* **Familiarity** – inspired by widely used .NET JSON APIs
* **Streaming-first** – no mandatory full materialization
* **Async-native** – asynchronous APIs are first-class citizens
* **Low overhead** – minimal allocations and indirection
* **Composable** – suitable for pipelines, tooling, and services

---

## 3. Core abstractions

The AJIS Core API is built around a small set of fundamental abstractions.

Normatively defined core concepts:

* **AjisParser** – incremental reader of AJIS input
* **AjisSerializer** – incremental writer of AJIS output
* **AjisValue** – immutable representation of a parsed value (optional)
* **AjisOptions** – configuration and mode selection
* **AjisEventStream** – optional observability channel

Implementations may extend these abstractions but must not weaken their contracts.

---

## 4. Parser API

### 4.1 Parser creation

Parsers are created via factory methods.

Normative requirements:

* creation must be cheap,
* no parsing occurs during construction,
* input sources may be streams, readers, or buffers.

---

### 4.2 Parsing methods

The Core API defines the following parsing styles:

* **Pull-based parsing** – caller drives iteration
* **Push/stream-based parsing** – values are emitted via callbacks or streams

Both styles may coexist in the same implementation.

---

### 4.3 Async parsing

Asynchronous parsing methods:

* must support cancellation tokens,
* must not block threads,
* must integrate with event streams.

Async and sync APIs must exhibit identical semantics.

---

## 5. Serializer API

### 5.1 Serializer creation

Serializers are created similarly to parsers and:

* must not write output during construction,
* must respect provided options and modes.

---

### 5.2 Serialization methods

Serialization may be:

* value-based (serialize object/value),
* stream-based (write incrementally),
* event-aware (emit progress and diagnostics).

Streaming serializers must support large outputs
without full buffering.

---

## 6. Options and configuration

### 6.1 AjisOptions

AjisOptions control:

* parser mode selection,
* enabled format features,
* diagnostic behavior,
* event emission policies.

Options must be immutable once parsing or serialization begins.

---

### 6.2 Defaults

Default options must:

* favor safety and correctness,
* enable diagnostics,
* select Auto mode unless overridden.

---

## 7. Cancellation and termination

AJIS operations must support cooperative cancellation.

Rules:

* cancellation must be observed promptly,
* partial output must be left in a consistent state,
* cancellation must not corrupt internal state.

---

## 8. Error handling contract

Core API methods:

* throw exceptions only for fatal errors,
* report diagnostics separately,
* never return partially invalid values.

The error handling contract must be consistent across sync and async APIs.

---

## 9. Relationship to other documents

This document defines the **shape and behavior of the public API**.

It relies on:

* Format Norms (what is valid AJIS),
* Diagnostics (how errors are reported),
* Events (how progress is observed),
* Parsers and Modes (how parsing behaves).

---

## 10. Status of this specification

This document is considered **stable**.

Additions may be made to extend functionality,
but existing API contracts must remain compatible.

### Error handling policy

Implementations MUST support at least these user-facing styles:

1) Parse(...) – throws AjisException on fatal errors.
2) TryParse(...) – returns false on error and provides diagnostic.
3) ParseOrNull(...) – returns null on error; diagnostics are emitted/collected.

Default behavior SHOULD be Parse(...) throwing, to match common .NET expectations.
