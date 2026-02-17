#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializationProfileSelectorTests
{
   [Fact]
   public void Select_DefaultsToUniversal()
   {
      Assert.Equal(AjisProcessingProfile.Universal, AjisSerializationProfileSelector.Select(null));
   }

   [Fact]
   public void Select_UsesSettingsProfile()
   {
      AjisSettings settings = new AjisSettings { SerializerProfile = AjisProcessingProfile.HighThroughput };

      Assert.Equal(AjisProcessingProfile.HighThroughput, AjisSerializationProfileSelector.Select(settings));
   }
}