#nullable enable

using CoreDiagnostics = global::Afrowave.AJIS.Core.Diagnostics;

namespace Afrowave.AJIS.Core.Tests.Diagnostics;

public sealed class AjisDiagnosticFactoryTests
{
   [Fact]
   public void Error_StoresArgsInData()
   {
      var diag = CoreDiagnostics.AjisDiagnosticFactory.Error(
         CoreDiagnostics.AjisDiagnosticCode.ExpectedChar,
         offset: 10,
         args: ["a", "b"]);

      Assert.Equal(CoreDiagnostics.AjisDiagnosticSeverity.Error, diag.Severity);
      Assert.Equal(CoreDiagnostics.AjisDiagnosticKeys.For(CoreDiagnostics.AjisDiagnosticCode.ExpectedChar), diag.Key);
      Assert.NotNull(diag.Data);

      var data = Assert.IsType<IReadOnlyDictionary<string, object?>>(diag.Data, exactMatch: false);
      var args = Assert.IsType<object?[]>(data["args"]);
      Assert.Equal(["a", "b"], args);
   }
}
