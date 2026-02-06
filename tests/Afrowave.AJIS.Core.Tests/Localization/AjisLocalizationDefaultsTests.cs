#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using Afrowave.AJIS.Core.Localization;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class AjisLocalizationDefaultsTests
{
   [Fact]
   public async Task BuildDefaultAsync_PrioritizesUserOverrides()
   {
      var overrides = new AjisLocDictionary(new Dictionary<string, string>
      {
         ["ajis.error.unknown"] = "override"
      });

      var provider = await AjisLocalizationDefaults.BuildDefaultAsync(overrides, MissingKeyBehavior.ReturnKey);

      Assert.Equal("override", provider.GetText("ajis.error.unknown"));
   }

   [Fact]
   public void GetDefaultCulture_UsesCurrentUICulture()
   {
      Assert.Equal(System.Globalization.CultureInfo.CurrentUICulture, AjisLocalizationDefaults.GetDefaultCulture());
   }
}
