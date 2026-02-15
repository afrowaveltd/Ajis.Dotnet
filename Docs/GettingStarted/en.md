# Getting Started with AJIS .NET

> Quick start guide for AJIS .NET libraries

---

## Install packages

Install the core package and any additional libraries you need:

```bash
# Core library (required)
dotnet add package Afrowave.AJIS.Core

# Choose additional packages based on needs:
dotnet add package Afrowave.AJIS.Streaming
dotnet add package Afrowave.AJIS.Serialization
dotnet add package Afrowave.AJIS.IO
dotnet add package Afrowave.AJIS.Net
dotnet add package Afrowave.AJIS.EntityFramework
dotnet add package Afrowave.AJIS.MongoDB
```

---

## Quick examples

### Parse AJIS text

```csharp
using System.Text;
using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming;

string ajisText = """
{
    name: "John Doe"
    age: 35
    email: "john@example.com"
}
""";

var bytes = Encoding.UTF8.GetBytes(ajisText);
using var ms = new MemoryStream(bytes);
using var reader = new StreamReader(ms);

var parser = new AjisLexerParserStreamingAsync(reader);
await foreach (var segment in parser.ParseAsync())
{
    Console.WriteLine($"{segment.Kind}: {segment.Value}");
}
```

### Serialize object to AJIS

```csharp
using Afrowave.AJIS.Serialization;

public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var user = new User { Name = "John Doe", Age = 35 };
var serializer = new AjisSerializer();

// Serialize to AJIS text
string ajisText = serializer.Serialize(user);
```

### Use with file I/O

```csharp
using Afrowave.AJIS.IO;
using System.Collections.Generic;

public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var users = new List<User>
{
    new() { Name = "Alice", Age = 30 },
    new() { Name = "Bob", Age = 25 }
};

// Write to file
AjisFile.Create("users.ajis", users);

// Read from file
var loadedUsers = AjisFile.ReadAll<User>("users.ajis");
```

### ASP.NET Core integration

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddAjisFormatters();

// Controller
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
}
```

---

## Common scenarios

### Working with large files

```csharp
// Streaming read for large files
await foreach (var user in AjisFile.ReadAsync<User>("large-file.ajis"))
{
    ProcessUser(user);
}
```

### Query AJIS files with LINQ

```csharp
var users = AjisFile.ReadAll<User>("users.ajis");

var result = from u in users
             where u.Age > 25
             orderby u.Name
             select u;
```

### Use type mapping with attributes

```csharp
public class User
{
    [AjisPropertyName("user_name")]
    public string Name { get; set; }

    [AjisIgnore]
    public string InternalId { get; set; }
}
```

### Configure serialization options

```csharp
var options = new AjisSerializationOptions
{
    Compact = false,      // Add spaces
    Pretty = true,        // Add newlines and indentation
    Canonicalize = true   // Order properties
};

string ajisText = serializer.Serialize(user, options);
```

---

## Next steps

- **API Reference:** See `Docs/API/` for detailed API documentation
- **Configuration:** See `Docs/Configuration/en.md` for configuration options
- **Performance:** See `Docs/Performance/en.md` for performance tips
- **Architecture:** See `Docs/Architecture/en.md` for architecture overview

---

## Need help?

- GitHub Issues: [repository](https://github.com/ajis/dotnet/issues)
- Full documentation: `Docs/` folder
