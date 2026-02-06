#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Streaming.Segments.Engines;

/// <summary>
/// Describes a segment parsing engine.
/// </summary>
public sealed record AjisSegmentParseEngineDescriptor(
   string EngineId,
   AjisSegmentParseEngineKind Kind,
   AjisSegmentParseEngineCapabilities Capabilities,
   AjisProcessingProfile Profile)
{
   /// <summary>
   /// Determines whether this descriptor supports the requested profile.
   /// </summary>
   public bool Supports(AjisProcessingProfile profile)
      => Profile == profile || Profile == AjisProcessingProfile.Universal;
}
