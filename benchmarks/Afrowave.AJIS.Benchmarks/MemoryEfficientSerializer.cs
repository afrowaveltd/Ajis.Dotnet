#nullable enable

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Memory-efficient serializer optimized for minimal memory allocations.
/// Uses ArrayPool for buffer reuse and optimized string creation.
/// Target: Match STJ memory usage (~414 MB for 1M objects) while maintaining speed.
/// </summary>
internal sealed class MemoryEfficientSerializer
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    private const int InitialBufferSize = 4 * 1024; // Start small - 4KB
    private const int MaxBufferSize = 1024 * 1024; // Max 1MB before resize

    /// <summary>
    /// Serialize list to JSON string with minimal memory allocations.
    /// Strategy:
    /// 1. Use ArrayPool for reusable buffers
    /// 2. Grow buffer only when needed
    /// 3. Use string.Create for zero-copy conversion
    /// 4. Return buffer to pool immediately after use
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Serialize(List<TestObject> list)
    {
        // Rent initial buffer from pool
        byte[] buffer = BytePool.Rent(InitialBufferSize);

        // Use ArrayBufferWriter backed by pooled array for dynamic growth
        using var bufferWriter = new PooledArrayBufferWriter(buffer, BytePool);
        
        using (var writer = new Utf8JsonWriter(bufferWriter, new JsonWriterOptions 
        { 
            Indented = false,
            SkipValidation = false // Keep validation for correctness
        }))
        {
            writer.WriteStartArray();
            foreach (var obj in list)
            {
                SerializeObject(writer, obj);
            }
            writer.WriteEndArray();
            writer.Flush();
        }

        // Create string directly from UTF8 bytes
        // Buffer will be returned to pool when bufferWriter disposes
        return CreateStringFromUtf8(bufferWriter.GetWrittenSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeObject(Utf8JsonWriter writer, TestObject obj)
    {
        writer.WriteStartObject();

        writer.WriteNumber("Id", obj.Id);
        writer.WriteString("Name", obj.Name);
        writer.WriteNumber("Value", obj.Value);
        writer.WriteNumber("Score", obj.Score);
        writer.WriteBoolean("Active", obj.Active);

        if (obj.Tags != null)
        {
            writer.WriteStartArray("Tags");
            foreach (var tag in obj.Tags)
            {
                writer.WriteStringValue(tag);
            }
            writer.WriteEndArray();
        }

        if (obj.Items != null)
        {
            writer.WriteStartArray("Items");
            foreach (var item in obj.Items)
            {
                SerializeTestItem(writer, item);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeTestItem(Utf8JsonWriter writer, TestItem item)
    {
        writer.WriteStartObject();
        writer.WriteNumber("ItemId", item.ItemId);
        writer.WriteString("ItemName", item.ItemName);
        writer.WriteNumber("Amount", item.Amount);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Create string from UTF8 bytes with minimal allocations.
    /// Uses Encoding.UTF8.GetString which is optimized in modern .NET.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string CreateStringFromUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        // In .NET 6+, GetString is optimized for spans
        return Encoding.UTF8.GetString(utf8Bytes);
    }

    /// <summary>
    /// Custom IBufferWriter implementation backed by ArrayPool.
    /// Grows buffer efficiently and returns to pool on dispose.
    /// </summary>
    private sealed class PooledArrayBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _buffer;
        private int _index;
        private readonly ArrayPool<byte> _pool;

        public PooledArrayBufferWriter(byte[] initialBuffer, ArrayPool<byte> pool)
        {
            _buffer = initialBuffer;
            _index = 0;
            _pool = pool;
        }

        public int WrittenCount => _index;

        public ReadOnlySpan<byte> GetWrittenSpan() => _buffer.AsSpan(0, _index);

        public void Advance(int count)
        {
            if (count < 0 || _index + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            
            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            int availableSpace = _buffer.Length - _index;
            
            if (availableSpace < sizeHint || (sizeHint == 0 && availableSpace < 256))
            {
                // Need to grow buffer
                int newSize = Math.Max(_buffer.Length * 2, _buffer.Length + sizeHint);
                
                // Rent new buffer
                byte[] newBuffer = _pool.Rent(newSize);
                
                // Copy existing data
                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _index);
                
                // Return old buffer to pool
                _pool.Return(_buffer, clearArray: false);
                
                _buffer = newBuffer;
            }
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                _pool.Return(_buffer, clearArray: false);
                _buffer = null!;
            }
        }
    }
}
