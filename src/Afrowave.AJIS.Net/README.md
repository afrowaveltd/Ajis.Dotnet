# Afrowave.AJIS.Net

[![NuGet](https://img.shields.io/nuget/v/Afrowave.AJIS.Net.svg)](https://www.nuget.org/packages/Afrowave.AJIS.Net)

ASP.NET Core integration for the AJIS (.NET) framework. Provides input/output formatters for seamless JSON replacement with enhanced performance and features.

## Overview

The Net library enables using AJIS as a drop-in replacement for JSON in ASP.NET Core applications:

* **HTTP formatters** - `AjisInputFormatter` and `AjisOutputFormatter` for MVC
* **Seamless JSON replacement** - Same API as System.Text.Json but with AJIS features
* **Async streaming** - Efficient serialization over HTTP streams
* **Type mapping** - Full support for AJIS type mapping features
* **Diagnostics** - Enhanced error reporting for API responses

## Features

* **Input formatter** - Deserialize JSON/AJIS requests automatically
* **Output formatter** - Serialize responses to JSON/AJIS format
* **ASP.NET Core integration** - Easy configuration with `AddAjisFormatters()`
* **HTTP content negotiation** - Supports `application/json` and `application/ajis+json`
* **Async support** - All formatters support async operations
* **Diagnostics** - Detailed error messages with AJIS diagnostics
* **Compatibility** - Works with existing JSON-based APIs

## Installation

```bash
dotnet add package Afrowave.AJIS.Net
```

## Dependencies

* **Afrowave.AJIS.Core** - Core contracts and settings
* **Afrowave.AJIS.Serialization** - Serializer and value types
* **Afrowave.AJIS.IO** - File I/O utilities (optional)
* **Microsoft.AspNetCore.Mvc.Core** - ASP.NET Core MVC infrastructure
* **Microsoft.Extensions.DependencyInjection** - Dependency injection
* **Microsoft.Extensions.Options** - Configuration options

## API

See the [API Reference](../../Docs/api/README.md) for complete documentation.

### Key Types

| Type | Description |
|------|-------------|
| `AjisInputFormatter` | ASP.NET Core input formatter |
| `AjisOutputFormatter` | ASP.NET Core output formatter |
| `AjisAspNetCoreExtensions` | Extension methods for service registration |

## Usage

### Basic configuration

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddAjisFormatters();
```

### Controller usage

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var users = new[] { new { Name = "John", Age = 30 } };
        return Ok(users); // Automatically serialized to AJIS
    }

    [HttpPost]
    public IActionResult Create([FromBody] User user)
    {
        // Automatically deserialized from AJIS
        return Ok(user);
    }
}
```

### Content negotiation

```csharp
// Client can request AJIS format
var client = new HttpClient();
client.DefaultRequestHeaders.Accept.ParseAdd("application/ajis+json");

var response = await client.GetAsync("api/users");
var users = await response.Content.ReadFromJsonAsync<User[]>();
```

## Documentation

* [API Reference](../../Docs/api/README.md) - Complete API documentation
* [HTTP Integration](../../Docs/HTTP_Integration_Implementation.md) - HTTP details
* [Getting Started](../../Docs/GettingStarted/en.md) - Quick start guide
* [API Integration](../../Docs/api/README.md#aspnet-core) - Integration docs

## Compatibility

* .NET 10.0+
* ASP.NET Core 8.0+
* Nullable reference types enabled

## License

MIT
