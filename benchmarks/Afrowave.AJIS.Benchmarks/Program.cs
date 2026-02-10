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
    public static void Main(string[] args)
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
        else if (args[0].ToLower() == "stress")
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
            RunBaselineBenchmark();
            Console.WriteLine("\n\n");
            RunStressTesting();
            Console.WriteLine("\n\n");
            RunLegacyMigration();
            Console.WriteLine("\n\n");
            RunImageReconstruction();
            Console.WriteLine("\n\n");
            RunJsonToAtpConversion();
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
  dotnet run all             - Run all: baseline, stress, legacy, images, convert

Examples:
  dotnet run                      # Runs baseline benchmark
  dotnet run stress               # Runs stress tests with 100K, 500K, 1M records
  dotnet run images               # Extracts flag images from countries4.json as ATP
  dotnet run convert              # Converts JSON files to .atp format
  dotnet run perf                 # Isolated performance tests for optimization
  dotnet run parsers              # Compare old vs new parsers
  dotnet run all                  # Complete benchmark + conversion suite

The benchmark suite includes:
  - Baseline:    Small object (1KB) to Large array (100KB) testing
  - Stress:      100K to 1M record processing with fair competition
  - Legacy:      Real JSON migration demo with ATP
  - Images:      Base64 image extraction and reconstruction to binary attachments
  - Convert:     JSON → AJIS → .atp automatic conversion with binary detection
  - Parsers:     Competition between old AjisUtf8Parser vs new FastDeserializer
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
}
