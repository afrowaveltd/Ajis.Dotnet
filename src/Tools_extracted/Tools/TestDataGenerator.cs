using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Afrowave.AJIS.Tools;

/// <summary>
/// Generates realistic test data for benchmarking.
/// </summary>
public class TestDataGenerator
{
    private static readonly Random _random = new Random(42); // Fixed seed for reproducibility

    private static readonly string[] _firstNames =
    {
        "Jan", "Petr", "Pavel", "Tomáš", "Jiří", "Martin", "Lukáš", "Michal", "Jakub", "Ondřej",
        "John", "Robert", "Michael", "William", "David", "James", "Richard", "Joseph", "Thomas", "Charles",
        "Anna", "Marie", "Eva", "Hana", "Kateřina", "Petra", "Lucie", "Věra", "Alena", "Jana",
        "Mary", "Patricia", "Jennifer", "Linda", "Barbara", "Elizabeth", "Susan", "Jessica", "Sarah", "Karen"
    };

    private static readonly string[] _lastNames =
    {
        "Novák", "Svoboda", "Novotný", "Dvořák", "Černý", "Procházka", "Kučera", "Veselý", "Horák", "Němec",
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez"
    };

    private static readonly string[] _streets =
    {
        "Main Street", "Oak Avenue", "Maple Drive", "Cedar Lane", "Elm Street",
        "Hlavní", "Nádražní", "Školní", "Zahradní", "Polní", "Lesní", "Břežná"
    };

    private static readonly string[] _cities =
    {
        "Praha", "Brno", "Ostrava", "Plzeň", "Liberec", "Olomouc", "Hradec Králové", "České Budějovice",
        "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego"
    };

    private static readonly string[] _countries =
    {
        "Czech Republic", "United States", "Germany", "France", "United Kingdom", "Spain", "Italy", "Poland"
    };

    private static readonly string[] _companies =
    {
        "TechCorp", "DataSystems", "CloudServices", "InnovateLabs", "DigitalWorks",
        "SmartSolutions", "FutureTech", "AlphaSoft", "BetaCode", "GammaData"
    };

    private static readonly string[] _departments =
    {
        "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "Support", "Research"
    };

    /// <summary>
    /// Generates a user with addresses, contacts, and metadata.
    /// </summary>
    public static AjisValue GenerateUser(int id)
    {
        var addressCount = _random.Next(1, 6); // 1-5 addresses
        var addresses = new List<AjisValue>();

        for (int i = 0; i < addressCount; i++)
        {
            addresses.Add(AjisValue.Object(new Dictionary<string, AjisValue>
            {
                ["type"] = AjisValue.String(i == 0 ? "home" : (i == 1 ? "work" : "other")),
                ["street"] = AjisValue.String($"{_random.Next(1, 9999)} {_streets[_random.Next(_streets.Length)]}"),
                ["city"] = AjisValue.String(_cities[_random.Next(_cities.Length)]),
                ["postalCode"] = AjisValue.String($"{_random.Next(10000, 99999)}"),
                ["country"] = AjisValue.String(_countries[_random.Next(_countries.Length)]),
                ["isPrimary"] = AjisValue.Boolean(i == 0)
            }));
        }

        var phoneCount = _random.Next(1, 4); // 1-3 phones
        var phones = new List<AjisValue>();
        for (int i = 0; i < phoneCount; i++)
        {
            phones.Add(AjisValue.Object(new Dictionary<string, AjisValue>
            {
                ["type"] = AjisValue.String(i == 0 ? "mobile" : (i == 1 ? "home" : "work")),
                ["number"] = AjisValue.String($"+{_random.Next(1, 999)}-{_random.Next(100, 999)}-{_random.Next(1000000, 9999999)}")
            }));
        }

        var emailCount = _random.Next(1, 3); // 1-2 emails
        var emails = new List<AjisValue>();
        for (int i = 0; i < emailCount; i++)
        {
            var firstName = _firstNames[_random.Next(_firstNames.Length)].ToLower();
            var lastName = _lastNames[_random.Next(_lastNames.Length)].ToLower();
            var domain = i == 0 ? "gmail.com" : "company.com";
            emails.Add(AjisValue.String($"{firstName}.{lastName}{_random.Next(1, 999)}@{domain}"));
        }

        var tagCount = _random.Next(2, 8); // 2-7 tags
        var tags = new List<AjisValue>();
        var allTags = new[] { "premium", "vip", "active", "verified", "new", "returning", "enterprise", "partner", "influencer", "beta-tester" };
        for (int i = 0; i < tagCount; i++)
        {
            tags.Add(AjisValue.String(allTags[_random.Next(allTags.Length)]));
        }

        return AjisValue.Object(new Dictionary<string, AjisValue>
        {
            ["id"] = AjisValue.Number(id),
            ["firstName"] = AjisValue.String(_firstNames[_random.Next(_firstNames.Length)]),
            ["lastName"] = AjisValue.String(_lastNames[_random.Next(_lastNames.Length)]),
            ["age"] = AjisValue.Number(_random.Next(18, 80)),
            ["email"] = AjisValue.String(emails.First().AsString()),
            ["isActive"] = AjisValue.Boolean(_random.Next(0, 100) > 10), // 90% active
            ["score"] = AjisValue.Number(_random.Next(0, 1000) + _random.NextDouble()),
            ["registeredAt"] = AjisValue.String($"2024-{_random.Next(1, 13):D2}-{_random.Next(1, 29):D2}T{_random.Next(0, 24):D2}:{_random.Next(0, 60):D2}:{_random.Next(0, 60):D2}Z"),
            ["addresses"] = AjisValue.Array(addresses),
            ["phones"] = AjisValue.Array(phones),
            ["emails"] = AjisValue.Array(emails),
            ["tags"] = AjisValue.Array(tags),
            ["metadata"] = AjisValue.Object(new Dictionary<string, AjisValue>
            {
                ["company"] = AjisValue.String(_companies[_random.Next(_companies.Length)]),
                ["department"] = AjisValue.String(_departments[_random.Next(_departments.Length)]),
                ["employeeId"] = AjisValue.String($"EMP{id:D6}"),
                ["salary"] = AjisValue.Number(_random.Next(30000, 150000)),
                ["lastLogin"] = AjisValue.String($"2026-01-{_random.Next(1, 23):D2}T{_random.Next(0, 24):D2}:{_random.Next(0, 60):D2}:{_random.Next(0, 60):D2}Z"),
                ["loginCount"] = AjisValue.Number(_random.Next(1, 10000))
            })
        });
    }

    /// <summary>
    /// Generates an array of users.
    /// </summary>
    public static AjisValue GenerateUsers(int count)
    {
        var users = new List<AjisValue>();
        for (int i = 0; i < count; i++)
        {
            users.Add(GenerateUser(i + 1));
        }
        return AjisValue.Array(users);
    }

    /// <summary>
    /// Generates test data and saves to file.
    /// </summary>
    public static void GenerateTestFile(string filePath, int userCount)
    {
        Console.WriteLine($"Generating {userCount} users (streaming to file to avoid high memory)...");

        var options = new Afrowave.AJIS.Legacy.AjisSerializerOptions
        {
            WriteIndented = false,
            UseAjisExtensions = false // Standard JSON for benchmark compatibility
        };

        // Stream users one-by-one into the output AJIS file to avoid building the entire array in memory.
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.SequentialScan))
        {
            // Write opening array bracket
            var open = System.Text.Encoding.UTF8.GetBytes("[");
            fs.Write(open, 0, open.Length);

            for (int i = 0; i < userCount; i++)
            {
                var user = GenerateUser(i + 1);

                // Serialize single user to pooled buffer writer (zero-copy path)
                var bufferWriter = Afrowave.AJIS.Legacy.AjisUtf8Serializer.SerializeToBufferWriter(user, options);

                if (i > 0)
                {
                    // write comma separator
                    fs.WriteByte((byte)',');
                }

                // write the serialized bytes directly
                fs.Write(bufferWriter.WrittenSpan);

                // return pooled writer
                Afrowave.AJIS.Legacy.AjisUtf8Serializer.ReturnBufferWriter(bufferWriter);

                // occasionally flush to keep pressure low
                if ((i & 0x3FF) == 0)
                {
                    fs.Flush();
                }
            }

            // Write closing array bracket
            var close = System.Text.Encoding.UTF8.GetBytes("]");
            fs.Write(close, 0, close.Length);
            fs.Flush();
        }

        var fileInfo = new FileInfo(filePath);
        Console.WriteLine($"Generated {fileInfo.Length:N0} bytes ({fileInfo.Length / 1024.0 / 1024.0:F2} MB)");
    }
}
