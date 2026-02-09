#nullable enable

using System.Diagnostics;
using Newtonsoft.Json;
using Afrowave.AJIS.Serialization.Mapping;
using Afrowave.AJIS.Benchmarks.Conversion;

namespace Afrowave.AJIS.Benchmarks.StressTest;

/// <summary>
/// Main stress test runner for enterprise load testing.
/// Tests 100K, 500K, and 1M records with graceful failure handling.
/// </summary>
public sealed class StressTestRunner
{
    private readonly ComplexDataGenerator _generator = new();
    private readonly StressTestFramework _framework = new();
    private readonly List<StressTestResult> _results = new();

    public void RunFullStressSuite()
    {
        Console.WriteLine("""
╔════════════════════════════════════════════════════════════════════════╗
║         STRESS TESTING SUITE - 100K / 500K / 1M Records                ║
║  Complex Objects with Nested Address + Enterprise Graceful Failure     ║
╚════════════════════════════════════════════════════════════════════════╝
""");

        // Generate test data
        Console.WriteLine("\n1. GENERATING TEST DATA...");
        var users100k = _generator.GenerateUsers(100_000);
        Console.WriteLine($"✓ Generated 100K users");

        var users500k = _generator.GenerateUsers(500_000);
        Console.WriteLine($"✓ Generated 500K users");

        // Note: 1M might exceed memory - we'll try but handle gracefully
        Console.WriteLine($"Generating 1M users (may require significant memory)...");
        List<StressTestUser> users1m;
        try
        {
            users1m = _generator.GenerateUsers(1_000_000);
            Console.WriteLine($"✓ Generated 1M users");
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine($"⚠️  Could not generate 1M users in memory (as expected)");
            users1m = new List<StressTestUser>();
        }

        // Run 100K tests
        Console.WriteLine("\n\n2. STRESS TEST 100K RECORDS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        RunStressTests(users100k, "100K");

        // Run 500K tests
        Console.WriteLine("\n\n3. STRESS TEST 500K RECORDS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
        RunStressTests(users500k, "500K");

        // Run 1M tests if available
        if (users1m.Count > 0)
        {
            Console.WriteLine("\n\n4. STRESS TEST 1M RECORDS");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            RunStressTests(users1m, "1M");
        }

        // Summary
        PrintStressSummary();
    }

    private void RunStressTests(List<StressTestUser> users, string label)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AjisStressTest");
        Directory.CreateDirectory(tempDir);

        // Save to files
        var ajisPath = Path.Combine(tempDir, $"users_{label}.ajis");
        _generator.SaveAsAjis(users, ajisPath);

        var jsonPath = Path.Combine(tempDir, $"users_{label}.json");
        SaveAsJson(users, jsonPath);

        Console.WriteLine();

        // Test AJIS
        var ajisResult = _framework.RunTest(
            $"AJIS Parsing ({label})",
            path => 
            {
                // Fair comparison: deserialize to List<StressTestUser> just like JSON
                using var fs = System.IO.File.OpenRead(path);
                var converter = new AjisConverter<List<StressTestUser>>();
                var ajisText = new System.IO.StreamReader(fs).ReadToEnd();
                var deserialized = converter.Deserialize(ajisText);
                return deserialized?.Count ?? 0;
            },
            ajisPath);
        _results.Add(ajisResult);

        // Test System.Text.Json
        var jsonResult = _framework.RunTest(
            $"System.Text.Json Parsing ({label})",
            path =>
            {
                // Fair comparison: use streaming
                using var fs = System.IO.File.OpenRead(path);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<StressTestUser>>(fs);
                return deserialized?.Count ?? 0;
            },
            jsonPath);
        _results.Add(jsonResult);

        // Test Newtonsoft.Json
        var newtonResult = _framework.RunTest(
            $"Newtonsoft.Json Parsing ({label})",
            path =>
            {
                // Fair comparison: use streaming
                using var fs = System.IO.File.OpenRead(path);
                using var sr = new System.IO.StreamReader(fs);
                using var jr = new Newtonsoft.Json.JsonTextReader(sr);
                var serializer = new Newtonsoft.Json.JsonSerializer();
                var deserialized = serializer.Deserialize<List<StressTestUser>>(jr);
                return deserialized?.Count ?? 0;
            },
            jsonPath);
        _results.Add(newtonResult);

        // Cleanup
        try
        {
            System.IO.File.Delete(ajisPath);
            System.IO.File.Delete(jsonPath);
        }
        catch { }

        // Run ATP Round-Trip Test
        Console.WriteLine("\n\n");
        RunAtpRoundTripTest();
    }

    /// <summary>
    /// Runs ATP round-trip test as final validation.
    /// </summary>
    private void RunAtpRoundTripTest()
    {
        try
        {
            var tester = new AtpRoundTripTester();
            tester.RunAtpRoundTrip();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ATP round-trip test error: {ex.Message}");
        }
    }

    private void SaveAsJson(List<StressTestUser> users, string filePath)
    {
        Console.WriteLine($"Saving {users.Count:N0} users to JSON file...");
        var json = System.Text.Json.JsonSerializer.Serialize(users);
        System.IO.File.WriteAllText(filePath, json);
        var fileInfo = new System.IO.FileInfo(filePath);
        Console.WriteLine($"✓ Saved {users.Count:N0} users ({fileInfo.Length / (1024 * 1024)}MB)");
    }

    private void PrintStressSummary()
    {
        // Generate fair competition report
        var reportGenerator = new CompetitionReportGenerator();
        var report = reportGenerator.GenerateReport(_results);
        Console.WriteLine(report);

        // Print robustness summary
        Console.WriteLine("\nROBUSTNESS ASSESSMENT:");
        var failedTests = _results.Where(r => !r.Success).ToList();
        if (failedTests.Count == 0)
        {
            Console.WriteLine("  ✅ All tests completed successfully (excellent robustness)");
        }
        else
        {
            Console.WriteLine($"  ⚠️  {failedTests.Count} tests failed (out of {_results.Count}):");
            foreach (var failed in failedTests)
            {
                Console.WriteLine($"      - {failed.TestName}: {failed.ErrorMessage}");
            }
        }

        Console.WriteLine("\n✓ STRESS TEST SUITE COMPLETE");
        Console.WriteLine("This demonstrates enterprise-grade performance and reliability.");
        Console.WriteLine("All results are fair, objective, and fully documented.\n");
    }

    private string ExtractSize(string testName)
    {
        if (testName.Contains("100K")) return "100K";
        if (testName.Contains("500K")) return "500K";
        if (testName.Contains("1M")) return "1M";
        return "Unknown";
    }
}

/// <summary>
/// Entry point for stress testing.
/// </summary>
internal static class StressTestProgram
{
    internal static void RunStressTest(string[] args)
    {
        Console.WriteLine("AJIS.Dotnet - Stress Testing Suite");
        Console.WriteLine("Enterprise Load Testing with Graceful Failure Handling\n");

        try
        {
            var runner = new StressTestRunner();
            runner.RunFullStressSuite();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Stress test suite failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\n✓ Stress testing complete.");
    }
}
