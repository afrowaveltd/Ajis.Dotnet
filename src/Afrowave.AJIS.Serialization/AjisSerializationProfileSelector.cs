#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Resolves serializer processing profile from settings.
/// </summary>
public static class AjisSerializationProfileSelector
{
   /// <summary>
   /// Resolves the serializer processing profile, defaulting to Universal.
   /// </summary>
   public static AjisProcessingProfile Select(AjisSettings? settings)
      => settings?.SerializerProfile ?? AjisProcessingProfile.Universal;
}
