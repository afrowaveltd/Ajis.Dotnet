#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Small helpers for creating common diagnostics with consistent payload shape.
/// </summary>
public static class AjisDiagnosticFactory
{
   /// <summary>
   /// Creates an <see cref="AjisDiagnosticSeverity.Error"/> diagnostic.
   /// </summary>
   /// <remarks>
   /// <para>
   /// If <paramref name="args"/> are provided, they are stored under <c>Data["args"]</c>.
   /// </para>
   /// </remarks>
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

      // Use the convenience constructor so Key stays stable and derived from Code.
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
