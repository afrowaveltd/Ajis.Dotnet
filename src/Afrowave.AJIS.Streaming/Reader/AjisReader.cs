#nullable enable

namespace Afrowave.AJIS.Streaming.Reader;

public interface IAjisReader
{
   long Offset { get; }
   int Line { get; }
   int Column { get; }
   bool EndOfInput { get; }

   byte Peek();
   byte Read();
   ReadOnlySpan<byte> ReadSpan(int length);
}
