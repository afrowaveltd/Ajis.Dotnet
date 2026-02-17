using Afrowave.AJIS.Serialization.Mapping;
using System.Diagnostics;
using System.Text;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Compares multiple parser implementations to find the fastest approach.
/// Tests: FastDeserializer vs AjisUtf8Parser vs System.Text.Json vs Newtonsoft.
/// </summary>
public sealed class ParserCompetitionBenchmark
{
   public void Run()
   {
      Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
      Console.WriteLine("║              PARSER COMPETITION BENCHMARK                              ║");
      Console.WriteLine("║    Comparing: FastDeserializer vs Old AjisUtf8Parser vs STJ vs NSJ    ║");
      Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
      Console.WriteLine();

      RunComparison(10_000, "10K");
      RunComparison(100_000, "100K");
      RunComparison(1_000_000, "1M");

      Console.WriteLine("\n✓ Parser competition complete!");
   }

   private void RunComparison(int recordCount, string label)
   {
      Console.WriteLine($"\n═══════════════════════════════════════════════════════════════════════");
      Console.WriteLine($"PARSER COMPETITION: {label} RECORDS");
      Console.WriteLine($"═══════════════════════════════════════════════════════════════════════\n");

      // Generate test data
      List<TestObject> testData = GenerateTestData(recordCount);
      var jsonBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(testData));

      Console.WriteLine($"Data size: {jsonBytes.Length / 1024.0:F2} KB");
      Console.WriteLine();

      // Test 1: Current FastDeserializer
      (long time1, long memory1, int gc1) = BenchmarkFastDeserializer(jsonBytes);

      // Test 2: Old AjisUtf8Parser
      (long time2, long memory2, int gc2) = BenchmarkOldUtf8Parser(jsonBytes);

      // Test 3: System.Text.Json
      (long time3, long memory3, int gc3) = BenchmarkSystemTextJson(jsonBytes);

      // Test 4: Newtonsoft.Json
      (long time4, long memory4, int gc4) = BenchmarkNewtonsoftJson(jsonBytes);

      // Print comparison
      PrintComparison(label, time1, time2, time3, time4, memory1, memory2, memory3, memory4, gc1, gc2, gc3, gc4);
   }

   private (long time, long memory, int gc) BenchmarkFastDeserializer(byte[] jsonBytes)
   {
      Console.WriteLine("┌─ FastDeserializer (Current) ──────────────────────────────────┐");

      var json = Encoding.UTF8.GetString(jsonBytes);
      var converter = new AjisConverter<List<TestObject>>();

      // Warmup
      for(int i = 0; i < 3; i++)
      {
         List<TestObject>? _ = converter.Deserialize(json);
      }

      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var baseline = GC.GetTotalMemory(false);
      var gcBefore = GC.CollectionCount(0);

      var sw = Stopwatch.StartNew();
      List<TestObject>? result = converter.Deserialize(json);
      sw.Stop();

      var peak = GC.GetTotalMemory(false);
      var gcAfter = GC.CollectionCount(0);

      var time = sw.ElapsedMilliseconds;
      var memory = (peak - baseline) / 1024 / 1024;
      var gc = gcAfter - gcBefore;

      Console.WriteLine($"   Time:   {time:N0} ms");
      Console.WriteLine($"   Memory: {memory:N0} MB");
      Console.WriteLine($"   GC:     {gc} collections");
      Console.WriteLine($"   Valid:  {result?.Count == jsonBytes.Length / 100}");
      Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

      return (time, memory, gc);
   }

   private (long time, long memory, int gc) BenchmarkOldUtf8Parser(byte[] jsonBytes)
   {
      Console.WriteLine("┌─ AjisUtf8Parser (Old Tools) ──────────────────────────────────┐");
      Console.WriteLine("   ⚠️  SKIPPED - Requires Tools_extracted integration");
      Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

      // TODO: Integrate old AjisUtf8Parser from Tools_extracted
      // For now, return dummy values to allow compilation
      return (long.MaxValue, 0, 0);

      /* DISABLED FOR NOW - needs Tools_extracted project reference
      try
      {
          // Warmup
          for (int i = 0; i < 3; i++)
          {
              var _ = Afrowave.AJIS.AjisUtf8Parser.Parse(jsonBytes);
          }

          GC.Collect();
          GC.WaitForPendingFinalizers();
          GC.Collect();

          var baseline = GC.GetTotalMemory(false);
          var gcBefore = GC.CollectionCount(0);

          var sw = Stopwatch.StartNew();
          var result = Afrowave.AJIS.AjisUtf8Parser.Parse(jsonBytes);
          sw.Stop();

          var peak = GC.GetTotalMemory(false);
          var gcAfter = GC.CollectionCount(0);

          var time = sw.ElapsedMilliseconds;
          var memory = (peak - baseline) / 1024 / 1024;
          var gc = gcAfter - gcBefore;

          Console.WriteLine($"   Time:   {time:N0} ms");
          Console.WriteLine($"   Memory: {memory:N0} MB");
          Console.WriteLine($"   GC:     {gc} collections");
          Console.WriteLine($"   Type:   {result.Type}");
          Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

          return (time, memory, gc);
      }
      catch (Exception ex)
      {
          Console.WriteLine($"   ❌ Error: {ex.Message}");
          Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");
          return (long.MaxValue, 0, 0);
      }
      */
   }

   private (long time, long memory, int gc) BenchmarkSystemTextJson(byte[] jsonBytes)
   {
      Console.WriteLine("┌─ System.Text.Json ────────────────────────────────────────────┐");

      // Warmup
      for(int i = 0; i < 3; i++)
      {
         List<TestObject>? _ = System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(jsonBytes);
      }

      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var baseline = GC.GetTotalMemory(false);
      var gcBefore = GC.CollectionCount(0);

      var sw = Stopwatch.StartNew();
      List<TestObject>? result = System.Text.Json.JsonSerializer.Deserialize<List<TestObject>>(jsonBytes);
      sw.Stop();

      var peak = GC.GetTotalMemory(false);
      var gcAfter = GC.CollectionCount(0);

      var time = sw.ElapsedMilliseconds;
      var memory = (peak - baseline) / 1024 / 1024;
      var gc = gcAfter - gcBefore;

      Console.WriteLine($"   Time:   {time:N0} ms");
      Console.WriteLine($"   Memory: {memory:N0} MB");
      Console.WriteLine($"   GC:     {gc} collections");
      Console.WriteLine($"   Valid:  {result?.Count == jsonBytes.Length / 100}");
      Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

      return (time, memory, gc);
   }

   private (long time, long memory, int gc) BenchmarkNewtonsoftJson(byte[] jsonBytes)
   {
      Console.WriteLine("┌─ Newtonsoft.Json ─────────────────────────────────────────────┐");

      var json = Encoding.UTF8.GetString(jsonBytes);

      // Warmup
      for(int i = 0; i < 3; i++)
      {
         List<TestObject>? _ = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TestObject>>(json);
      }

      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      var baseline = GC.GetTotalMemory(false);
      var gcBefore = GC.CollectionCount(0);

      var sw = Stopwatch.StartNew();
      List<TestObject>? result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TestObject>>(json);
      sw.Stop();

      var peak = GC.GetTotalMemory(false);
      var gcAfter = GC.CollectionCount(0);

      var time = sw.ElapsedMilliseconds;
      var memory = (peak - baseline) / 1024 / 1024;
      var gc = gcAfter - gcBefore;

      Console.WriteLine($"   Time:   {time:N0} ms");
      Console.WriteLine($"   Memory: {memory:N0} MB");
      Console.WriteLine($"   GC:     {gc} collections");
      Console.WriteLine($"   Valid:  {result?.Count == jsonBytes.Length / 100}");
      Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

      return (time, memory, gc);
   }

   private void PrintComparison(string label, long t1, long t2, long t3, long t4,
       long m1, long m2, long m3, long m4, int g1, int g2, int g3, int g4)
   {
      Console.WriteLine($"🏁 COMPETITION RESULTS ({label})");
      Console.WriteLine("─────────────────────────────────────────────────────────────────");

      var times = new[] { t1, t2, t3, t4 };
      var names = new[] { "FastDeserializer", "AjisUtf8Parser", "System.Text.Json", "Newtonsoft.Json" };
      var baseline = times.Min();

      Console.WriteLine("\n⚡ SPEED RANKING:");
      for(int i = 0; i < 4; i++)
      {
         var ratio = times[i] / (double)baseline;
         var medal = i == Array.IndexOf(times, baseline) ? "🥇" :
                     i == 1 ? "🥈" : i == 2 ? "🥉" : "  ";
         Console.WriteLine($"  {medal} {names[i],-20}: {times[i],6:N0} ms  [{ratio:F2}x]");
      }

      Console.WriteLine("\n💾 MEMORY RANKING:");
      var memories = new[] { m1, m2, m3, m4 };
      var memBaseline = memories.Min();
      for(int i = 0; i < 4; i++)
      {
         var ratio = memories[i] / (double)memBaseline;
         var medal = i == Array.IndexOf(memories, memBaseline) ? "🥇" :
                     i == 1 ? "🥈" : i == 2 ? "🥉" : "  ";
         Console.WriteLine($"  {medal} {names[i],-20}: {memories[i],4:N0} MB  [{ratio:F2}x]");
      }

      Console.WriteLine();
   }

   private List<TestObject> GenerateTestData(int count)
   {
      return [.. Enumerable.Range(1, count)
            .Select(i => new TestObject
            {
                Id = i,
                Name = $"Object {i}",
                Value = i * 1.5,
                Active = i % 2 == 0
            })];
   }

   private class TestObject
   {
      public int Id { get; set; }
      public string Name { get; set; } = "";
      public double Value { get; set; }
      public bool Active { get; set; }
   }
}