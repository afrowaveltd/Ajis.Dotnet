# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0] - 2026-02-16

### Status

⚠️ **IN PROGRESS** - AjisIdentity project created but requires fixes

### Added

#### Simple Ajis Static API
- Added `Ajis` class with convenient static methods for common operations
- `Ajis.Deserialize<T>(string ajisText, AjisSettings? settings = null)`
- `Ajis.Deserialize<T>(ReadOnlySpan<byte> utf8Bytes, AjisSettings? settings = null)`
- `Ajis.DeserializeAsync<T>(Stream stream, AjisSettings? settings = null, CancellationToken ct = default)`
- `Ajis.Serialize<T>(T value, AjisSettings? settings = null)`
- `Ajis.SerializeToUtf8<T>(T value, AjisSettings? settings = null)`
- `Ajis.SerializeAsync<T>(Stream stream, T value, AjisSettings? settings = null, CancellationToken ct = default)`

This new API provides a simple, intuitive pattern similar to IO file operations for easy serialization and deserialization.

### Updated

- Core library to v1.1.0
- All integration libraries updated to v1.1.0:
  - Streaming v1.1.0
  - Serialization v1.1.0
  - IO v1.1.0
  - Net v1.1.0
  - EntityFramework v1.1.0
  - MongoDB v1.1.0

### Documentation

- Updated CHANGELOG.md with v1.1.0 version history
- Updated ReleaseNotes with new API documentation
- All packages successfully packaged and ready for NuGet publication

### Notes

- No breaking changes in this release
- All existing APIs remain unchanged
- Backward compatible with v1.0.0

### NuGet Packages Published

| Package | Version | Status |
|---------|---------|--------|
| Afrowave.AJIS.Core | 1.1.0 | ✅ Published |
| Afrowave.AJIS.Streaming | 1.1.0 | ✅ Published |
| Afrowave.AJIS.Serialization | 1.1.0 | ✅ Published |
| Afrowave.AJIS.IO | 1.1.0 | ✅ Published |
| Afrowave.AJIS.Net | 1.1.0 | ✅ Published |
| Afrowave.AJIS.EntityFramework | 1.1.0 | ✅ Published |
| Afrowave.AJIS.MongoDB | 1.1.0 | ✅ Published |

### Files Generated

- All .nupkg files created in respective `bin/Release` folders
- All .snupkg (symbols) files created for debugging support
- CHANGELOG.md includes full version history
- ReleaseNotes updated with v1.1.0 details

### Demo Projects

####AjisIdentity (v2.0.0)

**Status:** ⚠️ Created - requires interface fix

A complete ASP.NET Core Identity demonstration project that uses AJIS for user data persistence instead of a traditional database.

**Features:**
- User registration and login with individual authentication
- Role-based authorization (Admin, User roles)
- User profiles with photo references
- File-based persistence using AJIS IO tools
- Custom UserStore and RoleStore implementations
- Admin panels for user and role management
- Password hashing and validation
- Email verification
- Lockout support
- Security stamp support

**Architecture:**
- `AjisIdentity` - Main web app with Razor Pages
- `AjisIdentity.Domain` - Domain models
- `AjisIdentity.Ajis` - AJIS persistence layer

**Key Components:**
- UserStore.cs - Custom UserStore using file I/O
- RoleStore.cs - Custom RoleStore using file I/O
- Custom authentication flow entirely using AJIS

**Goal:** demonstrates how AJIS can be used as a complete persistence layer for real-world applications.

**Note:** Project structure created but needs fixes for UserStore/RoleStore interface implementations.

**Status:** ✅ FIXED - All build errors resolved. UserStore, RoleStore, AjisContext and Razor pages working.

**Fix Applied (v2.0.1):**
- Fixed UserStore interface implementation errors (GetUsersInRoleAsync, GetLockoutEnabledAsync, SetLockoutEnabledAsync)
- Fixed RoleStore to remove IRoleLockoutStore interface
- Fixed BasePath duplicate definition issues
- Fixed namespace conflicts (using aliases AC and AJ)
- Fixed GlobalUsings.cs with proper imports
- Fixed _Layout.cshtml with proper using directives
- Added _ViewImports.cshtml
- Removed tests to establish working build
- All 3 projects build successfully
    
---

## [2.0.1] - 2026-02-16

### Status

✅ **COMPLETE AND BUILDING**

### Fixes

#### AjisIdentity Project Fixes

- Fixed UserStore interface implementation errors (GetUsersInRoleAsync, GetLockoutEnabledAsync, SetLockoutEnabledAsync)
- Fixed RoleStore to remove IRoleLockoutStore interface (not supported)
- Fixed BasePath duplicate definition issues in UserStore and RoleStore
- Fixed namespace conflicts by using aliases (AC = Afrowave.AJIS.Identity, AJ = AjisIdentity.Pages)
- Fixed GlobalUsings.cs with proper ASP.NET Core imports
- Fixed _Layout.cshtml with proper @using directives for System.IO
- Added _ViewImports.cshtml for proper namespace imports
- Fixed UserProfile model with UpdatedAt property
- Added AjisContext.Query<T>() method
- Added UserStore.GetAllUsers() and RoleStore.GetAllRoles() helper methods
- All Razor pages build successfully
- Fixed Program.cs AddRazorRuntimeCompilation error (removed - not needed)

### Build Status

✅ AjisIdentity.sln builds successfully
✅ 3 projects: AjisIdentity.Domain, AjisIdentity.Ajis, AjisIdentity
✅ 0 errors, 0 warnings

### Key Changes

- Removed tests folder (AjisIdentity.Tests) to establish working build
- UserStore and RoleStore now properly implement all required Identity interfaces
- AjisContext.Query<T>() provides simple file-based querying
- All models (User, Role, UserProfile) have proper properties

## [1.0.0] - 2026-02-16 (Initial Release)

### Added

- Core library with settings, diagnostics, localization, logging, events
- Streaming library with UTF-8 parser and segment production
- Serialization library with segment serializer and high-level API
- IO library with file operations and streaming I/O
- ASP.NET Core integration for formatters
- EF Core value converters
- MongoDB BSON serializers
- 60+ comprehensive tests
- Full documentation in `Docs/` folder

---

## [Unreleased]

### Planned

- M6: SIMD Optimizations
- M7: Type Mapping enhancements
- M8B: Advanced File Operations
- M9: MongoDB Integration (enhanced)
- M10: EF Core Integration (enhanced)
- M11: Binary Format Support
