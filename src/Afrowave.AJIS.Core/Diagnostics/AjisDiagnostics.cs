#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Helper factory methods for creating diagnostics.
/// </summary>
public static class AjisDiagnostics
{
   public static AjisDiagnostic Create(
      AjisDiagnosticCode code,
      long offset,
      AjisDiagnosticSeverity severity = AjisDiagnosticSeverity.Error,
      int? line = null,
      int? column = null,
      string? path = null,
      object? data = null,
      string? message = null)
      => new(code, offset, severity, line, column, path, data, message);

   public static AjisDiagnostic Create(
      AjisDiagnosticCode code,
      string key,
      long offset,
      AjisDiagnosticSeverity severity = AjisDiagnosticSeverity.Error,
      int? line = null,
      int? column = null,
      string? path = null,
      object? data = null,
      string? message = null)
      => new(code, key, severity, offset, line, column, path, data, message);
}
