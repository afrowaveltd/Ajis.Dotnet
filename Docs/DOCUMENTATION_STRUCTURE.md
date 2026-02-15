# Documentation Structure Guide

This document explains the new AJIS documentation structure designed for automated translation workflows.

---

## Overview

All AJIS documentation follows a consistent structure with topic-based organization and language-specific files.

```
Docs/
├── <Topic>/
│   ├── en.md          # English (primary, automatically generated)
│   └── [lang].md      # Translations (automatically generated)
```

---

## Directory Structure

### API Reference

`Docs/API/en.md` - Main API navigation and index

Subdirectories contain library-specific API documentation:
- `Docs/API/Core.md` - Core library API
- `Docs/API/Streaming.md` - Streaming library API
- `Docs/API/Serialization.md` - Serialization library API
- `Docs/API/IO.md` - IO library API
- `Docs/API/Net.md` - ASP.NET Core API
- `Docs/API/EntityFramework.md` - EF Core integration API
- `Docs/API/MongoDB.md` - MongoDB integration API
- `Docs/API/Diagnostics.md` - Diagnostics & error reporting
- `Docs/API/Events.md` - Event & progress infrastructure
- `Docs/API/Localization.md` - Localization support
- `Docs/API/ATP.md` - ATP binary format

### Architecture

`Docs/Architecture/en.md` - Repository overview and architecture

### Getting Started

`Docs/GettingStarted/en.md` - Quick start guides and examples

### Configuration

`Docs/Configuration/en.md` - Configuration options and settings

### Performance

`Docs/Performance/en.md` - Performance best practices and benchmarks

### Release Notes

`Docs/ReleaseNotes/en.md` - Version history and changelog

### Roadmap

`Docs/Roadmap/en.md` - Implementation roadmap and future plans

### Tutorials

`Docs/Tutorial/en.md` - Main tutorial guide

---

## Language-Based Files

Each documentation topic has a corresponding directory with language-specific `.md` files:

```
Docs/
├── API/
│   ├── en.md          # English API reference
│   └── cs.md          # Czech translation (auto-generated)
├── Architecture/
│   ├── en.md          # English architecture doc
│   └── es.md          # Spanish translation (auto-generated)
└── GettingStarted/
    ├── en.md          # English getting started guide
    └── fr.md          # French translation (auto-generated)
```

### Benefits

1. **Automated Translation**: Each language has its own file, making it easy to run translation tools
2. **Version Control**: Changes to translations can be tracked separately
3. **Quality Control**: English file is source of truth; translations can be reviewed independently
4. **Scalability**: Easy to add new languages by adding `[lang].md` files
5. **Context Preservation**: Topic folder provides context for translation tools

---

## Documentation Conventions

### File Naming

* Primary language (English): `en.md`
* Other languages: `[language-code].md`
  - Examples: `cs.md`, `es.md`, `fr.md`, `de.md`, `ja.md`

### Folder Naming

* Folders use camelCase or PascalCase
* Examples: `API`, `Architecture`, `GettingStarted`, `Configuration`, `Performance`, `ReleaseNotes`, `Roadmap`, `Tutorial`

### Content Structure

Each documentation file follows this structure:

```markdown
# Topic Title

> Brief description

---

## Section 1

Content...

## Section 2

Content...

---
```

---

## Translation Workflow

### For Automated Tools

1. English files (`en.md`) are the source of truth
2. Translation tools scan for `en.md` files
3. Tools generate `[lang].md` files with machine translations
4. Human review and refinement of translations
5. Commit translations back to repository

### Example Translation Command

```bash
# Pseudo-command for translation workflow
translate --source en.md --target [lang].md --provider azure-ai
```

---

## Migration from Old Structure

The old structure used numbered files:

```
Old: Docs/01_Repository_Overview_and_Architecture.md
New: Docs/Architecture/en.md
```

Migration mapping:

| Old | New |
|-----|-----|
| `01_Repository_Overview_and_Architecture.md` | `Docs/Architecture/en.md` |
| `00_FINAL_RELEASE_SUMMARY.md` | `Docs/ReleaseNotes/en.md` |
| `19_Project_Status_and_Roadmap.md` | `Docs/Roadmap/en.md` (combined with 00_Complete_Roadmap_v1_to_v2_Plus.md) |
| `00_Complete_Roadmap_v1_to_v2_Plus.md` | `Docs/Roadmap/en.md` (combined) |
| `03_Format_Norms.md` | `Docs/Configuration/en.md` |
| `04_Test_Format_Deep_Dive.md` | `Docs/Tutorial/TestFormat.md` |
| `15_Tools_CLI_and_FileOps.md` | `Docs/Tutorial/Tools.md` |

---

## Best Practices

### For Documentation Writers

1. **Write in English first**: All documentation starts as `en.md`
2. **Use clear headings**: Structure content with proper markdown headings
3. **Include code examples**: Use fenced code blocks with language specifier
4. **Keep it concise**: Avoid unnecessary verbosity
5. **Link to related docs**: Use relative links to other documentation

### For Translators

1. **Preserve formatting**: Keep markdown syntax identical
2. **Translate technical terms**: Keep API names, code examples in English
3. **Add notes**: Use translator comments for ambiguous sections
4. **Test rendering**: Verify markdown renders correctly

---

## Related Documentation

* [Main Documentation Summary](README.md)
* [API Reference](API/en.md)
* [Architecture Overview](Architecture/en.md)
* [Getting Started Guide](GettingStarted/en.md)
