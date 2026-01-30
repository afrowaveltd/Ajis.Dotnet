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
      IReadOnlyDictionary<string, object?>? data = null;

      if(args is not null && args.Length > 0)
      {
         data = new Dictionary<string, object?>(StringComparer.Ordinal)
         {
            ["args"] = args
         };
      }

      return new AjisDiagnostic(
          Severity: AjisDiagnosticSeverity.Error,
          Code: code,
          Offset: offset,
          Line: line,
          Column: column,
          Path: path,
          Data: data
      );
   }
}