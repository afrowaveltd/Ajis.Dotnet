# AJIS Documentation

> Comprehensive documentation for AJIS .NET libraries

---

## 📚 Documentation Structure

All documentation is organized by topic, with English (`en.md`) as the primary language.

```
Docs/
├── API/                          # API reference
│   ├── en.md                    # API navigation & overview
│   ├── Core.md                  # Core library API
│   ├── Streaming.md             # Streaming library API
│   ├── Serialization.md         # Serialization library API
│   ├── IO.md                    # IO library API
│   ├── Net.md                   # Net library API
│   ├── EntityFramework.md       # EntityFramework integration API
│   ├── MongoDB.md               # MongoDB integration API
│   ├── Diagnostics.md           # Diagnostics & error reporting
│   ├── Events.md                # Event & progress infrastructure
│   ├── Localization.md          # Localization support
│   └── ATP.md                   # ATP binary format
├── Architecture/                 # Architecture documentation
│   └── en.md                    # Repository overview & architecture
├── GettingStarted/               # Quick start guides
│   └── en.md                    # Getting started with AJIS
├── Configuration/                # Configuration options
│   └── en.md                    # Configuration reference
├── Performance/                  # Performance documentation
│   └── en.md                    # Performance best practices
├── ReleaseNotes/                 # Release summaries
│   └── en.md                    # Release notes & changelog
├── Roadmap/                      # Implementation roadmap
│   └── en.md                    # Roadmap & future plans
└── Tutorial/                     # Tutorials & examples
    ├── en.md                    # Main tutorial guide
    ├── TestFormat.md            # Test format deep dive
    └── Tools.md                 # Tools & CLI usage
```

---

## 🚀 Getting Started

New to AJIS? Start here:

1. **[Installation & Setup](../README.md#quick-start)** - Install packages and basic usage
2. **[Getting Started Guide](GettingStarted/en.md)** - Step-by-step tutorials
3. **[API Reference](API/en.md)** - Complete API documentation

---

## 📖 Documentation Topics

### API Reference

Complete API documentation for all libraries:

* **[API Overview](API/en.md)** - Library overview and navigation
* **[Core](API/en.md)** - Core library API
* **[Streaming](API/en.md)** - Streaming library API
* **[Serialization](API/en.md)** - Serialization library API
* **[IO](API/en.md)** - IO library API
* **[Net](API/en.md)** - ASP.NET Core API
* **[EntityFramework](API/en.md)** - EF Core integration API
* **[MongoDB](API/en.md)** - MongoDB integration API

### Architecture

* **[Repository Overview](Architecture/en.md)** - Project structure and package responsibilities
* **[Developer Experience](API/DeveloperExperience.md)** - API design principles

### Configuration

* **[Format Norms](docs/Configuration/en.md)** - AJIS format specifications
* **[Modes & Heuristics](docs/Configuration/Modes.md)** - Parsing modes and heuristics
* **[Settings & Mapping](API/Mapping.md)** - Configuration and type mapping

### Performance

* **[Best Practices](Performance/en.md)** - Performance optimization tips
* **[Metrics](ReleaseNotes/en.md)** - Performance benchmarks and results

### Tutorials

* **[Test Format](Tutorial/TestFormat.md)** - Deep dive into test formats
* **[Tools CLI](Tutorial/Tools.md)** - Command-line tools

---

## 🔧 Project Libraries

| Library | Package | Status | Description |
|---------|---------|--------|-------------|
| **Core** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Core) | Stable | Core contracts, settings, diagnostics |
| **Streaming** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Streaming) | Stable | UTF-8 streaming parser |
| **Serialization** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Serialization) | Stable | Segment serializer |
| **IO** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.IO) | Stable | File operations |
| **Net** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.Net) | Stable | ASP.NET Core |
| **EntityFramework** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.EntityFramework) | Stable | EF Core integration |
| **MongoDB** | [NuGet](https://www.nuget.org/packages/Afrowave.AJIS.MongoDB) | Stable | MongoDB integration |

---

## 📊 Quick Links

* **[Main README](../README.md)** - Project overview
* **[Release Notes](ReleaseNotes/en.md)** - Version history
* **[Roadmap](Roadmap/en.md)** - Future plans
* **[Getting Started](GettingStarted/en.md)** - Quick start guide

---

## 🌐 Language Support

All documentation is written in English (`en.md`). Automated translation workflows generate translations for other languages.

To add a translation:
1. Copy the `en.md` file to `[lang].md`
2. Translate the content
3. Ensure technical accuracy

---

## 🤝 Contributing

Documentation contributions are welcome! Please:

1. Follow the existing structure and format
2. Write in clear, concise English
3. Include code examples where applicable
4. Update related documentation when making changes

---

## 📝 Notes

* All documentation follows a consistent structure
* Technical terms are preserved in English
* Code examples are tested and verified
* Documentation is auto-generated where possible
