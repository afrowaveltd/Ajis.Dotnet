#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Serialization;

internal static class AjisSerializationFormatting
{
   public static AjisSerializationFormattingOptions GetOptions(AjisSettings? settings)
   {
      if(settings is null)
         return new AjisSerializationFormattingOptions(true, false, 2, false);

      bool canonical = settings.Serialization?.Canonicalize == true;
      bool compact = canonical || settings.Serialization?.Compact == true;
      bool pretty = !compact && settings.Serialization?.Pretty == true;
      int indentSize = settings.Serialization?.IndentSize ?? 2;

      if(indentSize <= 0)
         indentSize = 2;

      return new AjisSerializationFormattingOptions(compact, pretty, indentSize, canonical);
   }
}

internal readonly record struct AjisSerializationFormattingOptions(bool Compact, bool Pretty, int IndentSize, bool Canonicalize);
