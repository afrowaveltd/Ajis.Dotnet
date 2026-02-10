#nullable enable

using Afrowave.AJIS.Benchmarks.StressTest;
using Afrowave.AJIS.Benchmarks.Baseline;
using Afrowave.AJIS.Benchmarks.Legacy;
using Afrowave.AJIS.Benchmarks.Conversion;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Main entry point for AJIS benchmarking suite.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0].ToLower() == "baseline")
        {
            RunBaselineBenchmark();
        }
        else if (args[0].ToLower() == "stress")
        {
            RunStressTesting();
        }
        else if (args[0].ToLower() == "legacy")
        {
            RunLegacyMigration();
        }
        else if (args[0].ToLower() == "images")
        {
            RunImageReconstruction();
        }
        else if (args[0].ToLower() == "convert")
        {
            RunJsonToAtpConversion();
        }
        else if (args[0].ToLower() == "perf")
        {
            SimplePerfTest.Run();
        }
        else if (args[0].ToLower() == "roundtrip")
        {
            RoundTripStressTest.Run();
        }
        else if (args[0].ToLower() == "parsers")
        {
            RunParserComparison();
        }
        else if (args[0].ToLower() == "best")
        {
            RunBestOfBreed();
        }
        else if (args[0].ToLower() == "both")
        {
            RunBaselineBenchmark();
            Console.WriteLine("\n\n");
            RunStressTesting();
        }
        else if (args[0].ToLower() == "all")
        {
            await RunInteractiveDemo();
        }
        else if (args[0].ToLower() == "countries")
        {
            await CountriesBenchmark.RunAsync();
        }
        else
        {
            PrintUsage();
        }
    }

    private static void RunBaselineBenchmark()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("            BASELINE BENCHMARK");
        Console.WriteLine("════════════════════════════════════════════════════════");
        BaselineProgram.RunBaseline(Array.Empty<string>());
    }

    private static void RunStressTesting()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("            STRESS TESTING");
        Console.WriteLine("════════════════════════════════════════════════════════");
        StressTestProgram.RunStressTest(Array.Empty<string>());
    }

    private static void RunLegacyMigration()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("            LEGACY JSON → AJIS MIGRATION");
        Console.WriteLine("════════════════════════════════════════════════════════");
        LegacyMigrationProgram.RunMigration(Array.Empty<string>());
    }

    private static void RunImageReconstruction()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("        IMAGE RECONSTRUCTION FROM LEGACY JSON");
        Console.WriteLine("════════════════════════════════════════════════════════");
        
        // Resolve path correctly - relative to solution root, not bin directory
        var solutionRoot = FindSolutionRoot();
        var jsonFile = Path.Combine(solutionRoot, "test_data_legacy", "countries4.json");
        
        if (!File.Exists(jsonFile))
        {
            Console.WriteLine($"❌ File not found: {jsonFile}");
            Console.WriteLine($"   Current directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"   Solution root: {solutionRoot}");
            return;
        }

        ImageReconstructionProgram.RunImageReconstruction(jsonFile);
    }

    private static void RunJsonToAtpConversion()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("          JSON → AJIS → .ATP CONVERSION");
        Console.WriteLine("════════════════════════════════════════════════════════");
        JsonToAtpConversionProgram.RunJsonToAtp(Array.Empty<string>());
    }

    private static void RunPerformanceTests()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("         PERFORMANCE TEST SUITE (ISOLATED)");
        Console.WriteLine("════════════════════════════════════════════════════════");
        var runner = new PerformanceTestRunner();
        runner.Run();
    }

    private static void RunParserComparison()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("         PARSER COMPETITION (OLD VS NEW)");
        Console.WriteLine("════════════════════════════════════════════════════════");
        var benchmark = new ParserCompetitionBenchmark();
        benchmark.Run();
    }

    private static void RunBestOfBreed()
    {
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("         BEST-OF-BREED SELECTION");
        Console.WriteLine("════════════════════════════════════════════════════════");
        var benchmark = new BestOfBreedBenchmark();
        benchmark.Run();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
AJIS.Dotnet Benchmarking Suite

Usage:
  dotnen run                 - Run baseline benchmark (default)
  dotnet run baseline        - Run baseline benchmark
  dotnet run stress          - Run stress testing (100K/500K/1M records)
  dotnet run legacy          - Run legacy JSON to AJIS migration
  dotnet run images          - Reconstruct images from base64 in countries4.json
  dotnet run convert         - Convert JSON to .atp (AJIS with ATP)
  dotnet run perf            - Run isolated performance tests (lexer/parser/serializer)
  dotnet run parsers         - Parser competition (Old AjisUtf8Parser vs New vs STJ vs NSJ)
  dotnet run best            - Best-of-Breed selection (ALL variants, find winners)
  dotnet run both            - Run both baseline and stress testing
  dotnet run all             - Interactive AJIS demo with countries database search

Examples:
  dotnet run                      # Runs baseline benchmark
  dotnet run stress               # Runs stress tests with 100K, 500K, 1M records
  dotnet run images               # Extracts flag images from countries4.json as ATP
  dotnet run convert              # Converts JSON files to .atp format
  dotnet run perf                 # Isolated performance tests for optimization
  dotnet run parsers              # Compare old vs new parsers
  dotnet run all                  # Interactive demo: performance + live country search

The benchmark suite includes:
  - Baseline:    Small object (1KB) to Large array (100KB) testing
  - Stress:      100K to 1M record processing with fair competition
  - Legacy:      Real JSON migration demo with ATP
  - Images:      Base64 image extraction and reconstruction to binary attachments
  - Convert:     JSON → AJIS → .atp automatic conversion with binary detection
  - Parsers:     Competition between old AjisUtf8Parser vs new FastDeserializer
  - All:         Interactive demo showcasing AJIS file database capabilities
""");
    }

    private static string FindSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        
        while (currentDirectory != null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Ajis.Dotnet.sln")) ||
                Directory.Exists(Path.Combine(currentDirectory.FullName, "test_data_legacy")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return "D:\\Ajis.Dotnet";
    }

    private static async Task RunInteractiveDemo()
    {
        Console.WriteLine("🌍 AJIS INTERACTIVE DEMO - Countries Database");
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("This demo showcases AJIS file-based database capabilities:");
        Console.WriteLine("• Fast indexed lookups (13.8x faster than enumeration)");
        Console.WriteLine("• Linq query support");
        Console.WriteLine("• Lazy loading and background saves");
        Console.WriteLine("• Real-time observable file changes");
        Console.WriteLine();

        // Run countries benchmark first to show performance
        await CountriesBenchmark.RunAsync();
        
        Console.WriteLine();
        Console.WriteLine("🎯 INTERACTIVE COUNTRY SEARCH");
        Console.WriteLine("══════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("Now let's try interactive search!");
        Console.WriteLine("• Enter a full country name to find it");
        Console.WriteLine("• Enter 3+ characters to see matching countries");
        Console.WriteLine("• Type 'quit' or 'exit' to end");
        Console.WriteLine();

        // Create demo file with countries
        var countries = CountriesBenchmark.GenerateCountries(195);
        const string demoFile = "demo_countries.json";
        
        Console.WriteLine("📁 Creating demo countries file...");
        Afrowave.AJIS.IO.AjisFile.Create(demoFile, countries);
        Console.WriteLine($"   ✅ Created {demoFile} with {countries.Count} countries");
        Console.WriteLine();

        // Create index for fast lookups
        Console.WriteLine("🔍 Building search index...");
        using var index = Afrowave.AJIS.IO.AjisFile.CreateIndex<Afrowave.AJIS.Benchmarks.Country>(demoFile, "Name");
        index.Build();
        Console.WriteLine("   ✅ Index built for fast lookups");
        Console.WriteLine();

        // Interactive loop
        while (true)
        {
            Console.Write("🔎 Search countries: ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
                continue;
                
            if (input.ToLower() is "quit" or "exit" or "q")
                break;

            try
            {
                if (input.Length >= 3)
                {
                    // Search for countries containing the input (demonstrating nested field search)
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var matchingCountries = Afrowave.AJIS.IO.AjisFile.FindByPredicate<Afrowave.AJIS.Benchmarks.Country>(
                        demoFile, c => c.Name.Official.Contains(input, StringComparison.OrdinalIgnoreCase) || 
                                      c.Name.Common.Contains(input, StringComparison.OrdinalIgnoreCase));
                    stopwatch.Stop();

                    var results = matchingCountries.ToList();
                    
                    Console.WriteLine($"📊 Found {results.Count} countries in {stopwatch.Elapsed.TotalMilliseconds:F1}ms:");
                    
                    foreach (var country in results.Take(10)) // Show first 10
                    {
                        Console.WriteLine($"   🏛️  {country.Name.Official} ({country.Name.Common}) - {country.Capital} ({country.Region})");
                    }
                    
                    if (results.Count > 10)
                        Console.WriteLine($"   ... and {results.Count - 10} more");
                }
                else
                {
                    // Exact match lookup
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var country = Afrowave.AJIS.IO.AjisFile.FindByKey<Afrowave.AJIS.Benchmarks.Country>(demoFile, "Name", input);
                    stopwatch.Stop();

                    if (country != null)
                    {
                        Console.WriteLine($"🎯 Found in {stopwatch.Elapsed.TotalMilliseconds:F1}ms:");
                        Console.WriteLine($"   🏛️  Country: {country.Name}");
                        Console.WriteLine($"   🏛️  Capital: {country.Capital}");
                        Console.WriteLine($"   🌍 Region: {country.Region}");
                        Console.WriteLine($"   👥 Population: {country.Population:N0}");
                        Console.WriteLine($"   📏 Area: {country.Area:N0} km²");
                        Console.WriteLine($"   💰 Currencies: {string.Join(", ", country.Currencies)}");
                        Console.WriteLine($"   🗣️  Languages: {string.Join(", ", country.Languages)}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Country '{input}' not found");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        // Cleanup
        if (File.Exists(demoFile))
            File.Delete(demoFile);

        Console.WriteLine("👋 Thanks for trying AJIS interactive demo!");
        Console.WriteLine("   AJIS combines JSON performance with database-like features!");
    }
}
