# AJIS .NET Project Structure Cleanup - Plan

## Current State Analysis

### Projects Identified
1. **Core Projects (in solution)**
   - `Afrowave.AJIS.Core` - Core library with parsing/directives
   - `Afrowave.AJIS.IO` - File I/O operations
   - `Afrowave.AJIS.Serialization` - Serialization functionality
   - `Afrowave.AJIS.Streaming` - Streaming parser engine

2. **Missing/Unintegrated Projects**
   - `Afrowave.AJIS` - Empty folder, no csproj
   - `Afrowave.AJIS.Net` - Has source but not in solution
   - `Afrowave.AJIS.Records` - Has source but not in solution

3. **External/Unintegrated Code**
   - `Tools_extracted` - Old code with duplicate files, not integrated
   - `Afrowave.AJIS.EntityFramework` - Database integration (not in solution)
   - `Afrowave.AJIS.MongoDB` - MongoDB integration (not in solution)

4. **Test Projects (in solution)**
   - `Afrowave.AJIS.Core.Tests`
   - `Afrowave.AJIS.IO.Tests`
   - `Afrowave.AJIS.Serialization.Tests`
   - `Afrowave.AJIS.Testing`

5. **Unused Files**
   - `Class1.cs` placeholders in multiple projects
   - Empty `Abstractions` folder
   - `Tools_extracted` duplicates

### Folder Structure Inconsistencies
- `Abstraction` vs `Abstractions` (one empty)
- `Docs` folders vary across projects
- Mixed folder naming (CamelCase vs PascalCase)

### NuGet Package Recommendations
Based on functionality and dependencies:
1. `Afrowave.AJIS.Core` - Core parsing/engine
2. `Afrowave.AJIS.Serialization` - Serialization
3. `Afrowave.AJIS.IO` - File I/O
4. `Afrowave.AJIS.Streaming` - Streaming API
5. `Afrowave.AJIS.Net` - .NET integration (ASP.NET, HttpClient)
6. `Afrowave.AJIS.Records` - Record mapping
7. `Afrowave.AJIS.EntityFramework` - EF Core integration
8. `Afrowave.AJIS.MongoDB` - MongoDB integration

## Implementation Plan

### Step 1: Project Structure Audit ✅ COMPLETED
- Documented all projects and their purposes
- Identified unused/integrated projects
- Mapped dependencies

### Step 2: Remove Unused Project Files
- Delete `Afrowave.AJIS` folder (empty)
- Delete `Class1.cs` from all projects
- Delete empty `Abstractions` folder
- Remove unused docs from `Tools_extracted`

### Step 3: Integrate Uninegrated Projects
- Add `Afrowave.AJIS.Net` to solution
- Add `Afrowave.AJIS.Records` to solution (if needed)
- Decide fate of `Afrowave.AJIS.EntityFramework` and `Afrowave.AJIS.MongoDB`
- Remove or integrate `Tools_extracted`

### Step 4: Standardize Folder Structure
- Use consistent folder naming (e.g., `Abstraction` - singular)
- Remove `Docs` folders from individual projects (keep only at root level)
- Organize code into logical folders across all projects

### Step 5: Update Documentation
- Review all README.md files
- Update Docs folder to match current state
- Ensure XML comments are accurate

### Step 6: Update Test Projects
- Remove `Afrowave.AJIS.Tests` (empty)
- Remove `Afrowave.AJIS.Net.Tests` (references non-existent Class1)
- Remove `Afrowave.AJIS.Records.Tests` (if exists)
- Consolidate test projects as needed

### Step 7: Prepare for NuGet Publication
- Set up consistent folder structure for packages
- Configure package metadata (nuspec or PackageReference)
- Document public API surface area
- Create release notes template

### Step 8: Final Verification
- Run all tests to ensure clean state
- Verify build succeeds for all projects
- Document any breaking changes

## Immediate Actions (Priority)

1. **Delete unused placeholder files** (fast, safe)
2. **Remove or integrate Tools_extracted** (decide based on need)
3. **Clean up test projects** (remove broken tests)
4. **Add missing projects to solution** (if needed)
5. **Standardize folder structure** (consistent naming)
6. **Update documentation** (match implementation)

## Decision Points

1. **Tools_extracted**: Should we keep it for historical purposes or delete it?
2. **Extra test projects**: Should we consolidate or remove?
3. **Project structure**: Singular or plural folder names? (Recommend: singular)
4. **Docs organization**: Per-project or centralized?

---

*Status: Plan prepared, waiting for approval to proceed*
