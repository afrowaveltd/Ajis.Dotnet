#nullable enable

using Afrowave.AJIS.Serialization;
using Afrowave.AJIS.Streaming.Segments;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializeTests
{
   [Fact]
   public void ToText_ThrowsNotImplemented()
   {
      var segments = Array.Empty<AjisSegment>();
      Assert.Throws<NotImplementedException>(() => AjisSerialize.ToText(segments));
   }

   [Fact]
   public async Task ToStreamAsync_ThrowsNotImplemented()
   {
      await using var stream = new MemoryStream();
      var segments = AsyncEnumerable.Empty<AjisSegment>();

      await Assert.ThrowsAsync<NotImplementedException>(() => AjisSerialize.ToStreamAsync(stream, segments));
   }
}
