#nullable enable

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisSpanReader(ReadOnlyMemory<byte> memory) : IAjisReader
{
   private readonly ReadOnlyMemory<byte> _memory = memory;
   private int _offset = 0;
   private int _line = 1;
   private int _column = 1;
   private bool _previousWasCR = false;
   private int _pendingUtf8Continuation = 0;
   private bool _isTwoByteSequence = false;

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
      ArgumentOutOfRangeException.ThrowIfNegative(length);
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
      if(value == (byte)'\n')
      {
         if(!_previousWasCR)
         {
            _line++;
         }
         _column = 1;
         _previousWasCR = false;
         _pendingUtf8Continuation = 0;
         _isTwoByteSequence = false;
         return;
      }

      if(value == (byte)'\r')
      {
         _line++;
         _column = 1;
         _previousWasCR = true;
         _pendingUtf8Continuation = 0;
         _isTwoByteSequence = false;
         return;
      }

      // UTF-8 continuation bytes have top bits 10xxxxxx
      bool isContinuation = (value & 0b1100_0000) == 0b1000_0000;
      if(isContinuation)
      {
         if(_pendingUtf8Continuation > 0)
         {
            _pendingUtf8Continuation--;
            if(_pendingUtf8Continuation == 0)
            {
               // finished a multi-byte character
               if(!_isTwoByteSequence)
               {
                  // finished a multi-byte character (not 2-byte)
                  _column++;
               }
            }
         }
         else
         {
            // stray continuation - do not count as column
         }
      }
      else
      {
         // Start byte - determine sequence length
         if((value & 0b1000_0000) == 0)
         {
            // ASCII
            _column++;
         }
         else if((value & 0b1110_0000) == 0b1100_0000)
         {
            // 2-byte sequence
            _pendingUtf8Continuation = 1;
            _isTwoByteSequence = true;
            _column++; // extra increment for 2-byte
         }
         else if((value & 0b1111_0000) == 0b1110_0000)
         {
            // 3-byte sequence
            _pendingUtf8Continuation = 2;
            _isTwoByteSequence = false;
         }
         else if((value & 0b1111_1000) == 0b1111_0000)
         {
            // 4-byte sequence
            _pendingUtf8Continuation = 3;
            _isTwoByteSequence = false;
         }
         else
         {
            // invalid start byte, count as column
            _column++;
            _isTwoByteSequence = false;
         }
      }

      _previousWasCR = false;
   }
}
