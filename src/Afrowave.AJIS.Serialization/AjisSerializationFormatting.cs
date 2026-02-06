#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization;

internal static class AjisSerializationFormatting
{
   public static bool UseCompact(AjisSettings? settings)
      => settings?.Serialization?.Canonicalize == true
         || settings?.Serialization?.Compact == true
         || settings is null;
}
