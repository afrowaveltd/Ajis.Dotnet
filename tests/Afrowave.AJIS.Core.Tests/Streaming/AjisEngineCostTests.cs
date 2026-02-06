#nullable enable

using Afrowave.AJIS.Streaming.Walk.Engines;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisEngineCostTests
{
   [Fact]
   public void Score_PenalizesRandomAccess()
   {
      var baseCost = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 100, RequiresRandomAccess: false);
      var randomCost = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 100, RequiresRandomAccess: true);

      Assert.True(randomCost.Score > baseCost.Score);
   }

   [Fact]
   public void Score_WeightsPassesHeavily()
   {
      var onePass = new AjisEngineCost(EstimatedPasses: 1, EstimatedMemoryBytes: 0, RequiresRandomAccess: false);
      var twoPass = new AjisEngineCost(EstimatedPasses: 2, EstimatedMemoryBytes: 0, RequiresRandomAccess: false);

      Assert.True(twoPass.Score > onePass.Score);
   }
}
