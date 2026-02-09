using System.Diagnostics;
using System.Text;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Best-of-Breed benchmark - tests ALL parser/lexer/serializer variants.
/// Goal: Find the FASTEST implementation for each component.
/// </summary>
public sealed class BestOfBreedBenchmark
{
    public void Run()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║               BEST-OF-BREED SELECTION BENCHMARK                        ║");
        Console.WriteLine("║      Testing ALL variants to find the FASTEST implementation          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Test at multiple scales
        Console.WriteLine("Testing at 3 scales: 10K, 100K, 1M records");
        Console.WriteLine();

        var results = new Dictionary<string, BenchmarkResult>();

        // Small scale (10K)
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("SCALE: 10,000 RECORDS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");
        RunAllTests(10_000, "10K", results);

        // Medium scale (100K)
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("SCALE: 100,000 RECORDS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");
        RunAllTests(100_000, "100K", results);

        // Large scale (1M)
        Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
        Console.WriteLine("SCALE: 1,000,000 RECORDS");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");
        RunAllTests(1_000_000, "1M", results);

        // Print final summary
        PrintFinalSummary(results);
    }

    private void RunAllTests(int recordCount, string label, Dictionary<string, BenchmarkResult> results)
    {
        var testData = GenerateTestData(recordCount);
        var jsonString = System.Text.Json.JsonSerializer.Serialize(testData);
        var jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        Console.WriteLine($"Data size: {jsonBytes.Length / 1024.0:F2} KB\n");

        // PARSERS (Deserialization)
        Console.WriteLine("🔍 TESTING PARSERS (Deserialization):");
        Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

        TestParser($"Current-FastDeserializer-{label}", () => 
            BenchmarkCurrentFastDeserializer(jsonString), results);

        // TODO: Legacy Utf8Parser - needs namespace fixing
        // TestParser($"Legacy-Utf8Parser-{label}", () =>
        //     BenchmarkLegacyUtf8Parser(jsonBytes), results);

        TestParser($"SystemTextJson-{label}", () => 
            BenchmarkSystemTextJson(jsonBytes), results);

        TestParser($"NewtonsoftJson-{label}", () => 
            BenchmarkNewtonsoftJson(jsonString), results);

        // SERIALIZERS (Object → JSON)
        Console.WriteLine("\n📤 TESTING SERIALIZERS (Object → JSON):");
        Console.WriteLine("─────────────────────────────────────────────────────────────────\n");

        TestSerializer($"Current-AjisConverter-{label}", () =>
            BenchmarkCurrentSerializer(testData), results);

        TestSerializer($"SystemTextJson-Serializer-{label}", () =>
            BenchmarkSystemTextJsonSerializer(testData), results);

        TestSerializer($"NewtonsoftJson-Serializer-{label}", () =>
            BenchmarkNewtonsoftJsonSerializer(testData), results);
    }

    private void TestParser(string name, Func<BenchmarkResult> test, Dictionary<string, BenchmarkResult> results)
    {
        try
        {
            var result = test();
            results[name] = result;
            PrintResult(name, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {name}: FAILED - {ex.Message}\n");
        }
    }

    private void TestSerializer(string name, Func<BenchmarkResult> test, Dictionary<string, BenchmarkResult> results)
    {
        try
        {
            var result = test();
            results[name] = result;
            PrintResult(name, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {name}: FAILED - {ex.Message}\n");
        }
    }

    private BenchmarkResult BenchmarkCurrentFastDeserializer(string json)
    {
        var converter = new Afrowave.AJIS.Serialization.Mapping.AjisConverter<List<TestObject>>();

        // Warmup
        for (int i = 0; i < 3; i++)
            converter.Deserialize(json);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = converter.Deserialize(json);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = result?.Count > 0
        };
    }

    /* LEGACY PARSER BENCHMARK - DISABLED
    private BenchmarkResult BenchmarkLegacyUtf8Parser(byte[] jsonBytes)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            Legacy.LegacyAjisUtf8Parser.Parse(jsonBytes);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = Legacy.LegacyAjisUtf8Parser.Parse(jsonBytes);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = result != null && result.Type == Legacy.LegacyAjisValueType.Array
        };
    }
    */

    private BenchmarkResult BenchmarkSystemTextJson(byte[] jsonBytes)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(jsonBytes);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(jsonBytes);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = result?.Count > 0
        };
    }

    private BenchmarkResult BenchmarkNewtonsoftJson(string json)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            Newtonsoft.Json.JsonConvert.DeserializeObject<List<TestObject>>(json);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TestObject>>(json);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = result?.Count > 0
        };
    }

    private BenchmarkResult BenchmarkCurrentSerializer(List<TestObject> data)
    {
        var converter = new Afrowave.AJIS.Serialization.Mapping.AjisConverter<List<TestObject>>();

        // Warmup
        for (int i = 0; i < 3; i++)
            converter.Serialize(data);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = converter.Serialize(data);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = !string.IsNullOrEmpty(result)
        };
    }

    private BenchmarkResult BenchmarkSystemTextJsonSerializer(List<TestObject> data)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            System.Text.Json.JsonSerializer.Serialize(data);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = System.Text.Json.JsonSerializer.Serialize(data);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = !string.IsNullOrEmpty(result)
        };
    }

    private BenchmarkResult BenchmarkNewtonsoftJsonSerializer(List<TestObject> data)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            Newtonsoft.Json.JsonConvert.SerializeObject(data);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var baseline = GC.GetTotalMemory(false);
        var gc0Before = GC.CollectionCount(0);
        var gc1Before = GC.CollectionCount(1);
        var gc2Before = GC.CollectionCount(2);

        var sw = Stopwatch.StartNew();
        var result = Newtonsoft.Json.JsonConvert.SerializeObject(data);
        sw.Stop();

        var peak = GC.GetTotalMemory(false);
        var gc0After = GC.CollectionCount(0);
        var gc1After = GC.CollectionCount(1);
        var gc2After = GC.CollectionCount(2);

        return new BenchmarkResult
        {
            TimeMs = sw.ElapsedMilliseconds,
            MemoryMB = (peak - baseline) / 1024 / 1024,
            GC0 = gc0After - gc0Before,
            GC1 = gc1After - gc1Before,
            GC2 = gc2After - gc2Before,
            Success = !string.IsNullOrEmpty(result)
        };
    }

    private void PrintResult(string name, BenchmarkResult result)
    {
        var status = result.Success ? "✅" : "❌";
        Console.WriteLine($"{status} {name}:");
        Console.WriteLine($"   Time:   {result.TimeMs,6:N0} ms");
        Console.WriteLine($"   Memory: {result.MemoryMB,6:N0} MB");
        Console.WriteLine($"   GC:     Gen0={result.GC0} Gen1={result.GC1} Gen2={result.GC2}");
        Console.WriteLine();
    }

    private void PrintFinalSummary(Dictionary<string, BenchmarkResult> results)
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    BEST-OF-BREED WINNERS                               ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝\n");

        // Group by category and scale
        var parsers10K = results.Where(r => r.Key.Contains("10K") && !r.Key.Contains("Serializer")).ToList();
        var parsers100K = results.Where(r => r.Key.Contains("100K") && !r.Key.Contains("Serializer")).ToList();
        var parsers1M = results.Where(r => r.Key.Contains("1M") && !r.Key.Contains("Serializer")).ToList();

        PrintCategoryWinner("PARSER (10K)", parsers10K);
        PrintCategoryWinner("PARSER (100K)", parsers100K);
        PrintCategoryWinner("PARSER (1M)", parsers1M);

        var serializers10K = results.Where(r => r.Key.Contains("10K") && r.Key.Contains("Serializer")).ToList();
        var serializers100K = results.Where(r => r.Key.Contains("100K") && r.Key.Contains("Serializer")).ToList();
        var serializers1M = results.Where(r => r.Key.Contains("1M") && r.Key.Contains("Serializer")).ToList();

        PrintCategoryWinner("SERIALIZER (10K)", serializers10K);
        PrintCategoryWinner("SERIALIZER (100K)", serializers100K);
        PrintCategoryWinner("SERIALIZER (1M)", serializers1M);
    }

    private void PrintCategoryWinner(string category, List<KeyValuePair<string, BenchmarkResult>> results)
    {
        if (results.Count == 0) return;

        var fastest = results.OrderBy(r => r.Value.TimeMs).First();
        var mostMemoryEfficient = results.OrderBy(r => r.Value.MemoryMB).First();
        var leastGC = results.OrderBy(r => r.Value.GC0 + r.Value.GC1 + r.Value.GC2).First();

        Console.WriteLine($"🏆 {category}:");
        Console.WriteLine($"   ⚡ Fastest:        {fastest.Key.Split('-')[1],-20} ({fastest.Value.TimeMs,6:N0} ms)");
        Console.WriteLine($"   💾 Most Efficient: {mostMemoryEfficient.Key.Split('-')[1],-20} ({mostMemoryEfficient.Value.MemoryMB,4:N0} MB)");
        Console.WriteLine($"   🧹 Least GC:       {leastGC.Key.Split('-')[1],-20} ({leastGC.Value.GC0 + leastGC.Value.GC1 + leastGC.Value.GC2,3} collections)");
        Console.WriteLine();
    }

    private List<TestObject> GenerateTestData(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new TestObject
            {
                Id = i,
                Name = $"Object {i}",
                Value = i * 1.5,
                Active = i % 2 == 0
            })
            .ToList();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Value { get; set; }
        public bool Active { get; set; }
    }

    private class BenchmarkResult
    {
        public long TimeMs { get; set; }
        public long MemoryMB { get; set; }
        public int GC0 { get; set; }
        public int GC1 { get; set; }
        public int GC2 { get; set; }
        public bool Success { get; set; }
    }
}
