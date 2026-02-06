#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Afrowave.AJIS.Streaming.Walk.Engines;
using Xunit;

namespace Afrowave.AJIS.Testing.StreamWalk;

public sealed class EngineCostContractTests
{
   [Fact]
   public void EngineDescriptors_MustProvideReasonableCostEstimates()
   {
      AjisStreamWalkOptions options = AjisStreamWalkOptions.DefaultForM1 with
      {
         Mode = AjisStreamWalkMode.Ajis
      };

      AjisStreamWalkRunnerOptions runnerOptions = new()
      {
         LargePayloadThresholdBytes = 1024
      };

      foreach(IAjisStreamWalkEngineDescriptor d in AjisStreamWalkEngineRegistry.All)
      {
         AjisEngineCost costSmall = d.EstimateCost("{}"u8, options, runnerOptions);
         Assert.True(costSmall.EstimatedPasses >= 1);
         Assert.True(costSmall.EstimatedMemoryBytes > 0);

         AjisEngineCost costLarge = d.EstimateCost(new byte[4096], options, runnerOptions);
         Assert.True(costLarge.EstimatedPasses >= 1);
         Assert.True(costLarge.EstimatedMemoryBytes > 0);
      }
   }
}
