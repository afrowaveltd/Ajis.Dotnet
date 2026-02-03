#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Diagnostic severity.
/// </summary>
public enum AjisDiagnosticSeverity
{
   /// <summary>
   /// Extra diagnostic detail intended for deep troubleshooting.
   /// Typically off by default.
   /// </summary>
   Debug = 0,

   /// <summary>
   /// Informational message.
   /// </summary>
   Info = 1,

   /// <summary>
   /// Warning: non-fatal issue.
   /// </summary>
   Warning = 2,

   /// <summary>
   /// Error: parsing/processing could not continue or output is invalid.
   /// </summary>
   Error = 3,
}
