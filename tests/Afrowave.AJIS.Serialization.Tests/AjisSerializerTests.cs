#nullable enable

using Afrowave.AJIS.Serialization;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializerTests
{
   [Fact]
   public void Serialize_ThrowsNotImplemented()
   {
      var value = AjisValue.Null();
      using var stream = new MemoryStream();

      Assert.Throws<NotImplementedException>(() => AjisSerializer.Serialize(stream, value));
   }

   [Fact]
   public async Task SerializeAsync_ThrowsNotImplemented()
   {
      var value = AjisValue.Null();
      await using var stream = new MemoryStream();

      await Assert.ThrowsAsync<NotImplementedException>(() => AjisSerializer.SerializeAsync(stream, value).AsTask());
   }

   [Fact]
   public void SerializeToUtf8Bytes_ThrowsNotImplemented()
   {
      var value = AjisValue.Null();
      Assert.Throws<NotImplementedException>(() => AjisSerializer.SerializeToUtf8Bytes(value));
   }
}
