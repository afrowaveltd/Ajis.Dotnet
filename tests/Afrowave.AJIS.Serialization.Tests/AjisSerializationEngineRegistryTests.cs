#nullable enable

using Afrowave.AJIS.Serialization.Engines;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializationEngineRegistryTests
{
   [Fact]
   public void Registry_MustContain_AtLeastOneEngine()
   {
      Assert.NotEmpty(AjisSerializationEngineRegistry.All);
   }

   [Fact]
   public void Registry_EnginesMustDeclareCapabilities_AndIds()
   {
      foreach(AjisSerializationEngineDescriptor descriptor in AjisSerializationEngineRegistry.All)
      {
         Assert.False(string.IsNullOrWhiteSpace(descriptor.EngineId));
         Assert.NotEqual(AjisSerializationEngineCapabilities.None, descriptor.Capabilities);
      }
   }
}
