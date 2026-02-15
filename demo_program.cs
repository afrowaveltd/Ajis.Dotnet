#nullable enable

using Afrowave.AJIS.IO;
using Afrowave.AJIS.Core;

// ===== Ukázka použití AjisContext - univerzální AJIS nástroj jako EF Core =====

// Definice entit
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

public class Document
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public BinaryAttachment File { get; set; } = new();
}

// ===== PŘÍKLAD 1: Základní práce s textovými daty (AJIS) =====
async Task Example1_AddUsers()
{
    Console.WriteLine("=== Příklad 1: Základní práce s textovými daty ===");
    
    using var context = new AjisContext();
    var users = context.Set<User>("users.ajis");

    // Přidání uživatelů
    await users.AddAsync(new User { Id = 1, Name = "Alice", Age = 25, IsActive = true });
    await users.AddAsync(new User { Id = 2, Name = "Bob", Age = 30, IsActive = false });
    await users.AddAsync(new User { Id = 3, Name = "Charlie", Age = 35, IsActive = true });
    
    // Uložení změn
    await context.SaveChangesAsync();
    
    // Načtení uživatele podle klíče
    var user = await users.FindAsync(1);
    Console.WriteLine($"Nalezen uživatel: {user?.Name}");
    
    // Aktualizace
    user!.Age = 26;
    await users.UpdateAsync(user);
    await context.SaveChangesAsync();
    
    // Počet dospělých
    var adultCount = await users.CountAsync(u => u.Age >= 18);
    Console.WriteLine($"Počet dospělých: {adultCount}");
    
    // Odstranění
    await users.RemoveWhereAsync(u => u.IsActive == false);
    await context.SaveChangesAsync();
    
    Console.WriteLine($"Počet zbývajících uživatelů: {await users.CountAsync()}");
}

// ===== PŘÍKLAD 2: Práce s binárními přílohami (ATP) =====
async Task Example2_BinaryAttachments()
{
    Console.WriteLine("\n=== Příklad 2: Práce s binárními přílohami (ATP) ===");
    
    using var context = new AjisContext();
    var docs = context.Set<Document>("documents.atp");

    // Vytvoření binární přílohy z byte[]
    var fileData = System.Text.Encoding.UTF8.GetBytes("Toto je obsah dokumentu");
    var doc = new Document
    {
        Id = Guid.NewGuid(),
        Name = "dokument.txt",
        File = BinaryAttachment.FromBytes(fileData, "dokument.txt", "text/plain")
    };

    // Přidání dokumentu
    await docs.AddAsync(doc);
    await context.SaveChangesAsync();

    // Načtení dokumentu
    var loadedDoc = await docs.FindByKeyAsync(doc.Id);
    if (loadedDoc != null)
    {
        Console.WriteLine($"Načten dokument: {loadedDoc.Name}");
        Console.WriteLine($"Velikost přílohy: {loadedDoc.File.Size} bajtů");
        
        // Uložení přílohy na disk
        await loadedDoc.File.SaveToFileAsync("saved_document.txt");
        Console.WriteLine("Příloha uložena na disk");
    }
}

// ===== PŘÍKLAD 3: Automatická detekce formátu =====
async Task Example3_AutoDetect()
{
    Console.WriteLine("\n=== Příklad 3: Automatická detekce formátu ===");
    
    using var context = new AjisContext();

    // Soubor .ajis → automaticky použije AJIS formát
    var users = context.Set<User>("users.ajis");
    Console.WriteLine($"Formát users.ajis: {users.Format}");

    // Soubor .atp → automaticky použije ATP formát
    var docs = context.Set<Document>("documents.atp");
    Console.WriteLine($"Formát documents.atp: {docs.Format}");
}

// ===== PŘÍKLAD 4: Vývojářský override formátu =====
async Task Example4_FormatOverride()
{
    Console.WriteLine("\n=== Příklad 4: Vývojářský override formátu ===");
    
    using var context = new AjisContext();
    
    // Přestože koncovka je .ajis, vynutíme ATP formát
    var users = context.Set<User>("users.ajis", AjisFormat.Atp);
    Console.WriteLine($"Vynucený formát ATP: {users.Format}");
}

// ===== PŘÍKLAD 5: Explicitní konfigurace s binárními přílohami =====
async Task Example5_ConfigurationWithAttachments()
{
    Console.WriteLine("\n=== Příklad 5: Konfigurace s binárními přílohami ===");
    
    using var context = new AjisContext();
    
    // Vytvoření konfigurace s binární přílohou
    var config = new AjisEntityConfiguration<Document>();
    config.Key(d => d.Id);
    config.BinaryAttachment(d => d.File);
    
    // Automatická detekce ATP formátu kvůli binární příloze
    var docs = context.Set<Document>("docs.ajis", config);
    Console.WriteLine($"Formát s binární přílohou: {docs.Format}");
}

// ===== PŘÍKLAD 6: Vícenásobné zdroje dat =====
async Task Example6_MultipleDataSources()
{
    Console.WriteLine("\n=== Příklad 6: Vícenásobné zdroje dat ===");
    
    using var context = new AjisContext();
    
    // Různé soubory, každý má vlastní data
    var users1 = context.Set<User>("users1.ajis");
    var users2 = context.Set<User>("users2.ajis");

    // Přidání jedinečných dat do každého souboru
    await users1.AddAsync(new User { Id = 1, Name = "User1" });
    await users2.AddAsync(new User { Id = 2, Name = "User2" });
    
    await context.SaveChangesAsync();

    // Každý soubor obsahuje jen svá data
    Console.WriteLine($"V users1.ajis: {await users1.CountAsync()} uživatelů");
    Console.WriteLine($"V users2.ajis: {await users2.CountAsync()} uživatelů");
}

// ===== PŘÍKLAD 7: Použití s existujícími soubory =====
async Task Example7_ReadExistingFile()
{
    Console.WriteLine("\n=== Příklad 7: Čtení existujícího souboru ===");
    
    using var context = new AjisContext();
    var users = context.Set<User>("existing_users.ajis");
    
    // Načtení všech uživatelů
    await foreach (var user in users)
    {
        Console.WriteLine($"Uživatel: {user.Name} (ID: {user.Id})");
    }
}

// ===== SPUS'TENÍ VŠECH PŘÍKLADŮ =====
Console.WriteLine("========================================");
Console.WriteLine("     AJIS CONTEXT UKÁZKY - UNIVERZÁLNÍ NÁSTROJ");
Console.WriteLine("     Podobný EF Core pro práci s AJIS/ATP soubory");
Console.WriteLine("========================================\n");

await Example1_AddUsers();
await Example2_BinaryAttachments();
await Example3_AutoDetect();
await Example4_FormatOverride();
await Example5_ConfigurationWithAttachments();
await Example6_MultipleDataSources();

Console.WriteLine("\n========================================");
Console.WriteLine("           VŠECHNY PŘÍKLADY DOKONČENY");
Console.WriteLine("========================================");
