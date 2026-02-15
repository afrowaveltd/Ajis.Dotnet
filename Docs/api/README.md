# AJIS API Documentation

Complete API reference and documentation for the Ajis.Dotnet library.

## Quick Navigation

- **Core API** - Core parsing and serialization
- **Streaming API** - Low-level streaming operations
- **File I/O** - High-level file operations
- **Serialization** - Object mapping and conversion
- **Integration** - ASP.NET Core, EF Core, MongoDB
- **Testing** - StreamWalk and test utilities

---

## Core API

### Settings and Configuration

- `AjisSettings` - Central configuration object
- `AjisNumberOptions` - Number parsing options
- `AjisStringOptions` - String parsing options
- `AjisCommentOptions` - Comment handling options
- `AjisProcessingProfile` - Processing profiles (Default, Fast, LowMemory)

### Diagnostics and Events

- `AjisDiagnostic` - Diagnostic information
- `AjisDiagnosticSeverity` - Error levels (Info, Warning, Error, Debug)
- `AjisErrorCode` - Error codes
- `AjisEvent` hierarchy - Progress, diagnostic, and phase events
- `IAjisEventSink` - Event sink interface
- `AjisProgressReporter` - Progress reporting

### Exceptions

- `AjisException` - Base exception
- `AjisFormatException` - Format-specific exceptions

---

## Streaming API

### Reader

- `AjisReader` - Low-level streaming reader
- `AjisLexer` - Lexical analysis
- `AjisToken` - Token representation
- `AjisSliceUtf8` - UTF-8 slice for zero-allocation token delivery

### StreamWalk

- `AjisStreamWalkRunner` - Stream walking engine
- `StreamWalkOptions` - StreamWalk configuration
- `StreamWalkMode` - Walking modes
- `AjisStreamWalkEngineRegistry` - Engine registry
- `AjisStreamWalkEngineKind` - Engine selection

### Segments

- `AjisSegment` - Streaming segment
- `AjisSegmentMap` - Map transformation
- `AjisSegmentFilter` - Filter transformation
- `AjisSegmentSelect` - Select transformation
- `AjisSegmentPatch` - Patch transformation

---

## File I/O

### High-Level File Operations

- `AjisFile` - Static class with CRUD operations
  - `Create<T>()` - Create new file
  - `CreateAsync<T>()` - Create asynchronously
  - `Append<T>()` - Append single item
  - `AppendMany<T>()` - Append multiple items
  - `ReadAll<T>()` - Read all items
  - `ReadAllAsync<T>()` - Read all asynchronously
  - `Enumerate<T>()` - Stream items
  - `EnumerateAsync<T>()` - Stream items asynchronously
  - `ReadAt<T>()` - Read specific index
  - `FindByKey<T>()` - Find by key property
  - `Get<T>()` - Alternative find by key
  - `FindByPredicate<T>()` - Find by predicate
  - `UpdateByKey<T>()` - Update by key
  - `DeleteByKey<T>()` - Delete by key

### Indexing

- `AjisFileIndex<T>` - Index for fast lookups
  - `Build()` - Build index
  - `FindByKey()` - Find by key
  - `GetKeys()` - Get all keys
  - `ContainsKey()` - Check if key exists
  - `AjisFile.CreateIndex<T>()` - Create index

### LINQ Support

- `AjisQuery` - LINQ query provider
  - `FromFile<T>()` - Query from file
  - `Where()` - Filter
  - `OrderBy()` - Sort ascending
  - `OrderByDescending()` - Sort descending
  - `ThenBy()` - Secondary sort
  - `Skip()` - Skip items
  - `Take()` - Take items
  - `Select()` - Project

### Aggregations

- `AjisAggregations` - Aggregation methods
  - `Count()` - Count items
  - `LongCount()` - Count large datasets
  - `Any()` - Check if any match
  - `All()` - Check if all match
  - `Sum()` - Sum values
  - `Average()` - Calculate average
  - `Min()` - Find minimum
  - `Max()` - Find maximum
  - `Distinct()` - Remove duplicates
  - `DistinctBy()` - Remove duplicates by key

---

## Serialization

### Converter and Mapping

- `AjisConverter<T>` - Generic converter
  - `Serialize()` - Serialize to string
  - `Deserialize()` - Deserialize from string
  - `SerializeAsync()` - Serialize asynchronously
  - `DeserializeAsync()` - Deserialize asynchronously

### Direct Serialization

- `Utf8DirectSerializer<T>` - Ultra-fast UTF-8 serializer
- `Utf8DirectDeserializer<T>` - Ultra-fast UTF-8 deserializer
- `FastDeserializer<T>` - Fast deserializer

### Property Mapping

- `PropertyMapper` - Property mapping
- `PropertyGetterCompiler` - Getter compilation
- `PropertySetterCompiler` - Setter compilation
- `INamingPolicy` - Naming policies
  - `CamelCaseNamingPolicy` - camelCase
  - `PascalCaseNamingPolicy` - PascalCase
  - `SnakeCaseNamingPolicy` - snake_case
  - `KebabCaseNamingPolicy` - kebab-case

### Attributes

- `AjisPropertyName` - Custom property name
- `AjisIgnore` - Ignore property
- `AjisNumberBase` - Number base attribute

### Value Types

- `AjisValue` - Minimal value representation
  - `NullValue`, `BoolValue`, `NumberValue`, `StringValue`, `ArrayValue`, `ObjectValue`

---

## Integration

### ASP.NET Core

- `AjisAspNetCore` - Extension methods
  - `AddAjis()` - Add AJIS services
  - `UseAjis()` - Use AJIS middleware

### Entity Framework Core

- `AjisValueConverter<T>` - EF Core value converter
- `AjisCollectionConverter<T>` - Collection converter
- `AjisDbContext` - Base DbContext
- `AjisFileRepository<T>` - Repository pattern

### MongoDB

- `AjisBsonSerializer<T>` - BSON serializer
- `AjisMongoCollection<T>` - Collection wrapper
- `AjisMongoRepository<T>` - Repository
- `AjisMongoExtensions` - Extension methods

### HTTP Client

- `AjisHttpClient` - AJIS-optimized HTTP client
  - `GetAsync<T>()` - GET request
  - `GetListAsync<T>()` - GET for lists
  - `PostAsync<T>()` - POST request
  - `PostListAsync<T>()` - POST for lists
  - `PutAsync<T>()` - PUT request
  - `PatchAsync<T>()` - PATCH request
  - `StreamAsync<T>()` - Stream request

---

## Testing

### StreamWalk testing

- `AjisStreamWalkRunner` - Test runner
- `AjisStreamWalkOptions` - Test options
- `AjisStreamWalkTestCaseFile` - Test case files

### Test utilities

- `AjisBenchmarkRunner` - Benchmark runner
- `AjisLargePayloadGenerator` - Test data generator

---

## Documentation Structure

```
Docs/
├── api/
│   ├── README.md (this file)
│   ├── slices.md          - UTF-8 slice model
│   ├── events.md          - Event model
│   ├── visitor.md         - Visitor interface
│   ├── reader.md          - Reader contract
│   ├── stream-reader.md   - Streaming reader
│   ├── options.md         - Configuration options
│   ├── compliance.md      - Compliance checklists
│   └── errors.md          - Error model
├── Manual/
│   ├── README.md
│   ├── GettingStarted/
│   │   └── QuickStart.md
│   ├── Querying/
│   │   ├── Aggregations.md
│   │   ├── Sorting.md
│   │   └── BasicQueries.md
│   └── FileOperations/
│       └── FileOperationsReference.md
├── 01_Repository_Overview.md
├── 02_Diagnostics.md
├── 03_Format_Norms.md
├── 05_Events.md
├── 06_Parsers_and_Modes.md
├── 07_Core_API.md
├── 08_Serialization.md
├── 09_Streaming.md
├── 10_Modes_And_Heuristics.md
├── 11_Streaming_parser_Algorithm.md
├── 12_ATP_Text_Binary_Container.md
├── 13_AJIS_Settings_And_Mapping.md
├── 14_Public_API_Developer_Experience.md
├── 15_Tools_CLI_and_FileOps.md
├── 16_Pipelines_and_Segments.md
├── 17_Transform_Recipes.md
├── 18_Implementation_Roadmap.md
├── 19_Project_Status_and_Roadmap.md
└── 20_StreamWalk_Contract_Checklist.md
```

---

## Getting Started

See `Manual/GettingStarted/QuickStart.md` for a step-by-step tutorial.

### Basic Example

```csharp
using Afrowave.AJIS.IO;

// Create file
var users = new[] {
    new User { Id = 1, Name = "Alice" },
    new User { Id = 2, Name = "Bob" }
};
AjisFile.Create("users.ajis", users);

// Read all
var allUsers = AjisFile.ReadAll<User>("users.ajis");

// Query with LINQ
var activeUsers = AjisQuery.FromFile<User>("users.ajis")
    .Where(u => u.IsActive)
    .ToList();

// Aggregate
var count = AjisQuery.FromFile<User>("users.ajis").Count();
var avgPoints = AjisQuery.FromFile<User>("users.ajis")
    .Average(u => u.Points);
```

---

## Community and Support

- GitHub Issues: Report bugs and request features
- Documentation: See full documentation in `Docs/` folder
- Examples: Check `Manual/Examples/` for complete examples

---

*Last updated: Generated from source code*
