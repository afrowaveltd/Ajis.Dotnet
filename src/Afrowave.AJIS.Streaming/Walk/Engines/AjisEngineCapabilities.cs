#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Engines;

[Flags]
internal enum AjisEngineCapabilities
{
   None = 0,

   /// <summary>Consumes input sequentially.</summary>
   Streaming = 1 << 0,

   /// <summary>Can use random access (seek/mmap/windows).</summary>
   RandomAccess = 1 << 1,

   /// <summary>Optimized for minimal allocations / small memory footprint.</summary>
   LowMemory = 1 << 2,

   /// <summary>Optimized for throughput.</summary>
   HighThroughput = 1 << 3,

   /// <summary>Understands typed AJIS values (future milestone).</summary>
   TypedValues = 1 << 4,

   /// <summary>Supports ATP binary attachments (future milestone).</summary>
   BinaryATP = 1 << 5
}
