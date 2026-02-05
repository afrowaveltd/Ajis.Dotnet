#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

public static class AjisStreamWalkEngineRegistry
{
   // Keep deterministic order.
   public static readonly IAjisStreamWalkEngineDescriptor[] s_all =
   [
      new M1EngineDescriptor(),
      new LowMemEngineDescriptor()
   ];

   public static IReadOnlyList<IAjisStreamWalkEngineDescriptor> All { get; } = s_all;

   public sealed class M1EngineDescriptor : IAjisStreamWalkEngineDescriptor
   {
      public static readonly IAjisStreamWalkEngine s_engine = new AjisStreamWalkEngineM1();

      public string EngineId => AjisStreamWalkEngineIds.M1_Span_Serial;
      public AjisStreamWalkEngineKind Kind => AjisStreamWalkEngineKind.Serial;
      public AjisEngineCapabilities Capabilities => AjisEngineCapabilities.Streaming | AjisEngineCapabilities.HighThroughput;

      public bool Supports(AjisStreamWalkOptions options)
         => options.Mode != AjisStreamWalkMode.Lax; // M1 rejects LAX.

      public AjisEngineCost EstimateCost(
   ReadOnlySpan<byte> inputUtf8,
   AjisStreamWalkOptions options,
   AjisStreamWalkRunnerOptions runnerOptions)
      {
         // Single-pass, low allocation, optimized for throughput.
         // Memory is approximate (we don't want exact accounting here).
         long mem = Math.Max(4_096, inputUtf8.Length / 16);

         // Penalize big payloads (balanced mode tends to prefer LowMem for huge inputs).
         if(inputUtf8.Length >= runnerOptions.LargePayloadThresholdBytes)
            mem *= 4;

         return new AjisEngineCost(
            EstimatedPasses: 1,
            EstimatedMemoryBytes: mem,
            RequiresRandomAccess: false);
      }

      public IAjisStreamWalkEngine CreateEngine() => s_engine;
   }

   public sealed class LowMemEngineDescriptor : IAjisStreamWalkEngineDescriptor
   {
      public static readonly IAjisStreamWalkEngine s_engine = new AjisStreamWalkEngineLowMem();

      public string EngineId => AjisStreamWalkEngineIds.LowMem_Span_Serial;
      public AjisStreamWalkEngineKind Kind => AjisStreamWalkEngineKind.Serial;
      public AjisEngineCapabilities Capabilities => AjisEngineCapabilities.Streaming | AjisEngineCapabilities.LowMemory;

      public bool Supports(AjisStreamWalkOptions options)
         => options.Mode != AjisStreamWalkMode.Lax;

      public AjisEngineCost EstimateCost(
   ReadOnlySpan<byte> inputUtf8,
   AjisStreamWalkOptions options,
   AjisStreamWalkRunnerOptions runnerOptions)
      {
         // Single-pass, minimal allocations.
         // Fixed-ish memory behavior.
         long mem = 16_384;

         // Slightly increase for tiny payloads to avoid always "winning".
         if(inputUtf8.Length < 256)
            mem *= 2;

         return new AjisEngineCost(
            EstimatedPasses: 1,
            EstimatedMemoryBytes: mem,
            RequiresRandomAccess: false);
      }


      public IAjisStreamWalkEngine CreateEngine() => s_engine;
   }
}
