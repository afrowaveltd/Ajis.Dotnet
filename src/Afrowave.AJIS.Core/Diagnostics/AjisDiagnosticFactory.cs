#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

public static class AjisDiagnosticFactory
{
   public static AjisDiagnostic Error(
       AjisDiagnosticCode code,
       long offset,
       int? line = null,
       int? column = null,
       string? path = null,
       object?[]? args = null)
   {
      object? data = null;

      if(args is not null && args.Length > 0)
      {
         // Keep payload lightweight and serializable.
         data = new Dictionary<string, object?>(StringComparer.Ordinal)
         {
            ["args"] = args
         };
      }

      // Use the convenience ctor so Key is always provided via AjisDiagnosticKeys.For(code).
      return new AjisDiagnostic(
         code: code,
         offset: offset,
         severity: AjisDiagnosticSeverity.Error,
         line: line,
         column: column,
         path: path,
         data: data);
   }
}
