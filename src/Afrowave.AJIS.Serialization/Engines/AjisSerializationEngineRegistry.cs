#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization.Engines;

/// <summary>
/// Registry of available serialization engines.
/// </summary>
public static class AjisSerializationEngineRegistry
{
   private static readonly AjisSerializationEngineDescriptor[] s_all =
   [
      new(
         AjisSerializationEngineIds.Universal,
         AjisSerializationEngineKind.Serial,
         AjisSerializationEngineCapabilities.Streaming,
         AjisProcessingProfile.Universal),
      new(
         AjisSerializationEngineIds.LowMemory,
         AjisSerializationEngineKind.Serial,
         AjisSerializationEngineCapabilities.Streaming | AjisSerializationEngineCapabilities.LowMemory,
         AjisProcessingProfile.LowMemory),
      new(
         AjisSerializationEngineIds.HighThroughput,
         AjisSerializationEngineKind.Serial,
         AjisSerializationEngineCapabilities.Streaming | AjisSerializationEngineCapabilities.HighThroughput,
         AjisProcessingProfile.HighThroughput)
   ];

   /// <summary>
   /// Gets the available serialization engine descriptors.
   /// </summary>
   public static IReadOnlyList<AjisSerializationEngineDescriptor> All { get; } = s_all;
}
