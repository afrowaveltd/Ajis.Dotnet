# AJIS API – Reader

This document defines the low-level Reader contract used by AJIS parsers.

## Purpose

The Reader is a minimal, allocation-free abstraction over sequential input.
It operates on UTF-8 bytes and provides controlled lookahead without copying.

## Design Goals

* Zero allocations on the hot path
* Deterministic behavior
* No implicit buffering
* Works for text and stream inputs

## Core Interface

The reader exposes the following conceptual operations:

* Peek current byte
* Consume current byte
* Advance by N bytes
* Check end-of-input
* Capture slices

Exact naming is implementation-specific, but semantics must match.

## Position & State

* Reader maintains a current offset
* Offset is monotonic
* No seeking backwards

## UTF-8 Handling

The reader itself is UTF-8 aware but does not decode to UTF-16.
Decoding is delegated to higher layers.

## Error Handling

* Reader never throws for content errors
* Only structural failures are reported
* Parsing errors are reported by the parser layer

## Threading

Reader instances are not thread-safe.
Each worker must own its reader instance.

## Invariants

* No implicit normalization
* No hidden buffering
* No mutation of underlying data
