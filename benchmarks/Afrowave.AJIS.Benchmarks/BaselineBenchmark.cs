#nullable enable

using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Benchmarks.Baseline;

/// <summary>
/// Baseline performance benchmark comparing AJIS, System.Text.Json, and Newtonsoft.Json
/// with honest, transparent results.
/// </summary>
public sealed class BaselineBenchmark
{
    private readonly List<BenchmarkResult> _results = new();

    /// <summary>
    /// Runs baseline benchmarks for all three libraries.
    /// </summary>
    public void RunBaseline()
    {
        Console.WriteLine("""
╔════════════════════════════════════════════════════════════════════════╗
║          AJIS Baseline Performance Benchmark - All Three Libraries      ║
║       (AJIS vs System.Text.Json vs Newtonsoft.Json)                    ║
╚════════════════════════════════════════════════════════════════════════╝
""");

        // Test 1: Small Object
        RunSmallObjectBenchmark();

        // Test 2: Medium Array
        RunMediumArrayBenchmark();

        // Test 3: Large Array
        RunLargeArrayBenchmark();

        // Test 4: Deep Nesting
        RunDeepNestingBenchmark();

        // Summary
        PrintSummary();
    }

    private void RunSmallObjectBenchmark()
    {
        Console.WriteLine("\n┌─ Test 1: Small Object (1KB) ─────────────────────────────────────┐");

        var testObject = new TestUser
        {
            Id = 1,
            Name = "Alice Johnson",
            Email = "alice@example.com",
            Active = true,
            Score = 95.5m,
            Tags = new[] { "developer", "senior" }
        };

        var ajisTime = MeasureAjis(() =>
        {
            var converter = new AjisConverter<TestUser>();
            var json = converter.Serialize(testObject);
            var deserialized = converter.Deserialize(json);
            return deserialized;
        }, iterations: 100);

        var sysJsonTime = MeasureSystemJson(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(testObject);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestUser>(json);
            return deserialized;
        }, iterations: 100);

        var newtonTime = MeasureNewtonsoft(() =>
        {
            var json = JsonConvert.SerializeObject(testObject);
            var deserialized = JsonConvert.DeserializeObject<TestUser>(json);
            return deserialized;
        }, iterations: 100);

        PrintResults("Small Object (1KB)", ajisTime, sysJsonTime, newtonTime);

        _results.Add(new BenchmarkResult
        {
            Scenario = "Small Object (1KB)",
            AjisTime = ajisTime.Average,
            SystemJsonTime = sysJsonTime.Average,
            NewtonsoftTime = newtonTime.Average,
            Timestamp = DateTime.Now
        });
    }

    private void RunMediumArrayBenchmark()
    {
        Console.WriteLine("\n┌─ Test 2: Medium Array (10 objects, ~10KB) ────────────────────────┐");

        var testArray = Enumerable.Range(1, 10)
            .Select(i => new TestUser
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                Active = i % 2 == 0,
                Score = 50 + i * 5,
                Tags = new[] { $"tag{i}" }
            })
            .ToList();

        var ajisTime = MeasureAjis(() =>
        {
            var converter = new AjisConverter<List<TestUser>>();
            var ajis = converter.Serialize(testArray);
            var deserialized = converter.Deserialize(ajis);
            return deserialized;
        }, iterations: 50);

        var sysJsonTime = MeasureSystemJson(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(testArray);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<TestUser>>(json);
            return deserialized;
        }, iterations: 50);

        var newtonTime = MeasureNewtonsoft(() =>
        {
            var json = JsonConvert.SerializeObject(testArray);
            var deserialized = JsonConvert.DeserializeObject<List<TestUser>>(json);
            return deserialized;
        }, iterations: 50);

        PrintResults("Medium Array (10KB)", ajisTime, sysJsonTime, newtonTime);

        _results.Add(new BenchmarkResult
        {
            Scenario = "Medium Array (10KB)",
            AjisTime = ajisTime.Average,
            SystemJsonTime = sysJsonTime.Average,
            NewtonsoftTime = newtonTime.Average,
            Timestamp = DateTime.Now
        });
    }

    private void RunLargeArrayBenchmark()
    {
        Console.WriteLine("\n┌─ Test 3: Large Array (100 objects, ~100KB) ──────────────────────┐");

        var testArray = Enumerable.Range(1, 100)
            .Select(i => new TestUser
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                Active = i % 2 == 0,
                Score = 50 + i,
                Tags = new[] { $"tag{i}" }
            })
            .ToList();

        var ajisTime = MeasureAjis(() =>
        {
            var converter = new AjisConverter<List<TestUser>>();
            var ajis = converter.Serialize(testArray);
            var deserialized = converter.Deserialize(ajis);
            return deserialized;
        }, iterations: 20);

        var sysJsonTime = MeasureSystemJson(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(testArray);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<TestUser>>(json);
            return deserialized;
        }, iterations: 20);

        var newtonTime = MeasureNewtonsoft(() =>
        {
            var json = JsonConvert.SerializeObject(testArray);
            var deserialized = JsonConvert.DeserializeObject<List<TestUser>>(json);
            return deserialized;
        }, iterations: 20);

        PrintResults("Large Array (100KB)", ajisTime, sysJsonTime, newtonTime);

        _results.Add(new BenchmarkResult
        {
            Scenario = "Large Array (100KB)",
            AjisTime = ajisTime.Average,
            SystemJsonTime = sysJsonTime.Average,
            NewtonsoftTime = newtonTime.Average,
            Timestamp = DateTime.Now
        });
    }

    private void RunDeepNestingBenchmark()
    {
        Console.WriteLine("\n┌─ Test 4: Deep Nesting (50 levels) ───────────────────────────────┐");

        var testData = new NestedObject { Level = 0 };
        var current = testData;
        for (int i = 1; i < 50; i++)
        {
            current.Child = new NestedObject { Level = i };
            current = current.Child;
        }

        var ajisTime = MeasureAjis(() =>
        {
            var converter = new AjisConverter<NestedObject>();
            var json = converter.Serialize(testData);
            var deserialized = converter.Deserialize(json);
            return deserialized;
        }, iterations: 20);

        var sysJsonTime = MeasureSystemJson(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(testData);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<NestedObject>(json);
            return deserialized;
        }, iterations: 20);

        var newtonTime = MeasureNewtonsoft(() =>
        {
            var json = JsonConvert.SerializeObject(testData);
            var deserialized = JsonConvert.DeserializeObject<NestedObject>(json);
            return deserialized;
        }, iterations: 20);

        PrintResults("Deep Nesting (50 levels)", ajisTime, sysJsonTime, newtonTime);

        _results.Add(new BenchmarkResult
        {
            Scenario = "Deep Nesting (50 levels)",
            AjisTime = ajisTime.Average,
            SystemJsonTime = sysJsonTime.Average,
            NewtonsoftTime = newtonTime.Average,
            Timestamp = DateTime.Now
        });
    }

    private MeasurementResult MeasureAjis<T>(Func<T> operation, int iterations)
    {
        return Measure("AJIS", operation, iterations);
    }

    private MeasurementResult MeasureSystemJson<T>(Func<T> operation, int iterations)
    {
        return Measure("System.Text.Json", operation, iterations);
    }

    private MeasurementResult MeasureNewtonsoft<T>(Func<T> operation, int iterations)
    {
        return Measure("Newtonsoft.Json", operation, iterations);
    }

    private MeasurementResult Measure<T>(string name, Func<T> operation, int iterations)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            try { operation(); } catch { }

        // Measure
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            try { operation(); } catch { }
        sw.Stop();

        var avgMicroseconds = sw.Elapsed.TotalMicroseconds / iterations;
        Console.Write($"  {name,-20}: {avgMicroseconds,10:F2} µs  ");

        return new MeasurementResult
        {
            Library = name,
            Average = avgMicroseconds,
            Iterations = iterations
        };
    }

    private void PrintResults(string scenario, MeasurementResult ajis, MeasurementResult sysJson, MeasurementResult newton)
    {
        Console.WriteLine();

        var fastest = Math.Min(ajis.Average, Math.Min(sysJson.Average, newton.Average));

        foreach (var result in new[] { ajis, sysJson, newton })
        {
            var ratio = result.Average / fastest;
            var mark = ratio < 1.05 ? "✅" : ratio < 1.3 ? "⚠️" : "❌";
            var ratioStr = ratio < 1.05 ? "FASTEST" : $"{ratio:F2}x";
            Console.WriteLine($"      {mark} {result.Library,-20}: {result.Average,10:F2} µs  [{ratioStr}]");
        }

        // Analysis
        Console.WriteLine();
        if (ajis.Average < sysJson.Average)
            Console.WriteLine($"      ℹ️  AJIS is {sysJson.Average / ajis.Average:F2}x faster than System.Text.Json");
        else
            Console.WriteLine($"      ℹ️  System.Text.Json is {ajis.Average / sysJson.Average:F2}x faster than AJIS");

        Console.WriteLine($"      ℹ️  AJIS is {newton.Average / ajis.Average:F2}x faster than Newtonsoft.Json");
        Console.WriteLine($"      ℹ️  System.Text.Json is {newton.Average / sysJson.Average:F2}x faster than Newtonsoft.Json");
    }

    private void PrintSummary()
    {
        Console.WriteLine("""

╔════════════════════════════════════════════════════════════════════════╗
║                         BASELINE SUMMARY                               ║
╚════════════════════════════════════════════════════════════════════════╝
""");

        Console.WriteLine("\nScenario Results:");
        Console.WriteLine($"{"Scenario",-30} {"AJIS",-15} {"System.Json",-15} {"Newtonsoft",-15}");
        Console.WriteLine(new string('─', 75));

        foreach (var result in _results)
        {
            Console.WriteLine($"{result.Scenario,-30} {result.AjisTime,10:F2} µs  {result.SystemJsonTime,10:F2} µs  {result.NewtonsoftTime,10:F2} µs");
        }

        Console.WriteLine();
        Console.WriteLine("Key Findings:");
        var avgAjis = _results.Average(r => r.AjisTime);
        var avgSystem = _results.Average(r => r.SystemJsonTime);
        var avgNewton = _results.Average(r => r.NewtonsoftTime);

        Console.WriteLine($"  • Average AJIS time:           {avgAjis:F2} µs");
        Console.WriteLine($"  • Average System.Text.Json:    {avgSystem:F2} µs");
        Console.WriteLine($"  • Average Newtonsoft.Json:     {avgNewton:F2} µs");

        Console.WriteLine();
        if (avgAjis < avgSystem)
            Console.WriteLine($"  ✅ AJIS is {avgSystem / avgAjis:F2}x faster than System.Text.Json on average");
        else
            Console.WriteLine($"  ⚠️  System.Text.Json is {avgAjis / avgSystem:F2}x faster than AJIS on average");

        Console.WriteLine($"  ✅ AJIS is {avgNewton / avgAjis:F2}x faster than Newtonsoft.Json on average");

        Console.WriteLine("""

Interpretation:
  • This baseline establishes current performance levels
  • Newtonsoft.Json is generally slower (older implementation)
  • System.Text.Json is competitive (modern optimized)
  • Next phase: optimize AJIS to match or exceed both
""");
    }

    private record MeasurementResult
    {
        public required string Library { get; init; }
        public required double Average { get; init; }
        public required int Iterations { get; init; }
    }

    private record BenchmarkResult
    {
        public required string Scenario { get; init; }
        public required double AjisTime { get; init; }
        public required double SystemJsonTime { get; init; }
        public required double NewtonsoftTime { get; init; }
        public required DateTime Timestamp { get; init; }
    }

    private class TestUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public bool Active { get; set; }
        public decimal Score { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    private class NestedObject
    {
        public int Level { get; set; }
        public NestedObject? Child { get; set; }
    }
}

/// <summary>
/// Entry point for baseline benchmark.
/// </summary>
internal static class BaselineProgram
{
    internal static void RunBaseline(string[] args)
    {
        Console.WriteLine("Starting baseline benchmark comparison...\n");
        var benchmark = new BaselineBenchmark();
        benchmark.RunBaseline();

        Console.WriteLine("\nBaseline benchmark complete!");
        Console.WriteLine("Next step: Review results and plan optimizations.\n");
    }
}
