#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Core.Directives;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using Xunit;
using Afrowave.AJIS.Streaming;

namespace Afrowave.AJIS.Core.Tests.Directives;

public sealed class AjisDirectiveSettingsApplierTests
{
   [Fact]
   public void ApplyDocumentDirectives_ReturnsUpdatedSettings()
   {
      var settings = new AjisSettings();
      var binding = new AjisParsedDirectiveBinding(
         new AjisDirective("AJIS", "mode", new Dictionary<string, string> { ["value"] = "json" }),
         AjisDirectiveBindingScope.Document,
         "$",
         null);

      AjisSettings updated = AjisDirectiveSettingsApplier.ApplyDocumentDirectives([binding], settings);

      Assert.Equal(AjisTextMode.Json, updated.TextMode);
   }

   [Fact]
   public void ApplyDocumentDirectives_IgnoresNonDocumentBindings()
   {
      var settings = new AjisSettings();
      var binding = new AjisParsedDirectiveBinding(
         new AjisDirective("AJIS", "mode", new Dictionary<string, string> { ["value"] = "lex" }),
         AjisDirectiveBindingScope.Target,
         "$.a",
         AjisSegmentKind.Value);

      AjisSettings updated = AjisDirectiveSettingsApplier.ApplyDocumentDirectives([binding], settings);

      Assert.Equal(AjisTextMode.Ajis, updated.TextMode);
   }

   [Fact]
   public void ApplyDocumentDirectives_UsesSegments()
   {
      var settings = new AjisSettings();
      var segments = new List<AjisSegment>
      {
         AjisSegment.Directive(0, 0, new AjisSliceUtf8("ajis mode value=json"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(1, 0, AjisValueKind.Boolean, new AjisSliceUtf8("true"u8.ToArray(), AjisSliceFlags.None))
      };

      AjisSettings updated = AjisDirectiveSettingsApplier.ApplyDocumentDirectives(segments, settings);

      Assert.Equal(AjisTextMode.Json, updated.TextMode);
   }
}
