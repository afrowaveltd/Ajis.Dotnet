#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization.Engines;

/// <summary>
/// Describes a serialization engine and its capabilities.
/// </summary>
public sealed record AjisSerializationEngineDescriptor(
   string EngineId,
   AjisSerializationEngineKind Kind,
   AjisSerializationEngineCapabilities Capabilities,
   AjisProcessingProfile Profile)
{
   /// <summary>
   /// Determines whether this descriptor is a candidate for the provided profile.
   /// </summary>
   public bool Supports(AjisProcessingProfile profile)
      => Profile == profile || Profile == AjisProcessingProfile.Universal;
}
