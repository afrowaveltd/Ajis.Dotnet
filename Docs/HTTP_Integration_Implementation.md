# HTTP Integration – ASP.NET Core Formatters Implementation

> **Status:** IN PROGRESS → COMPLETION
>
> This document defines HTTP integration for AJIS with ASP.NET Core via input/output formatters.

---

## 1. HTTP Integration Scope

HTTP Integration enables ASP.NET Core web applications to automatically serialize/deserialize objects to/from AJIS format in HTTP requests and responses.

**Core Responsibilities:**

* OutputFormatter for HTTP response serialization
* InputFormatter for HTTP request deserialization
* Content type negotiation (`text/ajis`, `application/ajis+json`)
* Automatic model binding
* Dependency injection integration
* Error handling and validation
* Character encoding support

---

## 2. Design Goals

### 2.1 Drop-In Replacement
Developers familiar with JSON formatters should feel at home:
```csharp
services.AddAjisFormatters();

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<User> Get(int id) { ... }
    
    [HttpPost]
    public ActionResult<User> Create([FromBody] User user) { ... }
}
```

### 2.2 Content Negotiation
Support multiple AJIS content types:
- `text/ajis` - Plain AJIS text
- `application/ajis+json` - AJIS as JSON subtype
- Quality factors: `text/ajis;q=0.9`

### 2.3 Performance
- No unnecessary allocations
- Streaming where possible
- Cached converters
- Reused formatters

### 2.4 Flexibility
- Configurable naming policies per type
- Custom converter support
- Error handling policies
- Default settings per formatter

---

## 3. ASP.NET Core Formatter Architecture

### 3.1 OutputFormatter (Response Serialization)

```csharp
public class AjisOutputFormatter : TextOutputFormatter
{
    public AjisOutputFormatter();
    public AjisOutputFormatter(AjisSettings settings);
    public AjisOutputFormatter(INamingPolicy namingPolicy);
    
    public override async Task WriteResponseBodyAsync(
        OutputFormatterWriteContext context,
        Encoding selectedEncoding);
}
```

**Responsibilities:**
- Register supported content types
- Check if object can be serialized
- Serialize to AJIS
- Write to response stream
- Handle errors

### 3.2 InputFormatter (Request Deserialization)

```csharp
public class AjisInputFormatter : TextInputFormatter
{
    public AjisInputFormatter();
    public AjisInputFormatter(AjisSettings settings);
    public AjisInputFormatter(INamingPolicy namingPolicy);
    
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        Encoding encoding);
}
```

**Responsibilities:**
- Register supported content types
- Check if model type is supported
- Read from request stream
- Deserialize from AJIS
- Validate and return result
- Handle errors

### 3.3 Extension Methods

```csharp
public static class AjisFormatterExtensions
{
    /// <summary>
    /// Registers AJIS input and output formatters with default settings.
    /// </summary>
    public static IMvcBuilder AddAjisFormatters(this IMvcBuilder builder);
    
    /// <summary>
    /// Registers AJIS formatters with custom settings.
    /// </summary>
    public static IMvcBuilder AddAjisFormatters(
        this IMvcBuilder builder,
        AjisSettings settings);
    
    /// <summary>
    /// Registers AJIS formatters with custom naming policy.
    /// </summary>
    public static IMvcBuilder AddAjisFormatters(
        this IMvcBuilder builder,
        INamingPolicy namingPolicy);
}
```

---

## 4. Content Types

### 4.1 Supported Content Types

**Standard AJIS:**
- `text/ajis` - Primary format
- `text/ajis;charset=utf-8` - With charset

**AJIS as JSON subtype:**
- `application/ajis+json` - RFC 6839 compliant
- `application/ajis+json;charset=utf-8`

**Quality factors:**
- `text/ajis;q=0.9` - Slightly lower priority than JSON
- `application/ajis+json;q=0.8`

### 4.2 Content Negotiation Example

```
Request headers:
Accept: application/json;q=1.0, text/ajis;q=0.9, */*;q=0.1

Result: JSON selected (highest quality)

Request headers:
Accept: text/ajis;q=0.9, */*;q=0.1

Result: AJIS selected (only format with specific match)
```

---

## 5. Integration with M7 (Mapping Layer)

OutputFormatter and InputFormatter use AjisConverter<T> for all mapping:

```csharp
public class AjisOutputFormatter : TextOutputFormatter
{
    private readonly ConcurrentDictionary<Type, object> _converters = new();
    
    private AjisConverter<T> GetConverter<T>() where T : notnull
    {
        return (AjisConverter<T>)_converters.GetOrAdd(typeof(T), 
            _ => new AjisConverter<T>(_namingPolicy));
    }
}
```

This means:
- ✅ Naming policies work automatically
- ✅ Attributes ([AjisPropertyName], [AjisIgnore]) work
- ✅ Custom converters supported
- ✅ Type safety maintained

---

## 6. Error Handling

### 6.1 Deserialization Errors

```csharp
try
{
    var converter = GetConverter<T>();
    var obj = converter.Deserialize(ajisText);
    return InputFormatterResult.Success(obj);
}
catch (FormatException ex)
{
    context.ModelState.TryAddModelError(
        context.ModelName,
        $"Invalid AJIS format: {ex.Message}");
    return InputFormatterResult.Failure();
}
```

### 6.2 Serialization Errors

Handle exceptions during serialization and return 500 with details.

---

## 7. Completeness Checklist

### 7.1 Core Formatters
- [ ] AjisOutputFormatter implementation
- [ ] AjisInputFormatter implementation
- [ ] Content type registration
- [ ] Full XML documentation

### 7.2 Extension Methods
- [ ] AddAjisFormatters() method
- [ ] Settings-based configuration
- [ ] Naming policy configuration
- [ ] Full XML documentation

### 7.3 Features
- [ ] Streaming support
- [ ] Encoding support (UTF-8)
- [ ] Error handling
- [ ] Converter caching
- [ ] Content negotiation

### 7.4 Testing
- [ ] GET endpoint tests (serialization)
- [ ] POST endpoint tests (deserialization)
- [ ] Content negotiation tests
- [ ] Error handling tests
- [ ] Integration with sample controller

### 7.5 Documentation
- [ ] HTTP Integration specification
- [ ] Setup instructions
- [ ] Usage examples
- [ ] Configuration options

---

## 8. Example Usage

### 8.1 Startup Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add AJIS formatters with default settings
builder.Services.AddControllers()
    .AddAjisFormatters();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 8.2 Controller Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<UserResponse> GetUser(int id)
    {
        var user = new UserResponse { Id = id, Name = "Alice" };
        return Ok(user);
        // Client can request: Accept: text/ajis
        // Response: {"Id":1,"Name":"Alice"} as AJIS
    }
    
    [HttpPost]
    public ActionResult<UserResponse> CreateUser([FromBody] UserRequest request)
    {
        // Request Content-Type: text/ajis
        // Body: {"Name":"Bob","Email":"bob@example.com"}
        var response = new UserResponse { Id = 2, Name = request.Name };
        return CreatedAtAction(nameof(GetUser), new { id = 2 }, response);
    }
}
```

### 8.3 Client Example (HTTP)

```bash
# Get in AJIS format
curl -H "Accept: text/ajis" https://api.example.com/api/users/1

# Post in AJIS format
curl -X POST \
  -H "Content-Type: text/ajis" \
  -d '{"Name":"Charlie","Email":"charlie@example.com"}' \
  https://api.example.com/api/users
```

### 8.4 Client Example (C#)

```csharp
// Using HttpClient
var client = new HttpClient();
client.DefaultRequestHeaders.Add("Accept", "text/ajis");

var response = await client.GetAsync("https://api.example.com/api/users/1");
var ajisContent = await response.Content.ReadAsStringAsync();

// Deserialize
var converter = new AjisConverter<User>();
var user = converter.Deserialize(ajisContent);
```

---

## 9. Performance Targets

- **Serialization:** <1ms for 1KB object
- **Deserialization:** <1ms for 1KB AJIS
- **Memory:** No unnecessary allocations
- **Throughput:** >10,000 req/sec on modest hardware

---

## 10. References

* M4 (Serialization) for output formatting
* M7 (Mapping Layer) for AjisConverter<T>
* ASP.NET Core documentation
* RFC 2119 (MUST, SHOULD, MAY)
* RFC 6839 (subtype registration)

---

**Next Step:** Analyze ASP.NET Core formatter infrastructure.
