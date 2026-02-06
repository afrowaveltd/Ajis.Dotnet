#nullable enable

namespace Afrowave.AJIS.Serialization.Engines;

/// <summary>
/// Serialization engine capability flags.
/// </summary>
[Flags]
public enum AjisSerializationEngineCapabilities
{
   /// <summary>
   /// No declared capabilities.
   /// </summary>
   None = 0,

   /// <summary>
   /// Streaming output support.
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
