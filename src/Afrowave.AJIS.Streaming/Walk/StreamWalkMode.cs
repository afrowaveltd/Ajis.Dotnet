#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Selects the parsing flavor for the StreamWalk engine.
/// </summary>
/// <remarks>
/// <para>
/// AJIS is a superset of JSON, but the walker can be configured to behave strictly like JSON
/// (useful for baseline tests) or to enable AJIS-specific extensions.
/// </para>
/// <para>
/// <see cref="Lax"/> is a best-effort mode intended for tooling (editors, converters) where
/// preserving as much information as possible is more valuable than strict rejection.
/// </para>
/// </remarks>
public enum AjisStreamWalkMode
{
   /// <summary>
   /// Strict JSON-compatible walking/parsing rules.
   /// </summary>
   Json = 0,

   /// <summary>
   /// Strict AJIS walking/parsing rules (JSON + AJIS extensions as enabled by options).
   /// </summary>
   Ajis = 1,

   /// <summary>
   /// Best-effort mode: tries to recover from minor syntax issues when possible.
   /// </summary>
   /// <remarks>
   /// <para>
   /// Lax mode MUST NOT silently change the meaning of recognized constructs.
   /// Unknown constructs MUST be preserved as raw slices when safely bounded.
   /// </para>
   /// <para>
   /// In Lax mode the engine may emit <see cref="Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticSeverity.Debug"/>
   /// diagnostics for recoverable issues (controlled by runner options).
   /// </para>
   /// </remarks>
   Lax = 2,
}
