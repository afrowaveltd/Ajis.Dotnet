#nullable enable

namespace Afrowave.AJIS.Serialization.Engines;

/// <summary>
/// Stable serialization engine identifiers.
/// </summary>
public static class AjisSerializationEngineIds
{
   /// <summary>
   /// Universal serializer (balanced defaults).
   /// </summary>
   public const string Universal = "SER_UNIVERSAL";

   /// <summary>
   /// Low-memory serializer variant.
   /// </summary>
   public const string LowMemory = "SER_LOWMEM";

   /// <summary>
   /// High-throughput serializer variant.
   /// </summary>
   public const string HighThroughput = "SER_FAST";
}
