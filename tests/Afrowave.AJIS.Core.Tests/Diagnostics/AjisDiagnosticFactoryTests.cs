#nullable enable

using CoreDiagnostics = global::Afrowave.AJIS.Core.Diagnostics;
using Xunit;

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

      var data = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(diag.Data);
      var args = Assert.IsType<object?[]>(data["args"]);
      Assert.Equal(new object?[] { "a", "b" }, args);
   }
}
