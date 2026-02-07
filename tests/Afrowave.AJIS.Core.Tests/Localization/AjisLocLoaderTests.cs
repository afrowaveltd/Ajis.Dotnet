#nullable enable

using Afrowave.AJIS.Core.Localization;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class AjisLocLoaderTests
{
   [Fact]
   public async Task LoadAsync_ParsesValidLines()
   {
      const string text = "# comment\n\"k1\":\"v1\"\n\"k2\":\"v2\"\n";
      await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

      var dict = await AjisLocLoader.LoadAsync(stream, TestContext.Current.CancellationToken);

      Assert.True(dict.TryGet("k1", out var v1));
      Assert.Equal("v1", v1);
      Assert.True(dict.TryGet("k2", out var v2));
      Assert.Equal("v2", v2);
   }
}
