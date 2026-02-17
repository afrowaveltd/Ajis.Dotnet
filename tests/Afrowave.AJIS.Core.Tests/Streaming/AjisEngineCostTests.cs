#nullable enable

using Afrowave.AJIS.Streaming.Walk.Engines;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisEngineCostTests
{
   [Fact]
   public void Score_PenalizesRandomAccess()
   {
      AjisEngineCost baseCost = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 100, RequiresRandomAccess: false);
      AjisEngineCost randomCost = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 100, RequiresRandomAccess: true);

      Assert.True(randomCost.Score > baseCost.Score);
   }

   [Fact]
   public void Score_WeightsPassesHeavily()
   {
      AjisEngineCost onePass = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 0, RequiresRandomAccess: false);
      AjisEngineCost twoPass = new AjisEngineCost(EstimatedPasses: 2, EstimatedMemoryBytes: 0, RequiresRandomAccess: false);

      Assert.True(twoPass.Score > onePass.Score);
   }
}