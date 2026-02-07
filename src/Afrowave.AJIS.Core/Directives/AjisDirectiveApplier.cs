#nullable enable

using Afrowave.AJIS.Core;

namespace Afrowave.AJIS.Core.Directives;

/// <summary>
/// Applies directive settings to AJIS configuration.
/// </summary>
public static class AjisDirectiveApplier
{
   /// <summary>
   /// Applies supported directives to the provided settings.
   /// </summary>
   public static bool TryApply(AjisDirective directive, AjisSettings settings, out AjisSettings appliedSettings)
   {
      ArgumentNullException.ThrowIfNull(directive);
      ArgumentNullException.ThrowIfNull(settings);

      appliedSettings = settings;

      if(!string.Equals(directive.Namespace, "AJIS", StringComparison.Ordinal))
         return false;

      if(string.Equals(directive.Command, "mode", StringComparison.Ordinal))
      {
         if(!TryGetModeArgument(directive, out string? mode))
            return false;

         if(!TryParseTextMode(mode, out AjisTextMode textMode))
            return false;

         appliedSettings = CloneWithTextMode(settings, textMode);
         return true;
      }

      return false;
   }

   private static AjisSettings CloneWithTextMode(AjisSettings settings, AjisTextMode mode)
      => new()
      {
         Numbers = settings.Numbers,
         Strings = settings.Strings,
         Comments = settings.Comments,
         Serialization = settings.Serialization,
         AllowDuplicateObjectKeys = settings.AllowDuplicateObjectKeys,
         AllowTrailingCommas = settings.AllowTrailingCommas,
         AllowDirectives = settings.AllowDirectives,
         MaxDepth = settings.MaxDepth,
         TextProvider = settings.TextProvider,
         Logger = settings.Logger,
         EventSink = settings.EventSink,
         FormattingCulture = settings.FormattingCulture,
         ParserProfile = settings.ParserProfile,
         SerializerProfile = settings.SerializerProfile,
         TextMode = mode,
         StreamChunkThreshold = settings.StreamChunkThreshold
      };

   private static bool TryGetModeArgument(AjisDirective directive, out string? value)
   {
      value = null;

      if(directive.Arguments.TryGetValue("mode", out string? mode))
      {
         value = mode;
         return true;
      }

      if(directive.Arguments.TryGetValue("value", out string? val))
      {
         value = val;
         return true;
      }

      if(directive.Arguments.Count == 1)
      {
         value = directive.Arguments.Values.FirstOrDefault();
         return value is not null;
      }

      return false;
   }

   private static bool TryParseTextMode(string? text, out AjisTextMode mode)
   {
      mode = AjisTextMode.Ajis;
      if(string.IsNullOrWhiteSpace(text))
         return false;

      return text.ToLowerInvariant() switch
      {
         "json" => AssignMode(AjisTextMode.Json, out mode),
         "ajis" => AssignMode(AjisTextMode.Ajis, out mode),
         "lex" or "lax" => AssignMode(AjisTextMode.Lex, out mode),
         _ => false
      };
   }

   private static bool AssignMode(AjisTextMode value, out AjisTextMode mode)
   {
      mode = value;
      return true;
   }
}
