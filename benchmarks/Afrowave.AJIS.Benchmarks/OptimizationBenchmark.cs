using BenchmarkDotNet.Attributes;
using System.Text;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Benchmarks;

[SimpleJob(warmupCount: 3, launchCount: 1, iterationCount: 5)]
[MemoryDiagnoser]
public class OptimizationBenchmark
{
    private readonly AjisConverter<List<TestObject>> _ajisConverter = new();
    private string _testJson = "";
    private List<TestObject> _testData = new();

    [GlobalSetup]
    public void Setup()
    {
        // Generate test data - 10K objects
        _testData = GenerateTestData(10000);
        _testJson = System.Text.Json.JsonSerializer.Serialize(_testData);
    }

    [Benchmark(Description = "AJIS Deserialize 10K")]
    public List<TestObject>? AjisDeserialize()
    {
        return _ajisConverter.Deserialize(_testJson);
    }

    [Benchmark(Baseline = true, Description = "STJ Deserialize 10K")]
    public List<TestObject>? StjDeserialize()
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(_testJson);
    }

    [Benchmark(Description = "AJIS Serialize 10K")]
    public string AjisSerialize()
    {
        return _ajisConverter.Serialize(_testData);
    }

    [Benchmark(Description = "STJ Serialize 10K")]
    public string StjSerialize()
    {
        return System.Text.Json.JsonSerializer.Serialize(_testData);
    }

    public static List<TestObject> GenerateTestData(int count)
    {
        var random = new Random(42);
        var result = new List<TestObject>(count);

        for (int i = 0; i < count; i++)
        {
            result.Add(new TestObject
            {
                Id = i,
                Name = $"Object_{i}",
                Value = random.Next(1000),
                Score = random.NextDouble() * 100,
                Active = i % 2 == 0,
                Tags = new[] { $"tag_{i % 10}", $"type_{i % 5}" },
                Items = GenerateNestedItems(random, i % 5)
            });
        }

        return result;
    }

    private static List<TestItem> GenerateNestedItems(Random random, int count)
    {
        var items = new List<TestItem>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(new TestItem
            {
                ItemId = i,
                ItemName = $"Item_{i}",
                Amount = random.Next(1000)
            });
        }

        return items;
    }
}

public class TestObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public double Score { get; set; }
    public bool Active { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public List<TestItem> Items { get; set; } = new();
}

public class TestItem
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public int Amount { get; set; }
}