#nullable enable

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisSpanReader : IAjisReader
{
   private readonly ReadOnlyMemory<byte> _memory;
   private int _offset;
   private int _line = 1;
   private int _column = 1;

   public AjisSpanReader(ReadOnlyMemory<byte> memory)
   {
      _memory = memory;
      _offset = 0;
   }

   public long Offset => _offset;
   public int Line => _line;
   public int Column => _column;
   public bool EndOfInput => _offset >= _memory.Length;

   public byte Peek()
   {
      if(EndOfInput) throw new InvalidOperationException("End of input.");
      return _memory.Span[_offset];
   }

   public byte Read()
   {
      byte value = Peek();
      _offset++;
      AdvancePosition(value);
      return value;
   }

   public ReadOnlySpan<byte> ReadSpan(int length)
   {
      if(length < 0) throw new ArgumentOutOfRangeException(nameof(length));
      if(_offset + length > _memory.Length)
         throw new InvalidOperationException("Not enough data in input.");

      var span = _memory.Span.Slice(_offset, length);
      _offset += length;
      for(int i = 0; i < span.Length; i++)
         AdvancePosition(span[i]);
      return span;
   }

   private void AdvancePosition(byte value)
   {
      if(value == '\n')
      {
         _line++;
         _column = 1;
      }
      else if(value == '\r')
      {
         _column = 1;
      }
      else
      {
         _column++;
      }
   }
}
