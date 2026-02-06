#nullable enable

using CoreDiagnostics = global::Afrowave.AJIS.Core.Diagnostics;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Diagnostics;

public sealed class AjisDiagnosticKeysTests
{
   [Fact]
   public void For_ReturnsExpectedKey()
   {
      Assert.Equal("input_not_supported", CoreDiagnostics.AjisDiagnosticKeys.For(CoreDiagnostics.AjisDiagnosticCode.InputNotSupported));
   }

   [Fact]
   public void For_Unknown_ReturnsUnknownKey()
   {
      var key = CoreDiagnostics.AjisDiagnosticKeys.For((CoreDiagnostics.AjisDiagnosticCode)999999);
      Assert.Equal(CoreDiagnostics.AjisDiagnosticKeys.Unknown, key);
   }
}
