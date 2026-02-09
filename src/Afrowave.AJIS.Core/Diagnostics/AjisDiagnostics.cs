#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Helper factory methods for creating diagnostics.
/// </summary>
public static class AjisDiagnostics
{
   /// <summary>
   /// Creates a diagnostic using the standard key for the code.
   /// </summary>
   /// <param name="code">Diagnostic code.</param>
   /// <param name="offset">Byte offset in the source.</param>
   /// <param name="severity">Severity of the diagnostic.</param>
   /// <param name="line">Optional line number.</param>
   /// <param name="column">Optional column number.</param>
   /// <param name="path">Optional source identifier.</param>
   /// <param name="data">Optional structured payload.</param>
   /// <param name="message">Optional human-readable message override.</param>
   /// <returns>The created diagnostic.</returns>
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

   /// <summary>
   /// Creates a diagnostic with an explicit key override.
   /// </summary>
   /// <param name="code">Diagnostic code.</param>
   /// <param name="key">Stable localization key.</param>
   /// <param name="offset">Byte offset in the source.</param>
   /// <param name="severity">Severity of the diagnostic.</param>
   /// <param name="line">Optional line number.</param>
   /// <param name="column">Optional column number.</param>
   /// <param name="path">Optional source identifier.</param>
   /// <param name="data">Optional structured payload.</param>
   /// <param name="message">Optional human-readable message override.</param>
   /// <returns>The created diagnostic.</returns>
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
