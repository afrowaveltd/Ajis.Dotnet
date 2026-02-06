#nullable enable

using Afrowave.AJIS.Core.Localization;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class AjisBuiltInLocalesTests
{
   [Fact]
   public async Task LoadEnglishAsync_LoadsEmbeddedLocale()
   {
      var dict = await AjisBuiltInLocales.LoadEnglishAsync();

      Assert.True(dict.TryGet("ajis.error.unknown", out var text));
      Assert.False(string.IsNullOrWhiteSpace(text));
   }
}
