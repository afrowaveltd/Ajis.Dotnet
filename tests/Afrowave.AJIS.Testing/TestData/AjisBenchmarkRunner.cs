#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Segments;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.Json;

namespace Afrowave.AJIS.Testing.TestData;

public sealed record AjisBenchmarkResult(
   string Name,
   TimeSpan Elapsed,
   long MemoryDeltaBytes,
   int SegmentCount);

public static class AjisBenchmarkRunner
{
   public static async Task<IReadOnlyList<AjisBenchmarkResult>> RunAsync(string path, CancellationToken ct = default)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      if(!File.Exists(path)) throw new FileNotFoundException("Input file not found.", path);

      byte[] bytes = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
      string json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);

      List<AjisBenchmarkResult> results = new List<AjisBenchmarkResult>
      {
         RunAjisBytes("AJIS.Universal", bytes, AjisProcessingProfile.Universal),
         RunAjisBytes("AJIS.HighThroughput", bytes, AjisProcessingProfile.HighThroughput),
         await RunAjisStreamAsync("AJIS.LowMemory", path, ct).ConfigureAwait(false),
         RunSystemTextJson(bytes),
         RunNewtonsoftJson(json)
      };

      return results;
   }

   private static AjisBenchmarkResult RunAjisBytes(string name, byte[] bytes, AjisProcessingProfile profile)
   {
      AjisSettings settings = new AjisSettings { ParserProfile = profile };

      return Measure(name, () =>
      {
         int count = 0;
         foreach(var _ in AjisParse.ParseSegments(bytes, settings))
            count++;
         return count;
      });
   }

   private static async Task<AjisBenchmarkResult> RunAjisStreamAsync(string name, string path, CancellationToken ct)
   {
      AjisSettings settings = new AjisSettings { ParserProfile = AjisProcessingProfile.LowMemory };

      return await MeasureAsync(name, async () =>
      {
         int count = 0;
         await using var stream = File.OpenRead(path);
         await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings, ct))
            count++;
         return count;
      }).ConfigureAwait(false);
   }

   private static AjisBenchmarkResult RunSystemTextJson(byte[] bytes)
      => Measure("System.Text.Json", () =>
      {
         using JsonDocument doc = JsonDocument.Parse(bytes);
         return doc.RootElement.GetRawText().Length;
      });

   private static AjisBenchmarkResult RunNewtonsoftJson(string json)
      => Measure("Newtonsoft.Json", () =>
      {
         JToken token = JToken.Parse(json);
         return token.ToString().Length;
      });

   private static AjisBenchmarkResult Measure(string name, Func<int> action)
   {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      long before = GC.GetTotalMemory(true);
      Stopwatch sw = Stopwatch.StartNew();
      int count = action();
      sw.Stop();
      long after = GC.GetTotalMemory(true);

      return new AjisBenchmarkResult(name, sw.Elapsed, after - before, count);
   }

   private static async Task<AjisBenchmarkResult> MeasureAsync(string name, Func<Task<int>> action)
   {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();

      long before = GC.GetTotalMemory(true);
      Stopwatch sw = Stopwatch.StartNew();
      int count = await action().ConfigureAwait(false);
      sw.Stop();
      long after = GC.GetTotalMemory(true);

      return new AjisBenchmarkResult(name, sw.Elapsed, after - before, count);
   }
}