#nullable enable

using CoreDiagnostics = global::Afrowave.AJIS.Core.Diagnostics;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Diagnostics;

public sealed class AjisDiagnosticsTests
{
   [Fact]
   public void Create_UsesStableKeyFromCode()
   {
      var diag = CoreDiagnostics.AjisDiagnostics.Create(CoreDiagnostics.AjisDiagnosticCode.UnexpectedEof, offset: 5);

      Assert.Equal(CoreDiagnostics.AjisDiagnosticKeys.For(CoreDiagnostics.AjisDiagnosticCode.UnexpectedEof), diag.Key);
      Assert.Equal(CoreDiagnostics.AjisDiagnosticSeverity.Error, diag.Severity);
      Assert.Equal(5, diag.Offset);
   }

   [Fact]
   public void Create_WithKey_PreservesProvidedKey()
   {
      var diag = CoreDiagnostics.AjisDiagnostics.Create(
         CoreDiagnostics.AjisDiagnosticCode.Unknown,
         key: "custom_key",
         offset: 0,
         severity: CoreDiagnostics.AjisDiagnosticSeverity.Warning);

      Assert.Equal("custom_key", diag.Key);
      Assert.Equal(CoreDiagnostics.AjisDiagnosticSeverity.Warning, diag.Severity);
   }
}
