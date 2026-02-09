#nullable enable

namespace Afrowave.AJIS.Benchmarks.StressTest;

/// <summary>
/// Generates large, complex datasets for stress testing.
/// Creates realistic User objects with nested Address objects.
/// </summary>
public sealed class ComplexDataGenerator
{
    private readonly Random _random = new Random(42); // Seed for reproducibility

    /// <summary>
    /// Generates a list of User objects with nested Address.
    /// </summary>
    public List<StressTestUser> GenerateUsers(int count)
    {
        var users = new List<StressTestUser>(count);

        for (int i = 0; i < count; i++)
        {
            users.Add(GenerateUser(i));
        }

        return users;
    }

    /// <summary>
    /// Generates an async enumerable of User objects (memory-bounded).
    /// </summary>
    public async IAsyncEnumerable<StressTestUser> GenerateUsersAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return GenerateUser(i);
            
            // Simulate async work
            if (i % 1000 == 0)
                await Task.Delay(0);
        }
    }

    private StressTestUser GenerateUser(int id)
    {
        return new StressTestUser
        {
            Id = id,
            Name = GenerateName(),
            Email = GenerateEmail(id),
            Active = _random.Next(2) == 0,
            Score = _random.Next(0, 100),
            CreatedDate = DateTime.Now.AddDays(-_random.Next(365)),
            Tags = GenerateTags(),
            Address = GenerateAddress(),
            Metadata = GenerateMetadata()
        };
    }

    private string GenerateName()
    {
        var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Iris", "Jack" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        
        return $"{firstNames[_random.Next(firstNames.Length)]} {lastNames[_random.Next(lastNames.Length)]}";
    }

    private string GenerateEmail(int id)
    {
        var domains = new[] { "example.com", "test.com", "demo.com", "sample.org", "data.io" };
        return $"user{id}@{domains[_random.Next(domains.Length)]}";
    }

    private string[] GenerateTags()
    {
        var allTags = new[] 
        { 
            "developer", "senior", "junior", "manager", "architect",
            "frontend", "backend", "fullstack", "devops", "qa",
            "team-lead", "engineer", "specialist", "intern", "contractor"
        };
        
        var count = _random.Next(1, 5);
        var tags = new string[count];
        for (int i = 0; i < count; i++)
        {
            tags[i] = allTags[_random.Next(allTags.Length)];
        }
        return tags;
    }

    private StressTestAddress GenerateAddress()
    {
        var streets = new[] { "Main St", "Oak Ave", "Elm St", "Maple Dr", "Pine Rd" };
        var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix" };
        var states = new[] { "NY", "CA", "IL", "TX", "AZ" };
        var countries = new[] { "USA", "Canada", "UK", "Germany", "France" };

        return new StressTestAddress
        {
            Street = $"{_random.Next(1, 9999)} {streets[_random.Next(streets.Length)]}",
            City = cities[_random.Next(cities.Length)],
            State = states[_random.Next(states.Length)],
            ZipCode = $"{_random.Next(10000, 99999)}",
            Country = countries[_random.Next(countries.Length)]
        };
    }

    private Dictionary<string, object> GenerateMetadata()
    {
        return new Dictionary<string, object>
        {
            { "department", new[] { "Engineering", "Sales", "Marketing", "HR", "Finance" }[_random.Next(5)] },
            { "level", new[] { "L1", "L2", "L3", "L4", "L5" }[_random.Next(5)] },
            { "yearsExperience", _random.Next(0, 30) },
            { "isManager", _random.Next(2) == 0 },
            { "salary", _random.Next(40000, 200000) }
        };
    }

    /// <summary>
    /// Saves users to an AJIS file.
    /// </summary>
    public void SaveAsAjis(List<StressTestUser> users, string filePath)
    {
        Console.WriteLine($"Saving {users.Count:N0} users to AJIS file...");
        
        using (var writer = new System.IO.StreamWriter(new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write), System.Text.Encoding.UTF8, 4096))
        {
            writer.Write("[");
            for (int i = 0; i < users.Count; i++)
            {
                if (i > 0) writer.Write(",");
                
                // Simple JSON serialization
                var json = System.Text.Json.JsonSerializer.Serialize(users[i]);
                writer.Write(json);

                if (i % 10000 == 0 && i > 0)
                    Console.WriteLine($"  Saved {i:N0} users...");
            }
            writer.Write("]");
        }

        var fileInfo = new System.IO.FileInfo(filePath);
        Console.WriteLine($"✓ Saved {users.Count:N0} users ({fileInfo.Length / (1024 * 1024)}MB)");
    }

    /// <summary>
    /// Gets the file size in MB.
    /// </summary>
    public static double GetFileSizeMB(string filePath)
    {
        var info = new System.IO.FileInfo(filePath);
        return info.Length / (1024.0 * 1024.0);
    }
}

/// <summary>
/// Represents a user for stress testing (with nested address).
/// </summary>
public class StressTestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Active { get; set; }
    public int Score { get; set; }
    public DateTime CreatedDate { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public StressTestAddress? Address { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an address (nested in User).
/// </summary>
public class StressTestAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}
