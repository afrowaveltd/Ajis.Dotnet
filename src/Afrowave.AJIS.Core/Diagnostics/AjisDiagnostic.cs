#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using System.Globalization;

namespace Afrowave.AJIS.Core.Diagnostics;

public sealed record AjisDiagnostic(
    AjisDiagnosticSeverity Severity,
    AjisDiagnosticCode Code,
    long Offset,
    int? Line,
    int? Column,
    string? Path,
    IReadOnlyDictionary<string, object?>? Data = null)
{
   public string MessageKey => AjisDiagnosticKeys.For(Code);

   public string FormatMessage(IAjisTextProvider provider, CultureInfo? culture = null)
   {
      culture ??= CultureInfo.CurrentUICulture;

      // Minimal convention: Data["args"] = object?[] if formatting is needed.
      return provider.GetText(MessageKey, culture, Data);
   }
}