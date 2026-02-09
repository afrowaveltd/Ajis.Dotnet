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

   // ===== Multi-byte UTF-8 Position Tracking Tests =====

   [Fact]
   public void Reader_TwoByteCharacter_TrackingCorrect()
   {
      // € (Euro sign) is U+20AC, encoded as E2 82 AC in UTF-8 (3 bytes!)
      byte[] data = [0xE2, 0x82, 0xAC, (byte)'x']; // € + x
      var reader = new AjisSpanReader(data);

      reader.Read(); // Read first byte of €
      Assert.Equal(0, reader.Offset - 1); // Still in character
      
      reader.Read(); // Read second byte
      reader.Read(); // Read third byte of €
      
      Assert.Equal(3, reader.Offset);
      Assert.Equal(1, reader.Line);
      Assert.Equal(2, reader.Column); // Column should be 2 (one character + one position)
   }

   [Fact]
   public void Reader_ThreeByteCharacter_TrackingCorrect()
   {
      // 中 (CJK ideograph) is U+4E2D, encoded as E4 B8 AD in UTF-8 (3 bytes)
      byte[] data = System.Text.Encoding.UTF8.GetBytes("中x");
      var reader = new AjisSpanReader(data);

      // Read all bytes of 中
      reader.Read();
      reader.Read();
      reader.Read();

      Assert.Equal(3, reader.Offset);
      Assert.Equal(1, reader.Column); // Position tracks characters, not bytes
   }

   [Fact]
   public void Reader_FourByteCharacter_TrackingCorrect()
   {
      // 😀 (Emoji) is U+1F600, encoded as F0 9F 98 80 in UTF-8 (4 bytes)
      byte[] data = System.Text.Encoding.UTF8.GetBytes("😀x");
      var reader = new AjisSpanReader(data);

      // Read all bytes of emoji
      reader.Read();
      reader.Read();
      reader.Read();
      reader.Read();

      Assert.Equal(4, reader.Offset);
      Assert.Equal(1, reader.Column);
   }

   [Fact]
   public void Reader_MultipleMultiByteCharacters()
   {
      byte[] data = System.Text.Encoding.UTF8.GetBytes("你好世界"); // Chinese text (4 chars)
      var reader = new AjisSpanReader(data);

      // Read first character 你 (3 bytes)
      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(1, reader.Column);

      // Read second character 好 (3 bytes)
      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(2, reader.Column);

      // Continue...
      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(3, reader.Column);

      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(4, reader.Column);
   }

   [Fact]
   public void Reader_MixedASCIIandMultiByte()
   {
      byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello中world"); // ASCII + Chinese + ASCII
      var reader = new AjisSpanReader(data);

      // Read "Hello" (5 ASCII chars = 5 bytes)
      for (int i = 0; i < 5; i++) reader.Read();
      Assert.Equal(5, reader.Column);
      Assert.Equal(5, reader.Offset);

      // Read 中 (3 bytes)
      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(6, reader.Column);
      Assert.Equal(8, reader.Offset);

      // Read "world" (5 ASCII chars)
      for (int i = 0; i < 5; i++) reader.Read();
      Assert.Equal(11, reader.Column);
      Assert.Equal(13, reader.Offset);
   }

   // ===== Line Ending Normalization Tests =====

   [Fact]
   public void Reader_UnixLineEnding_LF_Tracked()
   {
      var reader = new AjisSpanReader("a\nb\nc"u8.ToArray());

      reader.Read(); // 'a'
      Assert.Equal(1, reader.Line);
      Assert.Equal(2, reader.Column);

      reader.Read(); // '\n'
      Assert.Equal(2, reader.Line);
      Assert.Equal(1, reader.Column);

      reader.Read(); // 'b'
      Assert.Equal(2, reader.Line);
      Assert.Equal(2, reader.Column);

      reader.Read(); // '\n'
      Assert.Equal(3, reader.Line);
      Assert.Equal(1, reader.Column);
   }

   [Fact]
   public void Reader_WindowsLineEnding_CRLF_TrackedAsOneLinebreak()
   {
      var reader = new AjisSpanReader("a\r\nb\r\nc"u8.ToArray());

      reader.Read(); // 'a'
      Assert.Equal(1, reader.Line);

      reader.Read(); // '\r'
      // \r\n should be treated as single newline
      reader.Read(); // '\n'
      Assert.Equal(2, reader.Line);

      reader.Read(); // 'b'
      Assert.Equal(2, reader.Line);
   }

   [Fact]
   public void Reader_OldMacLineEnding_CR_Tracked()
   {
      var reader = new AjisSpanReader("a\rb\rc"u8.ToArray());

      reader.Read(); // 'a'
      Assert.Equal(1, reader.Line);

      reader.Read(); // '\r'
      Assert.Equal(2, reader.Line);

      reader.Read(); // 'b'
      Assert.Equal(2, reader.Line);

      reader.Read(); // '\r'
      Assert.Equal(3, reader.Line);
   }

   [Fact]
   public void Reader_OffsetCorrectAfterMultibyteAndNewline()
   {
      byte[] data = System.Text.Encoding.UTF8.GetBytes("€\n$");
      var reader = new AjisSpanReader(data);

      // Read € (3 bytes)
      reader.Read();
      reader.Read();
      reader.Read();
      Assert.Equal(3, reader.Offset);
      Assert.Equal(1, reader.Column);

      // Read \n
      reader.Read();
      Assert.Equal(4, reader.Offset);
      Assert.Equal(2, reader.Line);
      Assert.Equal(1, reader.Column);

      // Read $
      reader.Read();
      Assert.Equal(5, reader.Offset);
      Assert.Equal(2, reader.Column);
   }

   [Fact]
   public void StreamReader_TrackingCorrectAcrossBufferBoundary()
   {
      byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello\nWorld");
      using var stream = new MemoryStream(data);
      using var reader = new AjisStreamReader(stream, bufferSize: 6);

      // Read "Hello\n" (6 bytes - fits in first buffer)
      for (int i = 0; i < 6; i++) reader.Read();
      Assert.Equal(6, reader.Offset);
      Assert.Equal(2, reader.Line);
      Assert.Equal(1, reader.Column);

      // Continue reading across buffer boundary
      reader.Read(); // 'W'
      Assert.Equal(7, reader.Offset);
      Assert.Equal(2, reader.Column);
   }

   [Fact]
   public void SpanReader_ColumnResetAfterNewline()
   {
      var reader = new AjisSpanReader("abc\ndef\nghi"u8.ToArray());

      // Read "abc\n"
      for (int i = 0; i < 4; i++) reader.Read();
      Assert.Equal(2, reader.Line);
      Assert.Equal(1, reader.Column);

      // Read "def\n"
      for (int i = 0; i < 4; i++) reader.Read();
      Assert.Equal(3, reader.Line);
      Assert.Equal(1, reader.Column);

      // Read "ghi"
      for (int i = 0; i < 3; i++) reader.Read();
      Assert.Equal(3, reader.Line);
      Assert.Equal(4, reader.Column);
    }
}
