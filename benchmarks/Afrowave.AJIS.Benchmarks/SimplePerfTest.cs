#nullable enable

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Afrowave.AJIS.Benchmarks;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Simple performance test to measure AJIS vs STJ
/// </summary>
public static class SimplePerfTest
{
    public static void Run()
    {
        Console.WriteLine("🚀 AJIS Performance Test - Optimized Version");
        Console.WriteLine("============================================");

        // Generate test data
        var testData = OptimizationBenchmark.GenerateTestData(10000);
        var json = System.Text.Json.JsonSerializer.Serialize(testData);

        Console.WriteLine($"Test data: {testData.Count} objects");
        Console.WriteLine($"JSON size: {json.Length / 1024.0:F1} KB");
        Console.WriteLine();

        // Test AJIS Fast Deserializer
        var ajisTime = Measure(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            var list = new List<TestObject>();
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var obj = TestObjectFastDeserializer.Deserialize(ref reader);
                    if (obj != null)
                        list.Add(obj);
                }
            }
            return list;
        });

        // Test STJ
        var stjTime = Measure(() =>
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(json) ?? new List<TestObject>();
        });

        // Test AJIS Fast Serializer
        var ajisSerializeTime = Measure(() =>
        {
            return TestObjectFastSerializer.Serialize(testData);
        });

        // Test STJ Serializer
        var stjSerializeTime = Measure(() =>
        {
            return System.Text.Json.JsonSerializer.Serialize(testData);
        });

        Console.WriteLine("📊 RESULTS:");
        Console.WriteLine($"AJIS Deserialize:  {ajisTime,6:N0} ms");
        Console.WriteLine($"STJ Deserialize:   {stjTime,6:N0} ms");
        Console.WriteLine($"Ratio:             {ajisTime / (double)stjTime,6:F2}x");
        Console.WriteLine();
        Console.WriteLine($"AJIS Serialize:    {ajisSerializeTime,6:N0} ms");
        Console.WriteLine($"STJ Serialize:     {stjSerializeTime,6:N0} ms");
        Console.WriteLine($"Ratio:             {ajisSerializeTime / (double)stjSerializeTime,6:F2}x");
    }

    private static long Measure(Func<object> action)
    {
        // Warmup
        for (int i = 0; i < 3; i++)
            action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        var result = action();
        sw.Stop();

        return sw.ElapsedMilliseconds;
    }
}