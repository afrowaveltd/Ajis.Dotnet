# AJIS Core

## Overview

The Core library provides the fundamental building blocks for AJIS:

* Settings and configuration (`AjisSettings`)
* Diagnostics and error reporting (`AjisDiagnostic`)
* Localization and message management (`AjisLoc*`)
* Core data structures and contracts

## Key Components

### AjisSettings

Configuration model grouped into small nested groups:

* `Format` - AJIS textual features
* `Parsing` - mode, limits
* `Serialization` - formatting, canonical
* `Naming` - policies, case
* `Mapping` - object model binding
* `Diagnostics` - errors, localization
* `Events` - progress / debug

### Diagnostics

Diagnostics are structured descriptions of problems or conditions:

* **Info** - informational messages
* **Warning** - non-fatal issues, recoverable conditions
* **Error** - fatal parsing or semantic errors
* **Critical** - internal or invariant violations

Each diagnostic includes:
* Stable diagnostic code (e.g., `AJIS1003`)
* Message key for localization
* Source location (byte offset, line, column)

### Localization

AJIS localization uses `.loc` files with simple key-value pairs:

* File extension: `.loc`
* Encoding: UTF-8
* Format: `"KEY":"VALUE"`

The core package includes English (en) localization. Additional languages are provided via separate NuGet packages.

## Package Structure

* `Afrowave.AJIS.Core` - Core primitives, settings, diagnostics, localization
* `Afrowave.AJIS.Streaming` - Streaming parser producing AJIS segments
* `Afrowave.AJIS.Serialization` - Serialization APIs and segment-based serializers
* `Afrowave.AJIS.IO` - File-level operations
* `Afrowave.AJIS.Net` - ASP.NET Core integration
* `Afrowave.AJIS.EntityFramework` - EF Core value converters
* `Afrowave.AJIS.MongoDB` - MongoDB BSON serializers

## Status

Stable. Core contractsremain unchanged across versions.
