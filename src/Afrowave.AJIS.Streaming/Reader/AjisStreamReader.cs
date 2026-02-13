#nullable enable

namespace Afrowave.AJIS.Streaming.Reader;

public sealed class AjisStreamReader : IAjisReader, IDisposable
{
   private readonly Stream _stream;
   private byte[] _buffer;
   private int _start;
   private int _end;
   private long _offset;
   private int _line = 1;
   private int _column = 1;
   private bool _previousWasCR = false;
   private bool _eof;

   public AjisStreamReader(Stream stream, int bufferSize = 4096)
   {
      _stream = stream ?? throw new ArgumentNullException(nameof(stream));
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
      _buffer = new byte[bufferSize];
   }

   public long Offset => _offset;
   public int Line => _line;
   public int Column => _column;
   public bool EndOfInput
   {
      get
      {
         if(_start >= _end && !_eof)
         {
            Compact();
            int read = _stream.Read(_buffer, _end, _buffer.Length - _end);
            if(read == 0)
               _eof = true;
            else
               _end += read;
         }

         return _eof && _start >= _end;
      }
   }

   public byte Peek()
   {
      EnsureAvailable(1);
      return _buffer[_start];
   }

   public byte Read()
   {
      byte value = Peek();
      _start++;
      _offset++;
      AdvancePosition(value);
      return value;
   }

   public ReadOnlySpan<byte> ReadSpan(int length)
   {
      ArgumentOutOfRangeException.ThrowIfNegative(length);
      EnsureAvailable(length);

      var span = new ReadOnlySpan<byte>(_buffer, _start, length);
      _start += length;
      _offset += length;
      for(int i = 0; i < span.Length; i++)
         AdvancePosition(span[i]);
      return span;
   }

   private void EnsureAvailable(int length)
   {
      if(length <= _end - _start) return;
      if(_eof) throw new InvalidOperationException("End of input.");

      if(length > _buffer.Length)
         Array.Resize(ref _buffer, length);

      Compact();

      while(length > _end - _start && !_eof)
      {
         int read = _stream.Read(_buffer, _end, _buffer.Length - _end);
         if(read == 0)
         {
            _eof = true;
            break;
         }
         _end += read;
      }

      if(length > _end - _start)
         throw new InvalidOperationException("Not enough data in input.");
   }

   private void AdvancePosition(byte value)
   {
      if(value == '\n')
      {
         if(!_previousWasCR)
         {
            _line++;
         }
         _column = 1;
         _previousWasCR = false;
      }
      else if(value == '\r')
      {
         _line++;
         _column = 1;
         _previousWasCR = true;
      }
      else if((value & 0b1100_0000) != 0b1000_0000)
      {
         _column++;
         _previousWasCR = false;
      }
   }

   private void Compact()
   {
      if(_start == 0) return;
      if(_start >= _end)
      {
         _start = 0;
         _end = 0;
         return;
      }

      Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
      _end -= _start;
      _start = 0;
   }

   public void Dispose() => _stream.Dispose();
}
