#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Core.Directives;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Applies document-level directives to AJIS settings.
/// </summary>
public static class AjisDirectiveSettingsApplier
{
   /// <summary>
   /// Applies document-level directives and returns updated settings.
   /// </summary>
   public static AjisSettings ApplyDocumentDirectives(
      IEnumerable<AjisParsedDirectiveBinding> bindings,
      AjisSettings settings)
   {
      ArgumentNullException.ThrowIfNull(bindings);
      ArgumentNullException.ThrowIfNull(settings);

      AjisSettings current = settings;
      foreach(AjisParsedDirectiveBinding binding in bindings)
      {
         if(binding.Scope != AjisDirectiveBindingScope.Document)
            continue;

         if(AjisDirectiveApplier.TryApply(binding.Directive, current, out AjisSettings updated))
            current = updated;
      }

      return current;
   }

   /// <summary>
   /// Applies document-level directives from a segment stream and returns updated settings.
   /// </summary>
   public static AjisSettings ApplyDocumentDirectives(
      IEnumerable<AjisSegment> segments,
      AjisSettings settings)
   {
      ArgumentNullException.ThrowIfNull(segments);
      ArgumentNullException.ThrowIfNull(settings);

      IEnumerable<AjisParsedDirectiveBinding> bindings = AjisDirectiveBinder.BindAndParseDirectives(segments);
      return ApplyDocumentDirectives(bindings, settings);
   }
}
