#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Afrowave.AJIS.Streaming.Walk.Engines;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.StreamWalk;

public sealed class EngineRegistryContractTests
{
   [Fact]
   public void Registry_MustContain_AtLeastOneEngine()
   {
      Assert.NotEmpty(AjisStreamWalkEngineRegistry.All);
   }

   [Fact]
   public void Registry_EnginesMustDeclareCapabilities_AndIds()
   {
      foreach(IAjisStreamWalkEngineDescriptor d in AjisStreamWalkEngineRegistry.All)
      {
         Assert.False(string.IsNullOrWhiteSpace(d.EngineId));
         Assert.NotEqual(AjisEngineCapabilities.None, d.Capabilities);
      }
   }

   [Fact]
   public void Registry_AtLeastOneEngine_MustSupport_DefaultM1Options()
   {
      AjisStreamWalkOptions options = AjisStreamWalkOptions.DefaultForM1 with
      {
         Mode = AjisStreamWalkMode.Ajis
      };

      bool any = AjisStreamWalkEngineRegistry.All.Any(d => d.Supports(options));
      Assert.True(any);
   }
}
