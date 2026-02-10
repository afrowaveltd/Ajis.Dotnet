# AJIS Manual

**Comprehensive guide to AJIS for .NET**

Welcome to the AJIS manual! This guide will help you master AJIS - from basic file operations to advanced querying and optimization.

## 📚 Table of Contents

### 🚀 Getting Started
- [Quick Start Guide](GettingStarted/QuickStart.md) - Get up and running in 5 minutes
- [Installation](GettingStarted/Installation.md) - How to add AJIS to your project
- [Your First AJIS App](GettingStarted/FirstApp.md) - Build a simple application

### 💡 Core Concepts
- [What is AJIS?](CoreConcepts/WhatIsAJIS.md) - Understanding the format
- [AJIS vs JSON](CoreConcepts/ComparisonWithJSON.md) - When to use AJIS
- [Data Types](CoreConcepts/DataTypes.md) - Supported types and serialization
- [Directives](CoreConcepts/Directives.md) - Comments, metadata, and extensions

### 📁 File Operations
- [Creating Files](FileOperations/Creating.md) - Write data to AJIS files
- [Reading Files](FileOperations/Reading.md) - Load and enumerate data
- [Updating Files](FileOperations/Updating.md) - Modify existing data
- [Deleting Records](FileOperations/Deleting.md) - Remove data efficiently
- [Indexing](FileOperations/Indexing.md) - Fast lookups with indexes

### 🔍 Querying Data
- [Basic Queries](Querying/BasicQueries.md) - Filter and find data
- [LINQ Support](Querying/LINQSupport.md) - Use familiar LINQ syntax
- [Filtering](Querying/Filtering.md) - Complex filter expressions
- [Sorting](Querying/Sorting.md) - Order results efficiently
- [Aggregations](Querying/Aggregations.md) - **NEW!** Count, Sum, Average, Min, Max
- [Joins](Querying/Joins.md) - Combine data from multiple files

### 🔧 ATP Tooling
- [ATP Overview](ATPTooling/Overview.md) - Afrowave Transport Package
- [JSON to ATP](ATPTooling/JSONtoATP.md) - Convert JSON to ATP
- [ATP to JSON](ATPTooling/ATPtoJSON.md) - Extract JSON from ATP
- [Binary Attachments](ATPTooling/BinaryAttachments.md) - Embed files and data
- [Validation](ATPTooling/Validation.md) - Verify ATP integrity

### 🚄 Advanced Scenarios
- [Streaming Large Files](AdvancedScenarios/Streaming.md) - Process GB-sized files
- [Performance Tuning](AdvancedScenarios/PerformanceTuning.md) - Optimize for speed
- [Memory Optimization](AdvancedScenarios/MemoryOptimization.md) - Minimize allocations
- [Async Operations](AdvancedScenarios/AsyncOperations.md) - Non-blocking I/O
- [Custom Converters](AdvancedScenarios/CustomConverters.md) - Serialize complex types
- [Migration from JSON](AdvancedScenarios/MigrationFromJSON.md) - Move from JSON to AJIS

### 📊 Performance
- [Benchmarks](Performance/Benchmarks.md) - AJIS vs STJ vs Newtonsoft
- [Best Practices](Performance/BestPractices.md) - Write efficient code
- [Memory Profiling](Performance/MemoryProfiling.md) - Understand allocations
- [Caching Strategies](Performance/CachingStrategies.md) - Speed up repeated access

### 🎯 Use Cases
- [Configuration Files](UseCases/ConfigurationFiles.md) - App settings and config
- [Data Import/Export](UseCases/DataImportExport.md) - ETL scenarios
- [API Responses](UseCases/APIResponses.md) - Web services
- [Logging](UseCases/Logging.md) - Structured logging
- [Database Alternative](UseCases/DatabaseAlternative.md) - Simple data storage
- [**Complete E-Commerce Example**](Examples/CompleteExamples.md) - **NEW!** Full application

### 🛠️ API Reference
- [AjisFile API](APIReference/AjisFile.md) - High-level file operations
- [AjisQuery API](APIReference/AjisQuery.md) - LINQ provider
- [AjisConverter API](APIReference/AjisConverter.md) - Serialization
- [AjisStream API](APIReference/AjisStream.md) - Low-level streaming

---

## 🎓 Learning Path

**Beginner:**
1. Read [Quick Start Guide](GettingStarted/QuickStart.md)
2. Try [Your First AJIS App](GettingStarted/FirstApp.md)
3. Learn [File Operations](FileOperations/Creating.md)

**Intermediate:**
4. Master [Querying](Querying/BasicQueries.md)
5. Understand [Indexing](FileOperations/Indexing.md)
6. Explore [Performance](Performance/BestPractices.md)

**Advanced:**
7. Study [Streaming](AdvancedScenarios/Streaming.md)
8. Implement [Custom Converters](AdvancedScenarios/CustomConverters.md)
9. Review [Benchmarks](Performance/Benchmarks.md)

---

## 💬 Get Help

- **Issues:** [GitHub Issues](https://github.com/afrowaveltd/Ajis.Dotnet/issues)
- **Discussions:** [GitHub Discussions](https://github.com/afrowaveltd/Ajis.Dotnet/discussions)
- **Documentation:** [docs/](../docs/)

---

## 🤝 Contributing

Want to improve this manual? PRs welcome!

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.
