# AJIS User Guide - Complete User Manual

## 🚀 Introduction to AJIS

**AJIS (Afrowave JSON-like Interchange Specification)** is a high-performance data interchange format inspired by JSON, but optimized for enterprise scenarios with large datasets, streaming, and precise diagnostics.

### ✅ When to Use AJIS

- **Large datasets** (hundreds of MB to GB)
- **Streaming applications** (real-time processing)
- **Enterprise systems** (precise diagnostics, extensibility)
- **Low-memory environments** (embedded, IoT)
- **Long-term archiving** (normative specification)

### ❌ When to Use JSON Instead of AJIS

- Simple REST APIs
- Small configuration files
- Maximum compatibility with existing tools

---

## 📦 Basic Usage

### 1. Installation

```bash
# Add NuGet package
dotnet add package Afrowave.AJIS
```

### 2. Basic Serialization/Deserialization

```csharp
using Afrowave.AJIS.Serialization.Mapping;

// Define your model
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create converter
var converter = new AjisConverter<User>();

// Serialize object
var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
string ajisText = converter.Serialize(user);

// Deserialize back
User? deserializedUser = converter.Deserialize(ajisText);
```

### 3. Working with Collections

```csharp
var users = new List<User>
{
    new User { Id = 1, Name = "Alice" },
    new User { Id = 2, Name = "Bob" }
};

var listConverter = new AjisConverter<List<User>>();
string jsonArray = listConverter.Serialize(users);
List<User>? loadedUsers = listConverter.Deserialize(jsonArray);
```

---

## ⚙️ Configuration and Settings

### 1. Basic Configuration

```csharp
var settings = new AjisSettings
{
    // Allow trailing commas in arrays and objects
    AllowTrailingCommas = true,

    // Allow comments
    Comments = new AjisCommentOptions
    {
        AllowLineComments = true,      // // comments
        AllowBlockComments = true     // /* comments */
    },

    // Set maximum depth
    MaxDepth = 100,

    // Output formatting
    Serialization = new AjisSerializationOptions
    {
        Pretty = true,        // Readable format
        IndentSize = 2        // 2 spaces per level
    }
};

var converter = new AjisConverter<User>(settings);
```

### 2. Naming Policies

```csharp
// PascalCase (default)
var pascalConverter = new AjisConverter<User>(PascalCaseNamingPolicy.Instance);

// camelCase for JavaScript compatibility
var camelConverter = new AjisConverter<User>(CamelCaseNamingPolicy.Instance);

// Custom naming policy
public class KebabCaseNamingPolicy : IAjisNamingPolicy
{
    public string ConvertName(string name)
    {
        // Implement kebab-case conversion
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}

var kebabConverter = new AjisConverter<User>(new KebabCaseNamingPolicy());
```

### 3. Processing Profiles

```csharp
var settings = new AjisSettings
{
    // For server applications (high-throughput)
    ParserProfile = AjisProcessingProfile.Server,
    SerializerProfile = AjisProcessingProfile.Server,

    // For desktop applications (balanced)
    ParserProfile = AjisProcessingProfile.Desktop,
    SerializerProfile = AjisProcessingProfile.Desktop,

    // For embedded systems (low-memory)
    ParserProfile = AjisProcessingProfile.Embedded,
    SerializerProfile = AjisProcessingProfile.Embedded,

    // Universal (default - auto-selection)
    ParserProfile = AjisProcessingProfile.Universal,
    SerializerProfile = AjisProcessingProfile.Universal
};
```

---

## 🔧 Advanced Scenarios

### 1. Custom Converters

```csharp
// Custom converter for DateTime
public class CustomDateTimeConverter : ICustomAjisConverter<DateTime>
{
    public object? ReadJson(ref Utf8JsonReader reader, Type typeToConvert, AjisSerializerOptions options)
    {
        var dateString = reader.GetString();
        return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public void WriteJson(Utf8JsonWriter writer, DateTime value, AjisSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}

// Register custom converter
var converter = new AjisConverter<User>()
    .RegisterConverter(new CustomDateTimeConverter());
```

### 2. Working with Enum Types

```csharp
public enum UserRole
{
    Admin,
    User,
    Guest
}

public class UserWithRole
{
    public string Name { get; set; }
    public UserRole Role { get; set; }
}

// Enums serialize as string values
var user = new UserWithRole { Name = "Alice", Role = UserRole.Admin };
var converter = new AjisConverter<UserWithRole>();
string json = converter.Serialize(user);
// {"Name":"Alice","Role":"Admin"}
```

### 3. Nullable Types and Default Values

```csharp
public class OptionalUser
{
    public string Name { get; set; } = "";
    public int? Age { get; set; }          // Nullable int
    public string? Email { get; set; }     // Nullable string
    public List<string>? Tags { get; set; } // Nullable collection
}

// AJIS automatically handles null values
var user = new OptionalUser { Name = "Bob" };
var converter = new AjisConverter<OptionalUser>();
string json = converter.Serialize(user);
// {"Name":"Bob","Age":null,"Email":null,"Tags":null}
```

### 4. Complex Nested Objects

```csharp
public class Company
{
    public string Name { get; set; }
    public Address Headquarters { get; set; }
    public List<Department> Departments { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

public class Department
{
    public string Name { get; set; }
    public int EmployeeCount { get; set; }
    public Manager Manager { get; set; }
}

public class Manager
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// AJIS automatically handles arbitrarily deep nesting
var company = new Company
{
    Name = "TechCorp",
    Headquarters = new Address { Street = "123 Main St", City = "NYC", Country = "USA" },
    Departments = new List<Department>
    {
        new Department
        {
            Name = "Engineering",
            EmployeeCount = 50,
            Manager = new Manager { Name = "Alice", Email = "alice@techcorp.com" }
        }
    }
};

var converter = new AjisConverter<Company>();
string json = converter.Serialize(company);
```

---

## 📊 Performance Optimization

### 1. Reuse Converter Instances

```csharp
// ❌ Bad - creating new instance for each request
public string ProcessRequest(string json)
{
    var converter = new AjisConverter<User>();  // NEW INSTANCE!
    var user = converter.Deserialize(json);
    return converter.Serialize(user);
}

// ✅ Good - reuse single instance
private static readonly AjisConverter<User> _userConverter = new();

public string ProcessRequest(string json)
{
    var user = _userConverter.Deserialize(json);
    return _userConverter.Serialize(user);
}
```

### 2. UTF-8 Optimization

```csharp
// For high performance, use UTF-8 bytes directly
var converter = new AjisConverter<User>();

// Serialize to bytes
using var stream = new MemoryStream();
using var writer = new Utf8JsonWriter(stream);
converter.SerializeToUtf8(writer, user);
byte[] utf8Bytes = stream.ToArray();

// Deserialize from bytes
var readOnlySpan = new ReadOnlySpan<byte>(utf8Bytes);
User? user = converter.DeserializeFromUtf8(readOnlySpan);
```

### 3. Streaming for Large Files

```csharp
// For files > 100MB use streaming
using var fileStream = File.OpenRead("large_file.json");
using var jsonReader = new Utf8JsonReader(fileStream);

// Stream processing - doesn't load entire file into memory
while (jsonReader.Read())
{
    // Process token by token
    switch (jsonReader.TokenType)
    {
        case JsonTokenType.StartObject:
            // Handle object start
            break;
        case JsonTokenType.PropertyName:
            var propertyName = jsonReader.GetString();
            // Handle property
            break;
        // ... other tokens
    }
}
```

### 4. Memory Pooling

```csharp
// For high throughput, use ArrayPool
var settings = new AjisSettings
{
    // AJIS automatically uses ArrayPool for large allocations
    StreamChunkThreshold = "1G"  // Memory-mapped for > 1GB
};

var converter = new AjisConverter<LargeData>(settings);
```

---

## 🌐 Web and API Scenarios

### 1. ASP.NET Core Integration

```csharp
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<AjisConverter<User>>();
    services.AddSingleton<AjisConverter<List<User>>>();
}

// Controller
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AjisConverter<User> _userConverter;
    private readonly AjisConverter<List<User>> _listConverter;

    public UsersController(
        AjisConverter<User> userConverter,
        AjisConverter<List<User>> listConverter)
    {
        _userConverter = userConverter;
        _listConverter = listConverter;
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = GetUserFromDatabase(id);
        var ajisResponse = _userConverter.Serialize(user);

        return Content(ajisResponse, "application/ajis+json");
    }

    [HttpPost("bulk")]
    public IActionResult CreateUsers([FromBody] string ajisPayload)
    {
        var users = _listConverter.Deserialize(ajisPayload);
        if (users == null) return BadRequest("Invalid AJIS");

        SaveUsersToDatabase(users);
        return Ok();
    }
}
```

### 2. HttpClient with AJIS

```csharp
public class AjisHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly AjisConverter<ApiResponse> _responseConverter;

    public AjisHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _responseConverter = new AjisConverter<ApiResponse>();
    }

    public async Task<ApiResponse?> GetAjisAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        var ajisText = await response.Content.ReadAsStringAsync();
        return _responseConverter.Deserialize(ajisText);
    }

    public async Task<HttpResponseMessage> PostAjisAsync(string url, object data)
    {
        var converter = new AjisConverter<object>();
        var ajisBody = converter.Serialize(data);

        var content = new StringContent(ajisBody, Encoding.UTF8, "application/ajis+json");
        return await _httpClient.PostAsync(url, content);
    }
}
```

---

## 💾 File Operations

### 1. Reading/Writing Files

```csharp
public class FileOperations
{
    private readonly AjisConverter<List<User>> _usersConverter = new();

    public async Task SaveUsersToFileAsync(string filePath, List<User> users)
    {
        var ajisText = _usersConverter.Serialize(users);
        await File.WriteAllTextAsync(filePath, ajisText, Encoding.UTF8);
    }

    public async Task<List<User>?> LoadUsersFromFileAsync(string filePath)
    {
        var ajisText = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        return _usersConverter.Deserialize(ajisText);
    }

    // For large files - streaming
    public async IAsyncEnumerable<User> StreamUsersFromFileAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var jsonReader = new Utf8JsonReader(fileStream);

        // Skip to array start
        while (jsonReader.Read() && jsonReader.TokenType != JsonTokenType.StartArray) { }

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonTokenType.StartObject)
            {
                // Parse individual User object
                var user = ParseUserFromReader(ref jsonReader);
                if (user != null)
                    yield return user;
            }
            else if (jsonReader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }
        }
    }

    private User? ParseUserFromReader(ref Utf8JsonReader reader)
    {
        // Implementation of stream parsing for individual objects
        // ... (simplified for example)
        return null;
    }
}
```

### 2. Compression and Archiving

```csharp
public class CompressedStorage
{
    private readonly AjisConverter<List<User>> _converter = new();

    public async Task SaveCompressedAsync(string filePath, List<User> users)
    {
        var ajisText = _converter.Serialize(users);
        var ajisBytes = Encoding.UTF8.GetBytes(ajisText);

        using var fileStream = File.Create(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
        await gzipStream.WriteAsync(ajisBytes);
    }

    public async Task<List<User>?> LoadCompressedAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var memoryStream = new MemoryStream();

        await gzipStream.CopyToAsync(memoryStream);
        var ajisBytes = memoryStream.ToArray();
        var ajisText = Encoding.UTF8.GetString(ajisBytes);

        return _converter.Deserialize(ajisText);
    }
}
```

---

## 🔍 Diagnostics and Error Handling

### 1. Basic Error Handling

```csharp
try
{
    var user = converter.Deserialize(ajisText);
    if (user == null)
    {
        Console.WriteLine("Deserialization returned null");
        return;
    }
    // Process user
}
catch (AjisFormatException ex)
{
    Console.WriteLine($"AJIS format error at {ex.Position}: {ex.Message}");
}
catch (AjisException ex)
{
    Console.WriteLine($"AJIS error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### 2. Detailed Diagnostics

```csharp
var settings = new AjisSettings
{
    EventSink = new ConsoleEventSink(),  // Log all events
    Logger = new ConsoleLogger()          // Log all messages
};

var converter = new AjisConverter<User>(settings);

// Custom event sink for detailed tracking
public class ConsoleEventSink : IAjisEventSink
{
    public void Emit(AjisEvent evt)
    {
        switch (evt)
        {
            case AjisProgressEvent progress:
                Console.WriteLine($"Progress: {progress.Phase} - {progress.Percent}%");
                break;
            case AjisDiagnosticEvent diagnostic:
                Console.WriteLine($"Diagnostic: {diagnostic.Diagnostic.Severity} - {diagnostic.Diagnostic.MessageKey}");
                break;
            case AjisPhaseEvent phase:
                Console.WriteLine($"Phase: {phase.Phase} - {phase.Detail}");
                break;
        }
    }
}
```

### 3. Validation and Sanitization

```csharp
public class DataValidator
{
    private readonly AjisConverter<User> _converter;

    public DataValidator()
    {
        var settings = new AjisSettings
        {
            MaxDepth = 10,  // Protection against deep nesting attacks
            AllowDuplicateObjectKeys = false  // Strict validation
        };
        _converter = new AjisConverter<User>(settings);
    }

    public ValidationResult ValidateAndParse(string ajisText)
    {
        try
        {
            var user = _converter.Deserialize(ajisText);
            return new ValidationResult
            {
                IsValid = user != null,
                Data = user,
                Errors = null
            };
        }
        catch (AjisFormatException ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                Data = null,
                Errors = new[] { $"Format error at {ex.Position}: {ex.Message}" }
            };
        }
    }
}

public record ValidationResult(bool IsValid, User? Data, string[]? Errors);
```

---

## 🧪 Testing and QA

### 1. Unit Tests

```csharp
[TestFixture]
public class AjisConverterTests
{
    private AjisConverter<User> _converter;

    [SetUp]
    public void Setup()
    {
        _converter = new AjisConverter<User>();
    }

    [Test]
    public void Serialize_ValidUser_ReturnsJson()
    {
        var user = new User { Id = 1, Name = "Test" };
        var result = _converter.Serialize(user);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("\"Id\":1"));
        Assert.IsTrue(result.Contains("\"Name\":\"Test\""));
    }

    [Test]
    public void Deserialize_ValidJson_ReturnsUser()
    {
        var json = "{\"Id\":1,\"Name\":\"Test\"}";
        var result = _converter.Deserialize(json);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test", result.Name);
    }

    [Test]
    public void Deserialize_InvalidJson_ThrowsException()
    {
        var invalidJson = "{\"Id\":1,\"Name\":}";  // Invalid syntax
        Assert.Throws<AjisFormatException>(() => _converter.Deserialize(invalidJson));
    }
}
```

### 2. Performance Tests

```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    public void Serialize_10kObjects_Under100ms()
    {
        var converter = new AjisConverter<List<User>>();
        var data = GenerateTestData(10000);

        var stopwatch = Stopwatch.StartNew();
        var result = converter.Serialize(data);
        stopwatch.Stop();

        Assert.Less(stopwatch.ElapsedMilliseconds, 100);
        Assert.IsNotNull(result);
    }

    [Test]
    public void RoundTrip_1kObjects_NoDataLoss()
    {
        var converter = new AjisConverter<List<User>>();
        var original = GenerateTestData(1000);

        var json = converter.Serialize(original);
        var deserialized = converter.Deserialize(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(original.Count, deserialized.Count);

        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original[i].Id, deserialized[i].Id);
            Assert.AreEqual(original[i].Name, deserialized[i].Name);
        }
    }
}
```

### 3. Integration Tests

```csharp
[TestFixture]
public class IntegrationTests
{
    [Test]
    public async Task FileRoundTrip_LargeDataset_Success()
    {
        var converter = new AjisConverter<List<User>>();
        var testData = GenerateTestData(50000);

        var tempFile = Path.GetTempFileName();

        try
        {
            // Save to file
            var json = converter.Serialize(testData);
            await File.WriteAllTextAsync(tempFile, json);

            // Load from file
            var loadedJson = await File.ReadAllTextAsync(tempFile);
            var loadedData = converter.Deserialize(loadedJson);

            // Verify
            Assert.IsNotNull(loadedData);
            Assert.AreEqual(testData.Count, loadedData.Count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
```

---

## 🚨 Troubleshooting

### 1. Common Errors

#### "Type X must have a parameterless constructor"
```csharp
// ❌ Bad
public class User
{
    public User(string name) { Name = name; }  // Only parameterized constructor
}

// ✅ Good
public class User
{
    public User() { }  // Parameterless constructor
    public User(string name) { Name = name; }

    public string Name { get; set; }
}
```

#### "Cannot deserialize abstract type"
```csharp
// ❌ Bad
public abstract class Shape { }
public class Circle : Shape { }

// Deserializing abstract class will fail
var shape = converter.Deserialize<Shape>(json);

// ✅ Good - use concrete type
var circle = converter.Deserialize<Circle>(json);
```

#### "Maximum depth exceeded"
```csharp
// Increase MaxDepth in settings
var settings = new AjisSettings { MaxDepth = 1000 };
var converter = new AjisConverter<DeepObject>(settings);
```

### 2. Performance Issues

#### Slow Serialization
```csharp
// Check Pretty formatting
var settings = new AjisSettings
{
    Serialization = new AjisSerializationOptions
    {
        Pretty = false,  // Compact mode is faster
        Compact = true
    }
};
```

#### High Memory Usage
```csharp
// Use streaming for large datasets
// Instead of: var data = converter.Deserialize<List<User>>(largeJson);
// Use: Stream processing with IAsyncEnumerable
```

#### GC Pressure
```csharp
// Reuse converter instances
// Reuse buffers with ArrayPool
// Use object pooling for frequently created objects
```

### 3. Compatibility

#### JSON Compatibility
```csharp
var settings = new AjisSettings
{
    Serialization = new AjisSerializationOptions
    {
        JsonCompatible = true  // Only standard JSON features
    }
};
```

#### Legacy System Integration
```csharp
// For compatibility with Newtonsoft.Json
var settings = new AjisSettings
{
    AllowTrailingCommas = true,  // Newtonsoft allows trailing commas
    Comments = new AjisCommentOptions
    {
        AllowLineComments = true,
        AllowBlockComments = true
    }
};
```

---

## 📚 Advanced Topics

### 1. Custom Type Converters

```csharp
public class MoneyConverter : ICustomAjisConverter<decimal>
{
    public object? ReadJson(ref Utf8JsonReader reader, Type typeToConvert, AjisSerializerOptions options)
    {
        var moneyString = reader.GetString();
        // Parse "$123.45" -> 123.45M
        return decimal.Parse(moneyString.TrimStart('$'));
    }

    public void WriteJson(Utf8JsonWriter writer, decimal value, AjisSerializerOptions options)
    {
        writer.WriteStringValue($"${value:F2}");
    }
}

public class Product
{
    public string Name { get; set; }
    [AjisConverter(typeof(MoneyConverter))]
    public decimal Price { get; set; }
}
```

### 2. Conditional Serialization

```csharp
public class User
{
    public string Name { get; set; }

    [AjisIgnoreIfNull]
    public string? Email { get; set; }

    [AjisIgnoreIfDefault]
    public int Age { get; set; }  // Ignored if 0

    [AjisPropertyName("user_type")]
    public string Type { get; set; }
}
```

### 3. Polymorphic Serialization

```csharp
[AjisDiscriminator("type")]
[AjisKnownType(typeof(Circle), "circle")]
[AjisKnownType(typeof(Square), "square")]
public abstract class Shape
{
    public string Color { get; set; }
}

public class Circle : Shape
{
    public double Radius { get; set; }
}

public class Square : Shape
{
    public double SideLength { get; set; }
}

// AJIS automatically adds discriminator field
// {"type":"circle","Color":"red","Radius":10.0}
```

---

## 🎯 Best Practices

### 1. **Performance**
- ✅ Reuse converter instances
- ✅ Use UTF-8 bytes directly
- ✅ Set Compact = true for APIs
- ✅ Use streaming for > 10MB

### 2. **Reliability**
- ✅ Always handle AjisException
- ✅ Validate input data
- ✅ Set reasonable MaxDepth
- ✅ Use diagnostic events

### 3. **Compatibility**
- ✅ Use JsonCompatible = true for APIs
- ✅ Document custom converters
- ✅ Test round-trip integrity
- ✅ Use semantic versioning

### 4. **Maintenance**
- ✅ Cover with unit tests (min 80%)
- ✅ Monitor performance metrics
- ✅ Use IAjisLogger for debugging
- ✅ Document breaking changes

---

## 📞 Support and Community

### Resources
- **GitHub**: https://github.com/afrowaveltd/Ajis.Dotnet
- **Issues**: For bug reports and feature requests
- **Discussions**: For questions and discussions
- **Wiki**: Extended documentation

### Contact
- **Email**: support@afrowave.com
- **Discord**: AJIS Community
- **Twitter**: @AfrowaveLtd

---

*This documentation is live and regularly updated. For the latest information, visit the GitHub repository.*