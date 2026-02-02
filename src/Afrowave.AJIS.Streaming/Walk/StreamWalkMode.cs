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
/// </remarks>
public enum AjisStreamWalkMode
{
   /// <summary>
   /// Strict JSON-compatible walking/parsing rules.
   /// </summary>
   Json = 0,

   /// <summary>
   /// AJIS walking/parsing rules (JSON + extensions).
   /// </summary>
   Ajis = 1,
}
