# Afrowave.AJIS.Core

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.Core.svg)](https://www.nuget.org/packages/Afrowave.AJIS.Core)

Core library for the AJIS (.NET) project. Provides core contracts, settings, diagnostics, localization, logging, and event model.

## Overview

The Core library is the foundation of the AJIS ecosystem. It defines:

* **Settings and options** - `AjisSettings`, `AjisOptions`
* **Diagnostics and error reporting** - `AjisDiagnostic`, `AjisException`
* **Localization support** - `AjisLocDictionary`, `AjisTextProvider`
* **Logging abstraction** - `IAjisLogger`
* **Event/progress infrastructure** - `IAjisEventSource`

**Important**: This package contains **no parsing or serialization logic**. It provides the infrastructure and contracts that other packages build upon.

## Installation

```bash
dotnet add package Afrowave.AJIS.Core
```

## API

See the [API Reference](../API/en.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisSettings` | Core configuration and options |
| `AjisDiagnostic` | Diagnostic information |
| `AjisException` | Exception with detailed diagnostics |
| `AjisLocDictionary` | Localization resources |
| `AjisTextProvider` | Text provider chain |
| `IAjisLogger` | Logging interface |
| `IAjisEventSource` | Event/progress interface |

## Documentation

* [API Reference](../API/en.md) - Complete API documentation
* [Configuration](../Configuration/en.md) - Configuration options
* [Architecture](../Architecture/en.md) - Repository architecture

## Compatibility

* .NET 10.0+
* Nullable reference types enabled

## License

MIT
