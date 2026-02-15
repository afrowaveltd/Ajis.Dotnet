# AJIS CONTEXT - Univerzální nástroj pro práci s AJIS a ATP soubory

Podobně jako Entity Framework Core (EF Core) pro databáze, AJIS Context je univerzální nástroj pro práci s textovými AJIS a binárními ATP soubory. Návrh je inspirován EF Core, ale přizpůsoben specifickuAJIS/ATP formátům.

## Hlavní vlastnosti

- **Podobné EF Core API**: `AjisContext`, `AjisSet<T>`, `Add`, `Update`, `Remove`, `Find`, atd.
- **Automatická detekce formátu**:自动 rozhodne mezi AJIS a ATP na základě koncovky souboru a obsahu
- **Explicitní výběr formátu**: Vývojář může vynutit použití AJIS nebo ATP
- **Práce s binárními přílohami**: Efektivní podpora ATP souboru s vlastními binárními daty (bez base64)
- **Souborové zamykání**: Bezpečné zápisy s vyloučením konfliktů
- **Async/Await**: Celé API je plně asynchronní
- **LINQ-like operátory**: `Count`, `Any`, `Find`, `Where`, atd.

## Jak to funguje

```
┌─────────────────────────────────────────────────────────────┐
│                    AjisContext                              │
│  - Vytváří a spravuje AjisSet<T> pro každý soubor          │
│  - Umožňuje uložit změny z více setů najednou              │
│  - Automatická detekce formátu (Auto/Ajis/Atp)            │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴────────────┬──────────────────┐
         │                        │                  │
    AjisSet<User>           AjisSet<Order>      AjisSet<Product>
         │                        │                  │
         │                        │                  │
  ┌──────▼──────┐        ┌───────▼──────┐      ┌─────▼──────┐
  │ AjisFile    │        │ AtpFile     │      │ AjisFile   │
  │ DataSource  │        │ DataSource  │      │ DataSource │
  │ (Text-only) │        │ (With bin.) │      │ (Text-only)│
  └─────────────┘        └─────────────┘      └────────────┘
```

## Filozofie AJIS vs ATP

| Vlastnost | AJIS | ATP |
|-----------|------|-----|
| Typ dat | Čistě textová/data | Text + binární přílohy |
| Formát | JSON-like | Binární s nativními přílohami |
| Výhoda | Jednoduchost, čitelnost | Efektivita, bez base64 |
| Použití | Konfigurace, metadata | Dokumenty s soubory, přílohy |

### Automatické rozhodování:

1. **Soubor `.atp`** → vždy použije ATP
2. **Soubor `.ajis` nebo `.json` s binárními přílohami** → automaticky použije ATP
3. **Soubor `.ajis` nebo `.json` bez binárních příloh** → použije AJIS
4. **Vývojářský override** → explicitní výběr formátu

## Ukázky použití

### 1. Základní CRUD operace

```csharp
using var context = new AjisContext();

// Získání "Setu" pro entitu User
var users = context.Set<User>("users.ajis");

// CREATE - Přidání nového uživatele
await users.AddAsync(new User { Id = 1, Name = "Alice" });

// Uložení změn do souboru
await context.SaveChangesAsync();

// READ - Načtení uživatele podle ID
var user = await users.FindAsync(1);

// UPDATE - Úprava existujícího uživatele
user.Age = 26;
await users.UpdateAsync(user);
await context.SaveChangesAsync();

// DELETE - Odstranění uživatele
await users.RemoveAsync(user);
await context.SaveChangesAsync();

// AGREGACE - Počet uživatelů
var count = await users.CountAsync();
```

### 2. Práce s binárními přílohami (ATP)

```csharp
using var context = new AjisContext();
var docs = context.Set<Document>("documents.atp");

// Vytvoření binární přílohy
var fileBytes = File.ReadAllBytes("document.pdf");
var doc = new Document
{
    Id = Guid.NewGuid(),
    Name = " Dokument PDF",
    File = BinaryAttachment.FromBytes(fileBytes, "document.pdf", "application/pdf")
};

// Přidání dokumentu s přílohou
await docs.AddAsync(doc);
await context.SaveChangesAsync();

// Načtení dokumentu s přílohou
var loadedDoc = await docs.FindAsync(doc.Id);
if (loadedDoc != null)
{
    // Přístup k binárním datům
    var attachmentData = loadedDoc.File.Data;
    var fileName = loadedDoc.File.FileName;
    var mimeType = loadedDoc.File.MimeType;
    
    // Uložení přílohy na disk
    await loadedDoc.File.SaveToFileAsync("saved_document.pdf");
}
```

### 3. Automatická detekce formátu

```csharp
using var context = new AjisContext();

// Soubor .ajis → automaticky použije AJIS (textový formát)
var users = context.Set<User>("users.ajis");

// Soubor .atp → automaticky použije ATP (s binárními přílohami)
var docs = context.Set<Document>("documents.atp");
```

### 4. Explicitní přepsání formátu

```csharp
using var context = new AjisContext();

// Přestože koncovka je .ajis, vynutíme ATP formát
var users = context.Set<User>("users.ajis", AjisFormat.Atp);

// Vynutíme AJIS formát i pro .atp soubor (nebezpečné s binárními daty!)
var docs = context.Set<Document>("docs.atp", AjisFormat.Ajis);
```

### 5. Konfigurace s binárními přílohami

```csharp
using var context = new AjisContext();

// Vytvoření konfigurace
var config = new AjisEntityConfiguration<User>();
config.Key(u => u.Id); // Primární klíč
config.Property(u => u.Name).IsRequired(); // Povinná vlastnost
config.BinaryAttachment(u => u.Photo); // Binární příloha

// Konverze .ajis s binárními přílohami automaticky použije ATP
var users = context.Set<User>("users.ajis", config);
Console.WriteLine($"Formát: {users.Format}"); // Vypíše: ATP
```

### 6. Vícenásobné zdroje dat

```csharp
using var context = new AjisContext();

// Každý soubor má vlastní data
var users1 = context.Set<User>("users1.ajis");
var users2 = context.Set<User>("users2.ajis");

// Přidání různých dat do každého souboru
await users1.AddAsync(new User { Id = 1, Name = "Alice" });
await users2.AddAsync(new User { Id = 2, Name = "Bob" });

await context.SaveChangesAsync();

// Každý soubor obsahuje jen svá data
Console.WriteLine($"Users1: {await users1.CountAsync()}"); // 1
Console.WriteLine($"Users2: {await users2.CountAsync()}"); // 1
```

### 7. Vícenásobné zápisy (batch operations)

```csharp
using var context = new AjisContext();

// Přidání kategorií
var categories = context.Set<Category>("categories.ajis");
await categories.AddRangeAsync(new[]
{
    new Category { Id = 1, Name = "Electronics" },
    new Category { Id = 2, Name = "Books" },
    new Category { Id = 3, Name = "Clothing" }
});

// Přidání produktů
var products = context.Set<Product>("products.ajis");
await products.AddRangeAsync(new[]
{
    new Product { Id = 1, Name = "Laptop", CategoryId = 1 },
    new Product { Id = 2, Name = "Book", CategoryId = 2 }
});

// Jedním voláním uloží všechny změny
await context.SaveChangesAsync();
```

## Třídy a rozhraní

### Hlavní třídy

- **`AjisContext`**: Hlavní třída pro práci s AJIS/ATP soubory (podobně jako DbContext)
- **`AjisSet<T>`**: Kolekce entit pro CRUD operace (podobně jako DbSet<T>)
- **`AjisEntityConfiguration<T>`**: Konfigurace entity (klíče, povinné vlastnosti, binární přílohy)

### Zdroje dat

- **`AjisFileDataSource<T>`**: Implementace pro čistě textová AJIS data
- **`AtpFileDataSource<T>`**: Implementace pro ATP soubory s binárními přílohami
- **`AutoDetectDataSource`**: Automatické rozhodování mezi AJIS a ATP (budoucnost)

### Podpora binárních příloh

- **`BinaryAttachment`**: Třída pro reprezentaci binární přílohy
- **`BinaryAttachmentExtensions`**: Rozšiřující metody pro práci se streams, byte[], atd.

## Rozhraní

- **`IAjisDataSource<T>`**: Rozhraní pro všechny zdroje dat
- **`IAjisDataSource`**: Non-generic rozhraní pro batch operace

## Podpora binárních příloh v BinaryAttachment

```csharp
// Vytvoření z různých zdrojů
var fromBytes = BinaryAttachment.FromBytes(data, "file.txt", "text/plain");
var fromStream = BinaryAttachment.FromStream(stream, "file.txt", "text/plain");
var fromFile = AjisAttachmentHelper.CreateFromFile("path/to/file.txt");

// Uložení na disk
await attachment.SaveToFileAsync("output.txt");
AjisAttachmentHelper.SaveToFile(attachment, "output.txt");

// Přístup k datům
var data = attachment.Data;
var fileName = attachment.FileName;
var mimeType = attachment.MimeType;
var size = attachment.FileSize;

// Base64 konverze
var base64 = attachment.ToBase64String();
var fromBase64 = BinaryAttachment.FromBase64String(base64);

// Práce jako stream
using var stream = attachment.OpenReadStream();
```

## Typické scénáře použití

### Scénář 1: Čtení XML dat a převod na AJIS

```csharp
// Přečíst existing users.ajis
using var context = new AjisContext();
var users = context.Set<User>("users.ajis");

// Zobrazit všechny uživatele
await foreach (var user in users)
{
    Console.WriteLine($"{user.Name} ({user.Age})");
}
```

### Scénář 2: Konverze JSON s base64 binárními přílohami na ATP

```csharp
// automaticky konvertuje a použije ATP
using var context = new AjisContext();
var docs = context.Set<Document>("documents.atp");

// Přidání dokumentu s binární přílohou
var doc = new Document
{
    Id = Guid.NewGuid(),
    File = BinaryAttachment.FromBytes(fileData, "document.pdf", "application/pdf")
};
await docs.AddAsync(doc);
await context.SaveChangesAsync();

// ATP soubor bude obsahovat nativní binární přílohu (ne base64!)
```

### Scénář 3: Konverze JSON s base64 vlajkami (použití countries2.json)

```csharp
// Načtení z countries2.json (textová data s base64 binárními vlajkami)
using var context = new AjisContext();
var countries = context.Set<Country>("countries2.json");

// Přečíst všechny země
var allCountries = await countries.ReadAllAsync();

// Převést na ATP soubor (automatická detekce binárních příloh)
// Pokud země obsahují binární data (vlajky), použije se ATP formát
var country = allCountries.FirstOrDefault();
if (country?.FlagData != null)
{
    var attachment = BinaryAttachment.FromBytes(country.FlagData, "flag.png", "image/png");
    // ... další práce s přílohou
}

// Uložit jako ATP
context.Set<Country>("countries2.atp", AjisFormat.Atp);
```

### Scénář 4: Vytvoření API endpointu pro upload souborů

```csharp
// ASP.NET Core controller
[HttpPost]
public async Task<IActionResult> UploadDocument(IFormFile file)
{
    using var context = new AjisContext();
    var docs = context.Set<Document>("documents.atp");

    // Vytvořit dokument s přílohou z IFormFile
    var attachment = BinaryAttachment.FromIFormFile(file);
    var doc = new Document
    {
        Id = Guid.NewGuid(),
        Name = file.FileName,
        File = attachment
    };

    await docs.AddAsync(doc);
    await context.SaveChangesAsync();

    return Ok(new { id = doc.Id });
}
```

## Výhody AJIS Context

### Oproti ručnímu čtení/zápisu AJIS souborů:

- **Typová bezpečnost**: kompilátor kontroluje vlastnosti entit
- **CRUD operace**: jednoduché metody pro Add/Update/Remove
- **Automatická serializace**: není potřeba volat serializeDeserialize manuálně
- **Zamykání souboru**: vylučuje konflikty při zápisech
- **Asynchronní API**: plná podpora async/await pro škálovatelnost

### Oproti EF Core:

- **Souborová úložiště**: nevyžaduje databázi
- **AJIS/ATP podpora**: nativní podpora pro textová a binární data
- **Jednoduchost**: není potřeba migrace, connection strings, atd.
- **Přenositelnost**: AJIS/ATP soubory lze snadno sdílet a editovat

### Oproti ruční práci s ATP:

- **Unifikované API**: stejné rozhraní jako pro AJIS
- **Automatická detekce formátu**: není potřeba ručně rozhodovat mezi AJIS/ATP
- **Konverze**: jednoduchá konverze mezi formáty
- **Práce s binárními přílohami**: pohodlné metody pro upload/download

## Budoucí vylepšení

- **LINQProvider**: plná LINQ podpora pro dotazy jako `from u in users where u.Age > 18 select u`
- **Indexy**: indexy pro rychlejší vyhledávání podle vlastností
- **Change tracking**: sledování změn entit pro batch operace
- **Validation**: validace entit před zápisem do souboru
- **Compression**: automatická komprese velkých souborů
- **Encryption**: šifrování citlivých dat (budoucnost)

## Shrnutí

AJIS Context poskytuje jednoduché, intuitive API pro práci s AJIS a ATP soubory, inspirované Entity Framework Core. Díky automatické detekci formátu a podpoře binárních příloh je to ideální nástroj pro:

- Čtení a zápis konfiguračních souborů (AJIS)
- Ukládání dokumentů s přílohami (ATP)
- Konverze mezi formáty (JSON → AJIS/ATP)
- API endpoints pro upload/download souborů
- Desktop aplikace s lokálním úložištěm

Je to "známý" přístup pro vývojáře (připomíná EF Core), ale optimalizovaný pro AJIS/ATP formáty.
