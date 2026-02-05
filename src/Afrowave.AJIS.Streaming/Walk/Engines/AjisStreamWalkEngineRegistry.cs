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

   public static IReadOnlyList<IAjisStreamWalkEngineDescriptor> All { get; }

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
         // Balanced default: M1 is preferred for smaller payloads.
         if(inputUtf8.Length >= runnerOptions.LargePayloadThresholdBytes)
            return new AjisEngineCost(40, "Large payload prefers LowMem profile.");

         return new AjisEngineCost(10, "Default small payload profile.");
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
         // Balanced: low-mem is preferred for large payloads.
         if(inputUtf8.Length >= runnerOptions.LargePayloadThresholdBytes)
            return new AjisEngineCost(5, "Large payload low-mem profile.");

         return new AjisEngineCost(30, "Small payload prefers M1 profile.");
      }

      public IAjisStreamWalkEngine CreateEngine() => s_engine;
   }
}
