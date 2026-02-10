#nullable enable

using System.Diagnostics;
using Afrowave.AJIS.IO;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Country name with nested fields.
/// </summary>
public class CountryName
{
    public string Common { get; set; } = "";
    public string Official { get; set; } = "";
}

/// <summary>
/// Country data model for benchmarking.
/// </summary>
public class Country
{
    public CountryName Name { get; set; } = new();
    public string Capital { get; set; } = "";
    public string Region { get; set; } = "";
    public string Subregion { get; set; } = "";
    public long Population { get; set; }
    public double Area { get; set; }
    public string Languages { get; set; } = ""; // Simplified from List<string>
    public string Currencies { get; set; } = ""; // Simplified from List<string>
    public string Flag { get; set; } = "";
    // Removed Translations dictionary for compatibility
}

/// <summary>
/// Countries benchmark - demonstrates real-world data access patterns.
/// </summary>
public static class CountriesBenchmark
{
    private const string CountriesFile = "countries.json";
    private static readonly Random _random = new(42);

    public static async Task RunAsync()
    {
        Console.WriteLine("🌍 COUNTRIES BENCHMARK - Real-World Data Access");
        Console.WriteLine("===============================================");

        // Generate sample countries data
        var countries = GenerateCountries(195); // All countries in the world
        Console.WriteLine($"📊 Generated {countries.Count} countries");

        // Save to file
        Console.WriteLine("💾 Saving countries to file...");
        var saveTimer = Stopwatch.StartNew();
        AjisFile.Create(CountriesFile, countries);
        saveTimer.Stop();
        Console.WriteLine($"   ✅ Saved in {saveTimer.Elapsed.TotalSeconds:F2}s");

        // Load and display random country
        Console.WriteLine("\n🎲 RANDOM COUNTRY LOOKUP DEMO");
        Console.WriteLine("================================");

        for (int i = 0; i < 5; i++)
        {
            var randomCountry = countries[_random.Next(countries.Count)];
            Console.WriteLine($"\n🔍 Looking up: {randomCountry.Name.Official}");

            // Method 1: Simple enumeration (demonstrating nested field access)
            var enumTimer = Stopwatch.StartNew();
            var foundByEnum = AjisFile.Enumerate<Country>(CountriesFile)
                .FirstOrDefault(c => c.Name.Official == randomCountry.Name.Official);
            enumTimer.Stop();

            // Method 2: Indexed lookup (using nested field path)
            var indexTimer = Stopwatch.StartNew();
            var foundByIndex = AjisFile.FindByKey<Country>(CountriesFile, "Name.Official", randomCountry.Name.Official);
            indexTimer.Stop();

            // Method 3: Linq query (demonstrating nested field querying)
            var linqTimer = Stopwatch.StartNew();
            var foundByLinq = (from c in AjisQuery.FromFile<Country>(CountriesFile, "Name.Official")
                              where c.Name.Official == randomCountry.Name.Official
                              select c).FirstOrDefault();
            linqTimer.Stop();

            // Display results
            if (foundByIndex != null)
            {
                Console.WriteLine($"   🏛️  Capital: {foundByIndex.Capital}");
                Console.WriteLine($"   🌍 Region: {foundByIndex.Region}");
                Console.WriteLine($"   👥 Population: {foundByIndex.Population:N0}");
                Console.WriteLine($"   📏 Area: {foundByIndex.Area:N0} km²");
                Console.WriteLine($"   💰 Currencies: {foundByIndex.Currencies}");
                Console.WriteLine($"   🗣️  Languages: {foundByIndex.Languages}");
            }

            Console.WriteLine("   ⏱️  Lookup times:");
            Console.WriteLine($"      Enumeration: {enumTimer.Elapsed.TotalMilliseconds:F1}ms");
            Console.WriteLine($"      Indexed:      {indexTimer.Elapsed.TotalMilliseconds:F1}ms");
            Console.WriteLine($"      Linq:         {linqTimer.Elapsed.TotalMilliseconds:F1}ms");
        }

        // Performance comparison
        Console.WriteLine("\n📈 PERFORMANCE ANALYSIS");
        Console.WriteLine("========================");

        const int iterations = 100;
        var testCountries = countries.Take(10).ToList();

        // Enumeration performance
        var enumTimes = new List<double>();
        foreach (var country in testCountries)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = AjisFile.Enumerate<Country>(CountriesFile)
                    .FirstOrDefault(c => c.Name.Official == country.Name.Official);
            }
            timer.Stop();
            enumTimes.Add(timer.Elapsed.TotalMilliseconds / iterations);
        }

        // Indexed performance
        var indexTimes = new List<double>();
        foreach (var country in testCountries)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var result = AjisFile.FindByKey<Country>(CountriesFile, "Name.Official", country.Name.Official);
            }
            timer.Stop();
            indexTimes.Add(timer.Elapsed.TotalMilliseconds / iterations);
        }

        Console.WriteLine($"Average enumeration time: {enumTimes.Average():F2}ms");
        Console.WriteLine($"Average indexed lookup:   {indexTimes.Average():F2}ms");
        Console.WriteLine($"Speed improvement:        {enumTimes.Average() / indexTimes.Average():F1}x faster");

        // Cleanup
        if (File.Exists(CountriesFile))
            File.Delete(CountriesFile);

        Console.WriteLine("\n✅ Countries benchmark completed!");
    }

    internal static List<Country> GenerateCountries(int count)
    {
        var countries = new List<Country>();
        var regions = new[] { "Africa", "Americas", "Asia", "Europe", "Oceania" };
        var subregions = new Dictionary<string, string[]>
        {
            ["Africa"] = new[] { "Eastern Africa", "Middle Africa", "Northern Africa", "Southern Africa", "Western Africa" },
            ["Americas"] = new[] { "Caribbean", "Central America", "North America", "South America" },
            ["Asia"] = new[] { "Central Asia", "Eastern Asia", "South-Eastern Asia", "Southern Asia", "Western Asia" },
            ["Europe"] = new[] { "Eastern Europe", "Northern Europe", "Southern Europe", "Western Europe" },
            ["Oceania"] = new[] { "Australia and New Zealand", "Melanesia", "Micronesia", "Polynesia" }
        };

        var languages = new[] { "English", "Spanish", "French", "Arabic", "Chinese", "Hindi", "Portuguese", "Russian", "German", "Japanese" };
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CNY", "INR", "BRL", "RUB", "CAD", "AUD" };
        
        // Real country names for demonstration (reused cyclically for all countries)
        var countryNames = new[]
        {
            ("USA", "United States of America"),
            ("UK", "United Kingdom of Great Britain and Northern Ireland"),
            ("France", "French Republic"),
            ("Germany", "Federal Republic of Germany"),
            ("Japan", "Japan"),
            ("China", "People's Republic of China"),
            ("India", "Republic of India"),
            ("Brazil", "Federative Republic of Brazil"),
            ("Canada", "Canada"),
            ("Australia", "Commonwealth of Australia"),
            ("Czechia", "Czech Republic"),
            ("Slovakia", "Slovak Republic"),
            ("Poland", "Republic of Poland"),
            ("Austria", "Republic of Austria"),
            ("Switzerland", "Swiss Confederation"),
            ("Italy", "Italian Republic"),
            ("Spain", "Kingdom of Spain"),
            ("Portugal", "Portuguese Republic"),
            ("Netherlands", "Kingdom of the Netherlands"),
            ("Belgium", "Kingdom of Belgium"),
            ("Denmark", "Kingdom of Denmark"),
            ("Sweden", "Kingdom of Sweden"),
            ("Norway", "Kingdom of Norway"),
            ("Finland", "Republic of Finland"),
            ("Ireland", "Republic of Ireland"),
            ("Greece", "Hellenic Republic"),
            ("Croatia", "Republic of Croatia"),
            ("Slovenia", "Republic of Slovenia"),
            ("Hungary", "Hungary"),
            ("Romania", "Romania"),
            ("Bulgaria", "Republic of Bulgaria"),
            ("Serbia", "Republic of Serbia"),
            ("Ukraine", "Ukraine"),
            ("Russia", "Russian Federation"),
            ("Turkey", "Republic of Turkey"),
            ("Egypt", "Arab Republic of Egypt"),
            ("South Africa", "Republic of South Africa"),
            ("Nigeria", "Federal Republic of Nigeria"),
            ("Kenya", "Republic of Kenya"),
            ("Morocco", "Kingdom of Morocco"),
            ("Argentina", "Argentine Republic"),
            ("Chile", "Republic of Chile"),
            ("Peru", "Republic of Peru"),
            ("Colombia", "Republic of Colombia"),
            ("Venezuela", "Bolivarian Republic of Venezuela"),
            ("Mexico", "United Mexican States"),
            ("Cuba", "Republic of Cuba"),
            ("Jamaica", "Jamaica"),
            ("South Korea", "Republic of Korea"),
            ("North Korea", "Democratic People's Republic of Korea"),
            ("Thailand", "Kingdom of Thailand"),
            ("Vietnam", "Socialist Republic of Vietnam"),
            ("Indonesia", "Republic of Indonesia"),
            ("Malaysia", "Malaysia"),
            ("Singapore", "Republic of Singapore"),
            ("Philippines", "Republic of the Philippines"),
            ("Pakistan", "Islamic Republic of Pakistan"),
            ("Bangladesh", "People's Republic of Bangladesh"),
            ("Afghanistan", "Islamic Emirate of Afghanistan"),
            ("Iran", "Islamic Republic of Iran"),
            ("Iraq", "Republic of Iraq"),
            ("Israel", "State of Israel"),
            ("Saudi Arabia", "Kingdom of Saudi Arabia"),
            ("UAE", "United Arab Emirates"),
            ("New Zealand", "New Zealand"),
            ("Papua New Guinea", "Independent State of Papua New Guinea"),
            ("Fiji", "Republic of Fiji")
        };

        // Capital cities corresponding to country names
        var capitals = new[]
        {
            "Washington, D.C.",
            "London",
            "Paris",
            "Berlin",
            "Tokyo",
            "Beijing",
            "New Delhi",
            "Brasília",
            "Ottawa",
            "Canberra",
            "Prague",
            "Bratislava",
            "Warsaw",
            "Vienna",
            "Bern",
            "Rome",
            "Madrid",
            "Lisbon",
            "Amsterdam",
            "Brussels",
            "Copenhagen",
            "Stockholm",
            "Oslo",
            "Helsinki",
            "Dublin",
            "Athens",
            "Zagreb",
            "Ljubljana",
            "Budapest",
            "Bucharest",
            "Sofia",
            "Belgrade",
            "Kyiv",
            "Moscow",
            "Ankara",
            "Cairo",
            "Pretoria",
            "Abuja",
            "Nairobi",
            "Rabat",
            "Buenos Aires",
            "Santiago",
            "Lima",
            "Bogotá",
            "Caracas",
            "Mexico City",
            "Havana",
            "Kingston",
            "Seoul",
            "Pyongyang",
            "Bangkok",
            "Hanoi",
            "Jakarta",
            "Kuala Lumpur",
            "Singapore",
            "Manila",
            "Islamabad",
            "Dhaka",
            "Kabul",
            "Tehran",
            "Baghdad",
            "Jerusalem",
            "Riyadh",
            "Abu Dhabi",
            "Wellington",
            "Port Moresby",
            "Suva"
        };

        for (int i = 0; i < count; i++)
        {
            var region = regions[_random.Next(regions.Length)];
            var nameIndex = i % countryNames.Length;
            var (common, official) = countryNames[nameIndex];
            
            var country = new Country
            {
                Name = new CountryName
                {
                    Common = common,
                    Official = official,
                },
                Capital = capitals[nameIndex],
                Region = region,
                Subregion = subregions[region][_random.Next(subregions[region].Length)],
                Population = _random.Next(100000, 1000000000),
                Area = _random.Next(1000, 10000000),
                Languages = string.Join(", ", languages.OrderBy(_ => _random.Next()).Take(_random.Next(1, 4))),
                Currencies = string.Join(", ", currencies.OrderBy(_ => _random.Next()).Take(_random.Next(1, 3))),
                Flag = $"🇺🇳", // Placeholder
            };

            countries.Add(country);
        }

        return countries;
    }
}