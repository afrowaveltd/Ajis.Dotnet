#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Structured payload for the engine selection debug/info diagnostic.
/// </summary>
public sealed record AjisEngineSelectedData(
   string EngineId,
   string Preference,
   int LargePayloadThresholdBytes);
