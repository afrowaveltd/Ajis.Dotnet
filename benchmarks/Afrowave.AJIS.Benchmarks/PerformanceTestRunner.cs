using Afrowave.AJIS.Serialization.Mapping;
using Afrowave.AJIS.Streaming.Segments;
using System.Diagnostics;
using System.Text;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Specialized performance test runner for isolated component benchmarking.
/// Focus: Pure parser/serializer/lexer speed without overhead.
/// </summary>
public sealed class PerformanceTestRunner
{
   public void Run()
   {
      Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
      Console.WriteLine("║           AJIS PERFORMANCE TEST SUITE - ISOLATED COMPONENTS            ║");
      Console.WriteLine("║              Micro-benchmarks for Systematic Optimization              ║");
      Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
      Console.WriteLine();

      // Phase 1: Lexer benchmarks
      RunLexerBenchmarks();

      // Phase 2: Parser benchmarks
      RunParserBenchmarks();

      // Phase 3: Serializer benchmarks
      RunSerializerBenchmarks();

      // Phase 4: Round-trip benchmarks
      RunRoundTripBenchmarks();

      // Phase 5: Memory stress test (10M records)
      RunMemoryStressTest();

      Console.WriteLine("\n✓ Performance test suite complete!");
   }

   private void RunLexerBenchmarks()
   {
      Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine("PHASE 1: LEXER BENCHMARKS (Token Generation Speed)");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

      // Test 1: Number parsing speed
      BenchmarkNumberParsing();

      // Test 2: String parsing speed
      BenchmarkStringParsing();

      // Test 3: Object structure parsing
      BenchmarkObjectParsing();
   }

   private void BenchmarkNumberParsing()
   {
      const int iterations = 1_000_000;
      List<int> numbers = GenerateNumberArray(1000);
      var json = System.Text.Json.JsonSerializer.Serialize(numbers);
      var bytes = Encoding.UTF8.GetBytes(json);

      Console.WriteLine($"🔢 Number Parsing ({iterations:N0} numbers):");

      var sw = Stopwatch.StartNew();
      for(int i = 0; i < iterations / 1000; i++)
      {
         var segments = AjisParse.ParseSegments(bytes).ToList();
      }
      sw.Stop();

      var numbersPerSecond = iterations / sw.Elapsed.TotalSeconds;
      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Throughput: {numbersPerSecond:N0} numbers/second");
      Console.WriteLine($"   Avg/number: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F3} µs");
      Console.WriteLine();
   }

   private void BenchmarkStringParsing()
   {
      const int iterations = 1_000_000;
      List<string> strings = GenerateStringArray(1000);
      var json = System.Text.Json.JsonSerializer.Serialize(strings);
      var bytes = Encoding.UTF8.GetBytes(json);

      Console.WriteLine($"📝 String Parsing ({iterations:N0} strings):");

      var sw = Stopwatch.StartNew();
      for(int i = 0; i < iterations / 1000; i++)
      {
         var segments = AjisParse.ParseSegments(bytes).ToList();
      }
      sw.Stop();

      var stringsPerSecond = iterations / sw.Elapsed.TotalSeconds;
      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Throughput: {stringsPerSecond:N0} strings/second");
      Console.WriteLine($"   Avg/string: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F3} µs");
      Console.WriteLine();
   }

   private void BenchmarkObjectParsing()
   {
      const int iterations = 100_000;
      List<SimpleObject> objects = GenerateSimpleObjects(1000);
      var json = System.Text.Json.JsonSerializer.Serialize(objects);
      var bytes = Encoding.UTF8.GetBytes(json);

      Console.WriteLine($"🏢 Object Parsing ({iterations:N0} objects):");

      var sw = Stopwatch.StartNew();
      for(int i = 0; i < iterations / 1000; i++)
      {
         var segments = AjisParse.ParseSegments(bytes).ToList();
      }
      sw.Stop();

      var objectsPerSecond = iterations / sw.Elapsed.TotalSeconds;
      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Throughput: {objectsPerSecond:N0} objects/second");
      Console.WriteLine($"   Avg/object: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F3} µs");
      Console.WriteLine();
   }

   private void RunParserBenchmarks()
   {
      Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine("PHASE 2: PARSER BENCHMARKS (Bytes → Objects)");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

      BenchmarkParseOnly(10_000, "10K");
      BenchmarkParseOnly(100_000, "100K");
      BenchmarkParseOnly(1_000_000, "1M");
   }

   private void BenchmarkParseOnly(int recordCount, string label)
   {
      List<SimpleObject> objects = GenerateSimpleObjects(recordCount);
      var json = System.Text.Json.JsonSerializer.Serialize(objects);
      var bytes = Encoding.UTF8.GetBytes(json);

      Console.WriteLine($"📥 Parse Only ({label} records):");
      Console.WriteLine($"   File size: {bytes.Length / 1024.0:F2} KB");

      // Warmup
      for(int i = 0; i < 3; i++)
      {
         var _ = AjisParse.ParseSegments(bytes).ToList();
      }

      // Measure
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var sw = Stopwatch.StartNew();
      var segments = AjisParse.ParseSegments(bytes).ToList();
      sw.Stop();

      var mbPerSecond = bytes.Length / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Throughput: {mbPerSecond:F2} MB/s");
      Console.WriteLine($"   Segments:   {segments.Count:N0}");
      Console.WriteLine();
   }

   private void RunSerializerBenchmarks()
   {
      Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine("PHASE 3: SERIALIZER BENCHMARKS (Objects → Bytes)");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

      BenchmarkSerializeOnly(10_000, "10K");
      BenchmarkSerializeOnly(100_000, "100K");
      BenchmarkSerializeOnly(1_000_000, "1M");
   }

   private void BenchmarkSerializeOnly(int recordCount, string label)
   {
      List<SimpleObject> objects = GenerateSimpleObjects(recordCount);

      Console.WriteLine($"📤 Serialize Only ({label} records):");

      // Warmup
      var converter = new AjisConverter<List<SimpleObject>>();
      for(int i = 0; i < 3; i++)
      {
         var _ = converter.Serialize(objects);
      }

      // Measure
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var baselineMemory = GC.GetTotalMemory(false);
      var sw = Stopwatch.StartNew();
      var result = converter.Serialize(objects);
      sw.Stop();
      var peakMemory = GC.GetTotalMemory(false);

      var bytes = Encoding.UTF8.GetByteCount(result);
      var mbPerSecond = bytes / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
      var memoryUsedMB = (peakMemory - baselineMemory) / 1024.0 / 1024.0;

      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Throughput: {mbPerSecond:F2} MB/s");
      Console.WriteLine($"   Output:     {bytes / 1024.0:F2} KB");
      Console.WriteLine($"   Memory:     {memoryUsedMB:F2} MB");
      Console.WriteLine();
   }

   private void RunRoundTripBenchmarks()
   {
      Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine("PHASE 4: ROUND-TRIP BENCHMARKS (Full Cycle)");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

      BenchmarkRoundTrip(10_000, "10K");
      BenchmarkRoundTrip(100_000, "100K");
      BenchmarkRoundTrip(1_000_000, "1M");
   }

   private void BenchmarkRoundTrip(int recordCount, string label)
   {
      List<SimpleObject> objects = GenerateSimpleObjects(recordCount);
      var converter = new AjisConverter<List<SimpleObject>>();

      Console.WriteLine($"🔄 Round-Trip ({label} records):");

      // Warmup
      for(int i = 0; i < 3; i++)
      {
         var warmupJson = converter.Serialize(objects);
         List<SimpleObject>? _ = converter.Deserialize(warmupJson);
      }

      // Measure
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var baselineMemory = GC.GetTotalMemory(false);
      var gcBefore0 = GC.CollectionCount(0);
      var gcBefore1 = GC.CollectionCount(1);
      var gcBefore2 = GC.CollectionCount(2);

      var sw = Stopwatch.StartNew();
      var json = converter.Serialize(objects);
      List<SimpleObject>? deserialized = converter.Deserialize(json);
      sw.Stop();

      var peakMemory = GC.GetTotalMemory(false);
      var gcAfter0 = GC.CollectionCount(0);
      var gcAfter1 = GC.CollectionCount(1);
      var gcAfter2 = GC.CollectionCount(2);

      var memoryUsedMB = (peakMemory - baselineMemory) / 1024.0 / 1024.0;

      Console.WriteLine($"   Time:       {sw.ElapsedMilliseconds:N0} ms");
      Console.WriteLine($"   Memory:     {memoryUsedMB:F2} MB");
      Console.WriteLine($"   GC Gen0:    {gcAfter0 - gcBefore0}");
      Console.WriteLine($"   GC Gen1:    {gcAfter1 - gcBefore1}");
      Console.WriteLine($"   GC Gen2:    {gcAfter2 - gcBefore2}");
      Console.WriteLine($"   Verified:   {deserialized?.Count == recordCount}");
      Console.WriteLine();
   }

   private void RunMemoryStressTest()
   {
      Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine("PHASE 5: MEMORY STRESS TEST (10M Records)");
      Console.WriteLine("═══════════════════════════════════════════════════════════════════════\n");

      Console.WriteLine("⚠️  WARNING: This test allocates several GB of memory!");
      Console.WriteLine("   Press Enter to continue or Ctrl+C to skip...");
      Console.ReadLine();

      BenchmarkExtreme(10_000_000, "10M");
   }

   private void BenchmarkExtreme(int recordCount, string label)
   {
      Console.WriteLine($"💪 Extreme Stress Test ({label} records):");
      Console.WriteLine($"   Generating {recordCount:N0} objects...");

      List<SimpleObject> objects = GenerateSimpleObjects(recordCount);
      var converter = new AjisConverter<List<SimpleObject>>();

      Console.WriteLine($"   ✓ Generated {recordCount:N0} objects");
      Console.WriteLine($"   Memory before: {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F2} MB");
      Console.WriteLine();

      // Serialize
      Console.WriteLine("   📤 Serializing...");
      GC.Collect();
      var baselineMemory = GC.GetTotalMemory(false);
      var gcBefore0 = GC.CollectionCount(0);
      var gcBefore1 = GC.CollectionCount(1);
      var gcBefore2 = GC.CollectionCount(2);

      var sw = Stopwatch.StartNew();
      var json = converter.Serialize(objects);
      sw.Stop();

      var serializeTime = sw.ElapsedMilliseconds;
      var bytes = Encoding.UTF8.GetByteCount(json);
      var peakMemory = GC.GetTotalMemory(false);

      Console.WriteLine($"      Time:   {serializeTime:N0} ms");
      Console.WriteLine($"      Output: {bytes / 1024.0 / 1024.0:F2} MB");
      Console.WriteLine($"      Memory: {(peakMemory - baselineMemory) / 1024.0 / 1024.0:F2} MB");
      Console.WriteLine();

      // Deserialize
      Console.WriteLine("   📥 Deserializing...");
      GC.Collect();
      baselineMemory = GC.GetTotalMemory(false);

      sw = Stopwatch.StartNew();
      List<SimpleObject>? deserialized = converter.Deserialize(json);
      sw.Stop();

      var deserializeTime = sw.ElapsedMilliseconds;
      peakMemory = GC.GetTotalMemory(false);
      var gcAfter0 = GC.CollectionCount(0);
      var gcAfter1 = GC.CollectionCount(1);
      var gcAfter2 = GC.CollectionCount(2);

      Console.WriteLine($"      Time:   {deserializeTime:N0} ms");
      Console.WriteLine($"      Memory: {(peakMemory - baselineMemory) / 1024.0 / 1024.0:F2} MB");
      Console.WriteLine($"      GC:     Gen0={gcAfter0 - gcBefore0}, Gen1={gcAfter1 - gcBefore1}, Gen2={gcAfter2 - gcBefore2}");
      Console.WriteLine($"      Valid:  {deserialized?.Count == recordCount}");
      Console.WriteLine();

      Console.WriteLine($"   🏁 TOTAL:");
      Console.WriteLine($"      Time:   {serializeTime + deserializeTime:N0} ms");
      Console.WriteLine($"      Memory: {bytes / 1024.0 / 1024.0:F2} MB output");
      Console.WriteLine();
   }

   // Helper generators
   private List<int> GenerateNumberArray(int count)
   {
      return [.. Enumerable.Range(1, count)];
   }

   private List<string> GenerateStringArray(int count)
   {
      return [.. Enumerable.Range(1, count).Select(i => $"String value {i}")];
   }

   private List<SimpleObject> GenerateSimpleObjects(int count)
   {
      return [.. Enumerable.Range(1, count)
            .Select(i => new SimpleObject
            {
                Id = i,
                Name = $"Object {i}",
                Value = i * 1.5,
                Active = i % 2 == 0
            })];
   }

   private sealed class SimpleObject
   {
      public int Id { get; set; }
      public string Name { get; set; } = "";
      public double Value { get; set; }
      public bool Active { get; set; }
   }
}