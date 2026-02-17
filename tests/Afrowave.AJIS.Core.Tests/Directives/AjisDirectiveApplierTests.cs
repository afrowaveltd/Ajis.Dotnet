#nullable enable

using Afrowave.AJIS.Core.Directives;

namespace Afrowave.AJIS.Core.Tests.Directives;

public sealed class AjisDirectiveApplierTests
{
   [Fact]
   public void TryApply_ReturnsTrue_ForMode()
   {
      AjisDirective directive = new AjisDirective("AJIS", "mode", new Dictionary<string, string> { ["value"] = "lex" });
      AjisSettings settings = new AjisSettings();

      bool applied = AjisDirectiveApplier.TryApply(directive, settings, out _);

      Assert.True(applied);
   }

   [Fact]
   public void TryApply_SetsTextMode()
   {
      AjisDirective directive = new AjisDirective("AJIS", "mode", new Dictionary<string, string> { ["value"] = "json" });
      AjisSettings settings = new AjisSettings();

      AjisDirectiveApplier.TryApply(directive, settings, out AjisSettings appliedSettings);

      Assert.Equal(AjisTextMode.Json, appliedSettings.TextMode);
   }

   [Fact]
   public void TryApply_ReturnsFalse_ForUnknownNamespace()
   {
      AjisDirective directive = new AjisDirective("TOOL", "mode", new Dictionary<string, string> { ["value"] = "lex" });
      AjisSettings settings = new AjisSettings();

      bool applied = AjisDirectiveApplier.TryApply(directive, settings, out _);

      Assert.False(applied);
   }
}