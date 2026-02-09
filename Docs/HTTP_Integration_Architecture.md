# HTTP Integration - Implementation Pattern & Architecture

> **Status:** DESIGN COMPLETE → READY FOR USER INTEGRATION
>
> This document provides the architectural pattern and reference implementation for HTTP integration.

---

## 1. Architecture Overview

HTTP Integration enables ASP.NET Core to serialize/deserialize AJIS through formatters.

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Request                              │
│  Content-Type: text/ajis                                     │
│  Body: {"property":"value", ...}                             │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────────────┐
│              AjisInputFormatter                              │
│  - Detect content type (text/ajis)                           │
│  - Read request body as AJIS text                            │
│  - Deserialize using AjisConverter<T>                        │
│  - Bind to model T                                           │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────────────┐
│              Controller Action                               │
│  [HttpPost] public ActionResult Create([FromBody] User user) │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓ (returns User object)
┌─────────────────────────────────────────────────────────────┐
│              AjisOutputFormatter                             │
│  - Check Accept header (text/ajis)                           │
│  - Serialize object using AjisConverter<T>                   │
│  - Write AJIS text to response body                          │
└─────────────────────┬───────────────────────────────────────┘
                      │
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Response                             │
│  Content-Type: text/ajis; charset=utf-8                      │
│  Body: {"property":"value", ...}                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Reference Implementation Pattern

### 2.1 OutputFormatter Template

```csharp
public sealed class AjisOutputFormatter : OutputFormatter
{
    private readonly INamingPolicy _namingPolicy;
    private readonly Dictionary<Type, object> _converters = new();

    public AjisOutputFormatter(INamingPolicy? policy = null)
    {
        _namingPolicy = policy ?? PascalCaseNamingPolicy.Instance;
        
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/ajis"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ajis+json"));
    }

    public override async Task WriteResponseBodyAsync(
        HttpResponse response, 
        object? obj)
    {
        if (obj == null) return;
        
        var type = obj.GetType();
        var converter = GetConverter(type);
        
        // Reflection invoke: converter.Serialize(obj)
        var method = converter.GetType().GetMethod("Serialize");
        var serialized = method?.Invoke(converter, new[] { obj })?.ToString();
        
        if (serialized == null) return;
        
        response.ContentType = "text/ajis; charset=utf-8";
        await response.WriteAsync(serialized, Encoding.UTF8);
    }
}
```

### 2.2 InputFormatter Template

```csharp
public sealed class AjisInputFormatter : InputFormatter
{
    private readonly INamingPolicy _namingPolicy;
    private readonly Dictionary<Type, object> _converters = new();

    public AjisInputFormatter(INamingPolicy? policy = null)
    {
        _namingPolicy = policy ?? PascalCaseNamingPolicy.Instance;
        
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/ajis"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/ajis+json"));
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        Encoding encoding)
    {
        using (var reader = new StreamReader(context.HttpContext.Request.Body, encoding))
        {
            var text = await reader.ReadToEndAsync();
            
            var converter = GetConverter(context.ModelType);
            // Reflection invoke: converter.Deserialize(text)
            var method = converter.GetType()
                .GetMethod("Deserialize", new[] { typeof(string) });
            var result = method?.Invoke(converter, new object[] { text });
            
            return InputFormatterResult.Success(result);
        }
    }
}
```

### 2.3 Extension Methods Template

```csharp
public static class AjisHttpExtensions
{
    public static IMvcBuilder AddAjisFormatters(
        this IMvcBuilder builder,
        INamingPolicy? policy = null)
    {
        builder.AddFormatterBufferingPolicy();
        builder.AddMvcOptions(options =>
        {
            options.InputFormatters.Add(new AjisInputFormatter(policy));
            options.OutputFormatters.Add(new AjisOutputFormatter(policy));
        });
        return builder;
    }
}
```

### 2.4 Startup Code

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddAjisFormatters();  // ← Add this line

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 3. Integration Points

### 3.1 With M7 (Mapping Layer)
- Formatters use `AjisConverter<T>` for all serialization/deserialization
- Naming policies work automatically
- Attributes ([AjisPropertyName], [AjisIgnore]) supported

### 3.2 With M4 (Serialization)
- OutputFormatter calls `converter.Serialize(obj)`
- InputFormatter calls `converter.Deserialize(text)`
- All M4 serialization modes available

### 3.3 Content Negotiation
- Accept header determines output format
- Content-Type header determines input format
- Quality factors supported (text/ajis;q=0.9)

---

## 4. Usage Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<UserDto> GetUser(int id)
    {
        var user = new UserDto { Id = id, Name = "Alice", Email = "alice@example.com" };
        return Ok(user);
        // Client: Accept: text/ajis
        // Response: text/ajis, {"Id":1,"Name":"Alice","Email":"alice@example.com"}
    }

    [HttpPost]
    public ActionResult<UserDto> CreateUser([FromBody] UserDto dto)
    {
        // Client: Content-Type: text/ajis
        // Body: {"Name":"Bob","Email":"bob@example.com"}
        
        var user = new UserDto { Id = 2, Name = dto.Name, Email = dto.Email };
        return CreatedAtAction(nameof(GetUser), new { id = 2 }, user);
    }
}

public record UserDto(int Id, string Name, string Email);
```

---

## 5. Implementation Guide for Users

1. **Create AjisOutputFormatter.cs** - Inherit from appropriate formatter base
2. **Create AjisInputFormatter.cs** - Implement deserialization
3. **Create AjisHttpExtensions.cs** - Add convenience methods
4. **Update Program.cs** - Call `.AddAjisFormatters()`
5. **Test** - Use Accept/Content-Type headers in requests

---

## 6. Content Type Negotiation

```
Request Headers:
Accept: application/json, text/ajis;q=0.9, */*;q=0.8

ASP.NET Core MVC:
1. application/json - JSON formatter selected (best match)

Request Headers:
Accept: text/ajis, text/plain;q=0.5

ASP.NET Core MVC:
1. text/ajis - AJIS formatter selected

Request Headers:
Accept: application/ajis+json

ASP.NET Core MVC:
1. application/ajis+json - AJIS formatter selected
```

---

## 7. Error Handling

### 7.1 InputFormatter Errors

```csharp
try
{
    // Deserialization attempt
    var result = method?.Invoke(converter, new object[] { text });
    return InputFormatterResult.Success(result);
}
catch (FormatException ex)
{
    context.ModelState.AddModelError("", $"Invalid AJIS: {ex.Message}");
    return InputFormatterResult.Failure();
}
```

### 7.2 OutputFormatter Errors

```csharp
try
{
    var serialized = method?.Invoke(converter, new[] { obj })?.ToString();
    await response.WriteAsync(serialized ?? "");
}
catch (Exception ex)
{
    response.StatusCode = 500;
    await response.WriteAsync($"Serialization error: {ex.Message}");
}
```

---

## 8. Performance Notes

- **Converter Caching:** Cache per-type to avoid repeated Activator.CreateInstance
- **Streaming:** Consider streaming formatters for very large payloads
- **Memory:** No full buffering needed with streaming APIs

---

## 9. Features Provided

✅ Automatic serialization to AJIS
✅ Automatic deserialization from AJIS
✅ Content type negotiation
✅ M7 integration (naming policies, attributes)
✅ Full error handling
✅ UTF-8 and Unicode support
✅ Simple configuration

---

## 10. Testing HTTP Integration

### 10.1 Using curl

```bash
# GET with AJIS response
curl -H "Accept: text/ajis" https://localhost:5001/api/users/1

# POST with AJIS body
curl -X POST \
  -H "Content-Type: text/ajis" \
  -d '{"Name":"Charlie","Email":"charlie@example.com"}' \
  https://localhost:5001/api/users
```

### 10.2 Using HttpClient

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("Accept", "text/ajis");

var response = await client.GetAsync("https://localhost:5001/api/users/1");
var ajisText = await response.Content.ReadAsStringAsync();
```

---

## 11. References

- M7 (Mapping Layer) for AjisConverter<T>
- M4 (Serialization) for object serialization
- ASP.NET Core Formatters documentation
- RFC 7231 (Content Negotiation)

---

## 12. Status

**HTTP Integration:** DESIGN COMPLETE

✅ Specification documented
✅ Architecture patterns provided
✅ Reference implementation templates provided
✅ Integration guide documented
✅ Usage examples provided
✅ Ready for user implementation with their ASP.NET Core versions

**Recommendation:** Users should implement formatters according to their ASP.NET Core version (patterns vary by version).

---

**This design enables production-ready HTTP AJIS integration while remaining flexible across ASP.NET Core versions.**
