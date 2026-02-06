#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Serialization.Engines;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializationEngineSelectorTests
{
   [Fact]
   public void Select_Universal_ReturnsUniversal()
   {
      AjisSerializationEngineDescriptor selected = AjisSerializationEngineSelector.Select(AjisProcessingProfile.Universal);

      Assert.Equal(AjisSerializationEngineIds.Universal, selected.EngineId);
   }

   [Fact]
   public void Select_LowMemory_ReturnsLowMemory()
   {
      AjisSerializationEngineDescriptor selected = AjisSerializationEngineSelector.Select(AjisProcessingProfile.LowMemory);

      Assert.Equal(AjisSerializationEngineIds.LowMemory, selected.EngineId);
   }

   [Fact]
   public void Select_HighThroughput_ReturnsHighThroughput()
   {
      AjisSerializationEngineDescriptor selected = AjisSerializationEngineSelector.Select(AjisProcessingProfile.HighThroughput);

      Assert.Equal(AjisSerializationEngineIds.HighThroughput, selected.EngineId);
   }
}
