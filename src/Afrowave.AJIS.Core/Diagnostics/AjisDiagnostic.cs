#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// A single diagnostic produced by AJIS components (parsers, walkers, serializers).
/// </summary>
/// <remarks>
/// <para>
/// Diagnostics are intended to be stable across versions:
/// <list type="bullet">
/// <item><description><see cref="Code"/> stays stable (enum value).</description></item>
/// <item><description><see cref="Key"/> stays stable (string), suitable for localization.</description></item>
/// </list>
/// </para>
/// <para>
/// <see cref="Path"/> is optional and can carry a logical source identifier
/// (file path, test case name, stream label, etc.).
/// </para>
/// <para>
/// <see cref="Data"/> is optional structured payload for diagnostics (e.g. expected/actual,
/// char code, limits). Keep it lightweight and serializable.
/// </para>
/// </remarks>
public sealed record AjisDiagnostic(
   AjisDiagnosticCode Code,
   string Key,
   AjisDiagnosticSeverity Severity,
   long Offset,
   int? Line,
   int? Column,
   string? Path = null,
   object? Data = null,
   string? Message = null)
{
   /// <summary>
   /// Convenience constructor that maps <see cref="AjisDiagnosticCode"/> to its stable <see cref="Key"/>.
   /// </summary>
   public AjisDiagnostic(
      AjisDiagnosticCode code,
      long offset,
      AjisDiagnosticSeverity severity = AjisDiagnosticSeverity.Error,
      int? line = null,
      int? column = null,
      string? path = null,
      object? data = null,
      string? message = null)
      : this(code, AjisDiagnosticKeys.For(code), severity, offset, line, column, path, data, message)
   {
   }
}
