#nullable enable

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Capability flags for segment parsing engines.
/// </summary>
[Flags]
public enum AjisSegmentParseEngineCapabilities
{
   /// <summary>
   /// No declared capabilities.
   /// </summary>
   None = 0,

   /// <summary>
   /// Supports streaming inputs.
   /// </summary>
   Streaming = 1 << 0,

   /// <summary>
   /// Optimized for low memory usage.
   /// </summary>
   LowMemory = 1 << 1,

   /// <summary>
   /// Optimized for throughput.
   /// </summary>
   HighThroughput = 1 << 2
}
