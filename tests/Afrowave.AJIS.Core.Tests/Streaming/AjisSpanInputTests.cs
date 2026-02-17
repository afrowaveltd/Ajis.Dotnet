#nullable enable

using Afrowave.AJIS.Streaming.Walk.Input;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSpanInputTests
{
   [Fact]
   public void SpanInput_ExposesLengthAndSpan()
   {
      byte[] bytes = "abc"u8.ToArray();
      AjisSpanInput input = new AjisSpanInput(bytes);

      Assert.Equal(bytes.Length, input.LengthBytes);

      Assert.True(input.TryGetUtf8Span(out var span));
      Assert.True(span.SequenceEqual(bytes));
   }

   [Fact]
   public void SpanInput_OpenStream_ReturnsNull()
   {
      AjisSpanInput input = new AjisSpanInput(ReadOnlyMemory<byte>.Empty);
      Assert.Null(input.OpenStream());
   }
}