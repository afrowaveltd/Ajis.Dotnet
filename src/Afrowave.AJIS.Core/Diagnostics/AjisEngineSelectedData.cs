#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Structured payload for the engine selection debug/info diagnostic.
/// </summary>
/// <param name="EngineId">Selected engine identifier.</param>
/// <param name="Preference">Selection preference that was applied.</param>
/// <param name="LargePayloadThresholdBytes">Payload size threshold used during selection.</param>
public sealed record AjisEngineSelectedData(
   string EngineId,
   string Preference,
   int LargePayloadThresholdBytes);
