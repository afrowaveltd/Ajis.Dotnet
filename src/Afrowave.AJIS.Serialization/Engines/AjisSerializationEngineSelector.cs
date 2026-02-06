#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization.Engines;

/// <summary>
/// Selects a serialization engine based on processing profile.
/// </summary>
public static class AjisSerializationEngineSelector
{
   /// <summary>
   /// Selects the first matching engine descriptor for the profile.
   /// </summary>
   public static AjisSerializationEngineDescriptor Select(AjisProcessingProfile profile)
   {
      foreach(AjisSerializationEngineDescriptor descriptor in AjisSerializationEngineRegistry.All)
      {
         if(descriptor.Profile == profile)
            return descriptor;
      }

      foreach(AjisSerializationEngineDescriptor descriptor in AjisSerializationEngineRegistry.All)
      {
         if(descriptor.Profile == AjisProcessingProfile.Universal)
            return descriptor;
      }

      return AjisSerializationEngineRegistry.All[0];
   }
}
