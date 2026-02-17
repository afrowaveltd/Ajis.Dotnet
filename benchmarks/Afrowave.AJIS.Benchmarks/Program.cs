#nullable enable

using Afrowave.AJIS.Benchmarks.Baseline;
using Afrowave.AJIS.Benchmarks.Conversion;
using Afrowave.AJIS.Benchmarks.Legacy;
using Afrowave.AJIS.Benchmarks.StressTest;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Main entry point for AJIS benchmarking suite.
/// </summary>
public static class Program
{
   public static async Task Main(string[] args)
   {
      if(args.Length == 0 || args[0].Equals("baseline", StringComparison.CurrentCultureIgnoreCase))
      {
         RunBaselineBenchmark();
      }
      else if(args[0].Equals("stress", StringComparison.CurrentCultureIgnoreCase))
      {
         RunStressTesting();
      }
      else if(args[0].Equals("legacy", StringComparison.CurrentCultureIgnoreCase))
      {
         RunLegacyMigration();
      }
      else if(args[0].Equals("images", StringComparison.CurrentCultureIgnoreCase))
      {
         RunImageReconstruction();
      }
      else if(args[0].Equals("convert", StringComparison.CurrentCultureIgnoreCase))
      {
         RunJsonToAtpConversion();
      }
      else if(args[0].Equals("perf", StringComparison.CurrentCultureIgnoreCase))
      {
         SimplePerfTest.Run();
      }
      else if(args[0].Equals("roundtrip", StringComparison.CurrentCultureIgnoreCase))
      {
         RoundTripStressTest.Run();
      }
      else if(args[0].Equals("parsers", StringComparison.CurrentCultureIgnoreCase))
      {
         RunParserComparison();
      }
      else if(args[0].Equals("best", StringComparison.CurrentCultureIgnoreCase))
      {
         RunBestOfBreed();
      }
      else if(args[0].Equals("both", StringComparison.CurrentCultureIgnoreCase))
      {
         RunBaselineBenchmark();
         Console.WriteLine("\n\n");
         RunStressTesting();
      }
      else if(args[0].Equals("all", StringComparison.CurrentCultureIgnoreCase))
      {
         // Run ALL benchmarks one by one
         Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
         Console.WriteLine("║              RUNNING ALL BENCHMARKS                            ║");
         Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
         Console.WriteLine();

         try
         {
            // 1. Baseline Benchmark
            RunBaselineBenchmark();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 2. Best-of-Breed
            RunBestOfBreed();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 3. Simple Performance Test
            Console.WriteLine("════════════════════════════════════════════════════════");
            Console.WriteLine("            SIMPLE PERFORMANCE TEST");
            Console.WriteLine("════════════════════════════════════════════════════════");
            SimplePerfTest.Run();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 4. Parser Comparison
            RunParserComparison();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 5. Round Trip Stress Test
            Console.WriteLine("════════════════════════════════════════════════════════");
            Console.WriteLine("            ROUND TRIP STRESS TEST");
            Console.WriteLine("════════════════════════════════════════════════════════");
            RoundTripStressTest.Run();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 6. Stress Testing
            RunStressTesting();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 7. Countries Interactive Demo
            Console.WriteLine("════════════════════════════════════════════════════════");
            Console.WriteLine("         COUNTRIES INTERACTIVE DEMO");
            Console.WriteLine("════════════════════════════════════════════════════════");
            await CountriesBenchmark.RunAsync();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 8. Performance Test Suite
            RunPerformanceTests();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 9. JSON to ATP Conversion
            RunJsonToAtpConversion();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // 10. Legacy Migration
            RunLegacyMigration();
            Console.WriteLine("\n" + new string('═', 60) + "\n");

            // Summary
            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ALL BENCHMARKS COMPLETED                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
         }
         catch(Exception ex)
         {
            Console.WriteLine($"\n❌ Error during benchmark execution: {ex.Message}");
         }
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
      BaselineProgram.RunBaseline([]);
   }

   private static void RunStressTesting()
   {
      Console.WriteLine("════════════════════════════════════════════════════════");
      Console.WriteLine("            STRESS TESTING");
      Console.WriteLine("════════════════════════════════════════════════════════");
      StressTestProgram.RunStressTest([]);
   }

   private static void RunLegacyMigration()
   {
      Console.WriteLine("════════════════════════════════════════════════════════");
      Console.WriteLine("            LEGACY JSON → AJIS MIGRATION");
      Console.WriteLine("════════════════════════════════════════════════════════");
      LegacyMigrationProgram.RunMigration([]);
   }

   private static void RunImageReconstruction()
   {
      Console.WriteLine("════════════════════════════════════════════════════════");
      Console.WriteLine("        IMAGE RECONSTRUCTION FROM LEGACY JSON");
      Console.WriteLine("════════════════════════════════════════════════════════");

      // Resolve path correctly - relative to solution root, not bin directory
      string solutionRoot = FindSolutionRoot();
      string jsonFile = Path.Combine(solutionRoot, "test_data_legacy", "countries4.json");

      if(!File.Exists(jsonFile))
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
      JsonToAtpConversionProgram.RunJsonToAtp([]);
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
  dotnet run                 - Run baseline benchmark (default)
  dotnet run baseline        - Run baseline benchmark
  dotnet run stress          - Run stress testing (100K/500K/1M records)
  dotnet run legacy          - Run legacy JSON to AJIS migration
  dotnet run images          - Reconstruct images from base64 in countries4.json
  dotnet run convert         - Convert JSON to .atp (AJIS with ATP)
  dotnet run perf            - Run isolated performance tests (lexer/parser/serializer)
  dotnet run roundtrip       - Round-trip stress test (serialize → deserialize)
  dotnet run parsers         - Parser competition (Old AjisUtf8Parser vs New vs STJ vs NSJ)
  dotnet run best            - Best-of-Breed selection (ALL variants, find winners)
  dotnet run countries       - Interactive AJIS demo with countries database search
  dotnet run both            - Run both baseline and stress testing
  dotnet run all             - **RUN ALL BENCHMARKS** (comprehensive test suite)

Examples:
  dotnet run                      # Runs baseline benchmark
  dotnet run stress               # Runs stress tests with 100K, 500K, 1M records
  dotnet run images               # Extracts flag images from countries4.json as ATP
  dotnet run convert              # Converts JSON files to .atp format
  dotnet run perf                 # Isolated performance tests for optimization
  dotnet run parsers              # Compare old vs new parsers
  dotnet run countries            # Interactive demo: performance + live country search
  dotnet run all                  # **RUNS EVERYTHING: All benchmarks one by one**

The benchmark suite includes:
  - Baseline:    Small object (1KB) to Large array (100KB) testing
  - Best:        Best-of-Breed selection - find fastest parser/serializer
  - Perf:        Simple performance test - AJIS vs STJ comparison
  - Parsers:     Competition between old AjisUtf8Parser vs new FastDeserializer
  - RoundTrip:   Serialize → Deserialize stress test
  - Stress:      100K to 1M record processing with fair competition
  - Countries:   Interactive demo showcasing AJIS file database capabilities
  - PerformanceTests: Comprehensive performance test suite (isolated)
  - Convert:     JSON → AJIS → .atp automatic conversion with binary detection
  - Legacy:      Real JSON migration demo with ATP
  - ALL:         **Runs ALL of the above sequentially**
""");
   }

   private static string FindSolutionRoot()
   {
      var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

      while(currentDirectory != null)
      {
         if(File.Exists(Path.Combine(currentDirectory.FullName, "Ajis.Dotnet.sln")) ||
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
      while(true)
      {
         Console.Write("🔎 Search countries: ");
         string? input = Console.ReadLine()?.Trim();

         if(string.IsNullOrEmpty(input))
            break;

         if(input.ToLower() is "quit" or "exit" or "q")
            break;

         try
         {
            if(input.Length >= 3)
            {
               // Search for countries containing the input (demonstrating nested field search)
               var stopwatch = System.Diagnostics.Stopwatch.StartNew();
               var matchingCountries = Afrowave.AJIS.IO.AjisFile.FindByPredicate<Afrowave.AJIS.Benchmarks.Country>(
                   demoFile, c => c.Name.Official.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                                 c.Name.Common.Contains(input, StringComparison.OrdinalIgnoreCase));
               stopwatch.Stop();

               var results = matchingCountries.ToList();

               Console.WriteLine($"📊 Found {results.Count} countries in {stopwatch.Elapsed.TotalMilliseconds:F1}ms:");

               foreach(var country in results.Take(10)) // Show first 10
               {
                  Console.WriteLine($"   🏛️  {country.Name.Official} ({country.Name.Common}) - {country.Capital} ({country.Region})");
               }

               if(results.Count > 10)
                  Console.WriteLine($"   ... and {results.Count - 10} more");
            }
            else
            {
               // Exact match lookup
               var stopwatch = System.Diagnostics.Stopwatch.StartNew();
               var country = Afrowave.AJIS.IO.AjisFile.FindByKey<Afrowave.AJIS.Benchmarks.Country>(demoFile, "Name", input);
               stopwatch.Stop();

               if(country != null)
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
         catch(Exception ex)
         {
            Console.WriteLine($"❌ Error: {ex.Message}");
         }

         Console.WriteLine();
      }

      // Cleanup
      if(File.Exists(demoFile))
         File.Delete(demoFile);

      Console.WriteLine("👋 Thanks for trying AJIS interactive demo!");
      Console.WriteLine("   AJIS combines JSON performance with database-like features!");
   }
}