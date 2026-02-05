#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Input;

public readonly struct AjisSpanInput(ReadOnlyMemory<byte> utf8) : IAjisInput
{
   private readonly ReadOnlyMemory<byte> _mem = utf8;

   public long LengthBytes => _mem.Length;

   public bool TryGetUtf8Span(out ReadOnlySpan<byte> utf8)
   {
      utf8 = _mem.Span;
      return true;
   }

   public Stream? OpenStream() => null;
}
