#nullable enable

using Afrowave.AJIS.Streaming.Reader;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Reader;

public sealed class AjisReaderTests
{
   [Fact]
   public void SpanReader_ReadsBytesInOrder()
   {
      var reader = new AjisSpanReader("abc"u8.ToArray());

      Assert.Equal((byte)'a', reader.Peek());
      Assert.Equal((byte)'a', reader.Read());
      Assert.Equal((byte)'b', reader.Read());
      Assert.Equal((byte)'c', reader.Read());
      Assert.True(reader.EndOfInput);
      Assert.Equal(1, reader.Line);
      Assert.Equal(4, reader.Column);
   }

   [Fact]
   public void StreamReader_ReadsBytesInOrder()
   {
      using var stream = new MemoryStream("abc"u8.ToArray());
      using var reader = new AjisStreamReader(stream, bufferSize: 2);

      Assert.Equal((byte)'a', reader.Peek());
      Assert.Equal((byte)'a', reader.Read());
      Assert.Equal((byte)'b', reader.Read());
      Assert.Equal((byte)'c', reader.Read());
      Assert.True(reader.EndOfInput);
   }

   [Fact]
   public void StreamReader_ReadSpanAcrossRefill()
   {
      using var stream = new MemoryStream("abcdef"u8.ToArray());
      using var reader = new AjisStreamReader(stream, bufferSize: 3);

      var span = reader.ReadSpan(5);
      Assert.Equal("abcde"u8.ToArray(), span.ToArray());
      Assert.Equal(5, reader.Offset);
      Assert.Equal(1, reader.Line);
      Assert.Equal(6, reader.Column);
   }

   [Fact]
   public void Reader_TracksNewlines()
   {
      var reader = new AjisSpanReader("a\nb"u8.ToArray());
      reader.Read();
      reader.Read();
      reader.Read();

      Assert.Equal(2, reader.Line);
      Assert.Equal(2, reader.Column);
   }
}
