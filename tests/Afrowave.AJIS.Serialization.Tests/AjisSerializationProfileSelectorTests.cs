#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Serialization;
using Xunit;

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
      var settings = new AjisSettings { SerializerProfile = AjisProcessingProfile.HighThroughput };

      Assert.Equal(AjisProcessingProfile.HighThroughput, AjisSerializationProfileSelector.Select(settings));
   }
}
