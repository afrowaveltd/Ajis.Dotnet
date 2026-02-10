# AJIS User Guide - Kompletní uživatelská příručka

## 🚀 Úvod do AJIS

**AJIS (Afrowave JSON-like Interchange Specification)** je high-performance formát pro výměnu dat, inspirovaný JSON, ale optimalizovaný pro enterprise scénáře s velkými daty, streaming a přesnými diagnostikami.

### ✅ Kdy použít AJIS

- **Velké datasety** (stovky MB až GB)
- **Streaming aplikace** (real-time processing)
- **Enterprise systémy** (přesné diagnostiky, rozšiřitelnost)
- **Low-memory prostředí** (embedded, IoT)
- **Dlouhodobá archivace** (normativní specifikace)

### ❌ Kdy použít JSON místo AJIS

- Jednoduché REST API
- Malé konfigurační soubory
- Maximální kompatibilita s existujícími nástroji

---

## 📦 Základní použití

### 1. Instalace

```bash
# Přidejte NuGet balíček
dotnet add package Afrowave.AJIS
```

### 2. Základní serializace/deserializace

```csharp
using Afrowave.AJIS.Serialization.Mapping;

// Definujte svůj model
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Vytvořte converter
var converter = new AjisConverter<User>();

// Serializujte objekt
var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
string ajisText = converter.Serialize(user);

// Deserializujte zpět
User? deserializedUser = converter.Deserialize(ajisText);
```

### 3. Práce s kolekcemi

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

## ⚙️ Konfigurace a nastavení

### 1. Základní konfigurace

```csharp
var settings = new AjisSettings
{
    // Povolit trailing commas v arrays a objektech
    AllowTrailingCommas = true,

    // Povolit komentáře
    Comments = new AjisCommentOptions
    {
        AllowLineComments = true,      // // komentáře
        AllowBlockComments = true     // /* komentáře */
    },

    // Nastavit maximální hloubku
    MaxDepth = 100,

    // Formátování výstupu
    Serialization = new AjisSerializationOptions
    {
        Pretty = true,        // Čitelný formát
        IndentSize = 2        // 2 mezery na úroveň
    }
};

var converter = new AjisConverter<User>(settings);
```

### 2. Naming policies

```csharp
// PascalCase (výchozí)
var pascalConverter = new AjisConverter<User>(PascalCaseNamingPolicy.Instance);

// camelCase pro JavaScript kompatibilitu
var camelConverter = new AjisConverter<User>(CamelCaseNamingPolicy.Instance);

// Custom naming policy
public class KebabCaseNamingPolicy : IAjisNamingPolicy
{
    public string ConvertName(string name)
    {
        // Implementujte kebab-case konverzi
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}

var kebabConverter = new AjisConverter<User>(new KebabCaseNamingPolicy());
```

### 3. Processing profily

```csharp
var settings = new AjisSettings
{
    // Pro server aplikace (high-throughput)
    ParserProfile = AjisProcessingProfile.Server,
    SerializerProfile = AjisProcessingProfile.Server,

    // Pro desktop aplikace (balanced)
    ParserProfile = AjisProcessingProfile.Desktop,
    SerializerProfile = AjisProcessingProfile.Desktop,

    // Pro embedded systémy (low-memory)
    ParserProfile = AjisProcessingProfile.Embedded,
    SerializerProfile = AjisProcessingProfile.Embedded,

    // Universal (výchozí - auto-selection)
    ParserProfile = AjisProcessingProfile.Universal,
    SerializerProfile = AjisProcessingProfile.Universal
};
```

---

## 🔧 Pokročilé scénáře

### 1. Custom converters

```csharp
// Vlastní converter pro DateTime
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

// Registrace custom converteru
var converter = new AjisConverter<User>()
    .RegisterConverter(new CustomDateTimeConverter());
```

### 2. Práce s enum typy

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

// Enums se serializují jako string hodnoty
var user = new UserWithRole { Name = "Alice", Role = UserRole.Admin };
var converter = new AjisConverter<UserWithRole>();
string json = converter.Serialize(user);
// {"Name":"Alice","Role":"Admin"}
```

### 3. Nullable typy a výchozí hodnoty

```csharp
public class OptionalUser
{
    public string Name { get; set; } = "";
    public int? Age { get; set; }          // Nullable int
    public string? Email { get; set; }     // Nullable string
    public List<string>? Tags { get; set; } // Nullable kolekce
}

// AJIS automaticky handluje null hodnoty
var user = new OptionalUser { Name = "Bob" };
var converter = new AjisConverter<OptionalUser>();
string json = converter.Serialize(user);
// {"Name":"Bob","Age":null,"Email":null,"Tags":null}
```

### 4. Komplexní nested objekty

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

// AJIS automaticky handluje libovolně hluboké nesting
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

## 📊 Performance optimalizace

### 1. Reuse converter instances

```csharp
// ❌ Špatně - vytváření nové instance pro každý request
public string ProcessRequest(string json)
{
    var converter = new AjisConverter<User>();  // NOVÁ INSTANCE!
    var user = converter.Deserialize(json);
    return converter.Serialize(user);
}

// ✅ Dobře - reuse jedné instance
private static readonly AjisConverter<User> _userConverter = new();

public string ProcessRequest(string json)
{
    var user = _userConverter.Deserialize(json);
    return _userConverter.Serialize(user);
}
```

### 2. UTF-8 optimalizace

```csharp
// Pro vysoký výkon použijte UTF-8 bytes přímo
var converter = new AjisConverter<User>();

// Serializace do bytes
using var stream = new MemoryStream();
using var writer = new Utf8JsonWriter(stream);
converter.SerializeToUtf8(writer, user);
byte[] utf8Bytes = stream.ToArray();

// Deserializace z bytes
var readOnlySpan = new ReadOnlySpan<byte>(utf8Bytes);
User? user = converter.DeserializeFromUtf8(readOnlySpan);
```

### 3. Streaming pro velké soubory

```csharp
// Pro soubory > 100MB použijte streaming
using var fileStream = File.OpenRead("large_file.json");
using var jsonReader = new Utf8JsonReader(fileStream);

// Stream processing - nezabírá celou paměť
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
        // ... další tokeny
    }
}
```

### 4. Memory pooling

```csharp
// Pro vysoký throughput použijte ArrayPool
var settings = new AjisSettings
{
    // AJIS automaticky používá ArrayPool pro velké alokace
    StreamChunkThreshold = "1G"  // Memory-mapped pro > 1GB
};

var converter = new AjisConverter<LargeData>(settings);
```

---

## 🌐 Web a API scénáře

### 1. ASP.NET Core integration

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

### 2. HttpClient s AJIS

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

## 💾 Souborové operace

### 1. Čtení/zápis souborů

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

    // Pro velké soubory - streaming
    public async IAsyncEnumerable<User> StreamUsersFromFileAsync(string filePath)
    {
        using var fileStream = File.OpenRead(filePath);
        using var jsonReader = new Utf8JsonReader(fileStream);

        // Přeskočit na začátek array
        while (jsonReader.Read() && jsonReader.TokenType != JsonTokenType.StartArray) { }

        while (jsonReader.Read())
        {
            if (jsonReader.TokenType == JsonTokenType.StartObject)
            {
                // Parse jednotlivý User objekt
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
        // Implementace stream parsing pro jednotlivé objekty
        // ... (zjednodušeno pro ukázku)
        return null;
    }
}
```

### 2. Komprese a archivace

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

## 🔍 Diagnostika a error handling

### 1. Základní error handling

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

### 2. Detailní diagnostika

```csharp
var settings = new AjisSettings
{
    EventSink = new ConsoleEventSink(),  // Log všechny eventy
    Logger = new ConsoleLogger()          // Log všechny zprávy
};

var converter = new AjisConverter<User>(settings);

// Custom event sink pro detailní tracking
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

### 3. Validation a sanitizace

```csharp
public class DataValidator
{
    private readonly AjisConverter<User> _converter;

    public DataValidator()
    {
        var settings = new AjisSettings
        {
            MaxDepth = 10,  // Ochrana před deep nesting útoky
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

## 🧪 Testování a QA

### 1. Unit testy

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
        var invalidJson = "{\"Id\":1,\"Name\":}";  // Neplatná syntax
        Assert.Throws<AjisFormatException>(() => _converter.Deserialize(invalidJson));
    }
}
```

### 2. Performance testy

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

### 3. Integration testy

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

### 1. Časté chyby

#### "Type X must have a parameterless constructor"
```csharp
// ❌ Špatně
public class User
{
    public User(string name) { Name = name; }  // Pouze konstruktor s parametry
}

// ✅ Dobře
public class User
{
    public User() { }  // Parameterless konstruktor
    public User(string name) { Name = name; }

    public string Name { get; set; }
}
```

#### "Cannot deserialize abstract type"
```csharp
// ❌ Špatně
public abstract class Shape { }
public class Circle : Shape { }

// Deserializace abstraktní třídy selže
var shape = converter.Deserialize<Shape>(json);

// ✅ Dobře - použijte konkrétní typ
var circle = converter.Deserialize<Circle>(json);
```

#### "Maximum depth exceeded"
```csharp
// Zvětšete MaxDepth v settings
var settings = new AjisSettings { MaxDepth = 1000 };
var converter = new AjisConverter<DeepObject>(settings);
```

### 2. Performance problémy

#### Pomalá serializace
```csharp
// Zkontrolujte Pretty formatting
var settings = new AjisSettings
{
    Serialization = new AjisSerializationOptions
    {
        Pretty = false,  // Compact mode je rychlejší
        Compact = true
    }
};
```

#### Vysoká paměťová spotřeba
```csharp
// Použijte streaming pro velké datasety
// Místo: var data = converter.Deserialize<List<User>>(largeJson);
// Použijte: Stream processing s IAsyncEnumerable
```

#### GC pressure
```csharp
// Reuse converter instances
// Reuse buffers s ArrayPool
// Použijte object pooling pro často vytvářené objekty
```

### 3. Kompatibilita

#### JSON kompatibilita
```csharp
var settings = new AjisSettings
{
    Serialization = new AjisSerializationOptions
    {
        JsonCompatible = true  // Pouze standard JSON features
    }
};
```

#### Legacy systém integrace
```csharp
// Pro kompatibilitu s Newtonsoft.Json
var settings = new AjisSettings
{
    AllowTrailingCommas = true,  // Newtonsoft umožňuje trailing commas
    Comments = new AjisCommentOptions
    {
        AllowLineComments = true,
        AllowBlockComments = true
    }
};
```

---

## 📚 Pokročilé témata

### 1. Custom type converters

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

### 2. Conditional serialization

```csharp
public class User
{
    public string Name { get; set; }

    [AjisIgnoreIfNull]
    public string? Email { get; set; }

    [AjisIgnoreIfDefault]
    public int Age { get; set; }  // Ignoruje se pokud je 0

    [AjisPropertyName("user_type")]
    public string Type { get; set; }
}
```

### 3. Polymorfní serializace

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

// AJIS automaticky přidá discriminator field
// {"type":"circle","Color":"red","Radius":10.0}
```

---

## 🎯 Best practices

### 1. **Výkon**
- ✅ Reuse converter instances
- ✅ Používejte UTF-8 bytes přímo
- ✅ Nastavte Compact = true pro API
- ✅ Používejte streaming pro > 10MB

### 2. **Spolehlivost**
- ✅ Vždy handlujte AjisException
- ✅ Validujte vstupní data
- ✅ Nastavte rozumné MaxDepth
- ✅ Používejte diagnostické eventy

### 3. **Kompatibilita**
- ✅ Používejte JsonCompatible = true pro API
- ✅ Dokumentujte custom converters
- ✅ Testujte round-trip integritu
- ✅ Používejte semantic versioning

### 4. **Údržba**
- ✅ Pokrývejte unit testy (min 80%)
- ✅ Monitorujte performance metriky
- ✅ Používejte IAjisLogger pro debugging
- ✅ Dokumentujte breaking changes

---

## 📞 Podpora a komunita

### Zdroje
- **GitHub**: https://github.com/afrowaveltd/Ajis.Dotnet
- **Issues**: Pro bug reporty a feature requests
- **Discussions**: Pro otázky a diskuse
- **Wiki**: Rozšířená dokumentace

### Kontakt
- **Email**: support@afrowave.com
- **Discord**: AJIS Community
- **Twitter**: @AfrowaveLtd

---

*Tato dokumentace je živá a pravidelně aktualizovaná. Pro nejnovější informace navštivte GitHub repository.*