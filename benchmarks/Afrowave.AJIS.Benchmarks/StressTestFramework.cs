#nullable enable

using System.Diagnostics;

namespace Afrowave.AJIS.Benchmarks.StressTest;

/// <summary>
/// Framework for stress testing with memory monitoring and graceful failure handling.
/// </summary>
public sealed class StressTestFramework
{
    /// <summary>
    /// Runs a stress test and returns detailed metrics.
    /// </summary>
    public StressTestResult RunTest(
        string testName,
        Func<string, object> operation,
        string testFilePath)
    {
        Console.WriteLine($"\n┌─ {testName} ─────────────────────────────────────────────┐");

        // Get baseline memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var baselineMemory = GC.GetTotalMemory(false);
        var peakMemory = baselineMemory;
        var peakWorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;

        var sw = Stopwatch.StartNew();
        long gcCollections0 = GC.CollectionCount(0);
        long gcCollections1 = GC.CollectionCount(1);
        long gcCollections2 = GC.CollectionCount(2);

        try
        {
            // Track peak memory during execution
            var memoryTracker = Task.Run(() =>
            {
                var localPeak = peakMemory;
                var localWorkingSetPeak = peakWorkingSet;
                while (sw.IsRunning)
                {
                    localPeak = Math.Max(localPeak, GC.GetTotalMemory(false));
                    localWorkingSetPeak = Math.Max(localWorkingSetPeak,
                        System.Diagnostics.Process.GetCurrentProcess().WorkingSet64);
                    Thread.Sleep(10); // Sample every 10ms
                }
                peakMemory = localPeak;
                peakWorkingSet = localWorkingSetPeak;
            });

            // Run the operation
            var result = operation(testFilePath);

            sw.Stop();

            // Wait for memory tracker to finish
            memoryTracker.Wait();

            // Measure peak memory (now accurate from tracking)
            var gcCollectionsAfter0 = GC.CollectionCount(0);
            var gcCollectionsAfter1 = GC.CollectionCount(1);
            var gcCollectionsAfter2 = GC.CollectionCount(2);

            var fileInfo = new System.IO.FileInfo(testFilePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            var metrics = new StressTestResult
            {
                TestName = testName,
                Success = true,
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                PeakMemoryMB = (peakMemory - baselineMemory) / (1024.0 * 1024.0),
                FileSizeMB = fileSizeMB,
                GCGen0Collections = gcCollectionsAfter0 - gcCollections0,
                GCGen1Collections = gcCollectionsAfter1 - gcCollections1,
                GCGen2Collections = gcCollectionsAfter2 - gcCollections2,
                Timestamp = DateTime.Now,
                PeakWorkingSetMB = peakWorkingSet / (1024.0 * 1024.0)
            };

            PrintResult(metrics);
            return metrics;
        }
        catch (OutOfMemoryException ex)
        {
            sw.Stop();

            var metrics = new StressTestResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"OutOfMemoryException: {ex.Message}",
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                PeakMemoryMB = (GC.GetTotalMemory(false) - baselineMemory) / (1024.0 * 1024.0),
                Timestamp = DateTime.Now
            };

            PrintResult(metrics);
            return metrics;
        }
        catch (Exception ex)
        {
            sw.Stop();

            var metrics = new StressTestResult
            {
                TestName = testName,
                Success = false,
                ErrorMessage = $"{ex.GetType().Name}: {ex.Message}",
                ElapsedMs = sw.Elapsed.TotalMilliseconds,
                Timestamp = DateTime.Now
            };

            PrintResult(metrics);
            return metrics;
        }
    }

    private void PrintResult(StressTestResult result)
    {
        if (result.Success)
        {
            Console.WriteLine($"  ✅ Success");
            Console.WriteLine($"     Time:     {result.ElapsedMs:F2} ms");
            Console.WriteLine($"     Memory:   {result.PeakMemoryMB:F2} MB (managed)");
            Console.WriteLine($"     Peak WS:  {result.PeakWorkingSetMB:F2} MB (process)");
            Console.WriteLine($"     File:     {result.FileSizeMB:F2} MB");
            Console.WriteLine($"     GC Gen0:  {result.GCGen0Collections} collections");
            Console.WriteLine($"     GC Gen1:  {result.GCGen1Collections} collections");
            Console.WriteLine($"     GC Gen2:  {result.GCGen2Collections} collections");

            // Calculate throughput
            if (result.FileSizeMB > 0 && result.ElapsedMs > 0)
            {
                var throughputMBps = result.FileSizeMB / (result.ElapsedMs / 1000.0);
                Console.WriteLine($"     Speed:    {throughputMBps:F2} MB/s");
            }
        }
        else
        {
            Console.WriteLine($"  ❌ Failed");
            Console.WriteLine($"     Error:    {result.ErrorMessage}");
            Console.WriteLine($"     Time:     {result.ElapsedMs:F2} ms");
            if (result.PeakMemoryMB > 0)
                Console.WriteLine($"     Memory:   {result.PeakMemoryMB:F2} MB");
        }

        Console.WriteLine("└─────────────────────────────────────────────────────────┘");
    }
}

/// <summary>
/// Result metrics from a stress test.
/// </summary>
public sealed class StressTestResult
{
    public required string TestName { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required double ElapsedMs { get; init; }
    public double PeakMemoryMB { get; init; }
    public double FileSizeMB { get; init; }
    public long GCGen0Collections { get; init; }
    public long GCGen1Collections { get; init; }
    public long GCGen2Collections { get; init; }
    public required DateTime Timestamp { get; init; }
    public double PeakWorkingSetMB { get; init; }
}
