#nullable enable

using Afrowave.AJIS.Core.Localization;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class AjisLocDictionaryTests
{
   [Fact]
   public void TryGet_ReturnsExpectedValues()
   {
      AjisLocDictionary dict = new AjisLocDictionary(new Dictionary<string, string>
      {
         ["k"] = "v"
      });

      Assert.True(dict.TryGet("k", out var value));
      Assert.Equal("v", value);
      Assert.False(dict.TryGet("missing", out _));
   }

   [Fact]
   public void Entries_ReturnsBackingDictionary()
   {
      Dictionary<string, string> source = new Dictionary<string, string> { ["k"] = "v" };
      AjisLocDictionary dict = new AjisLocDictionary(source);

      Assert.Same(source, dict.Entries);
   }
}