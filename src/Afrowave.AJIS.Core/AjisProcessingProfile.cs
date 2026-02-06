#nullable enable

namespace Afrowave.AJIS.Core;

/// <summary>
/// Processing profile hint for parsers and serializers.
/// </summary>
public enum AjisProcessingProfile
{
   /// <summary>
   /// Balanced, general-purpose profile.
   /// </summary>
   Universal = 0,

   /// <summary>
   /// Prefer minimal memory usage (embedded / large files).
   /// </summary>
   LowMemory = 1,

   /// <summary>
   /// Prefer maximum throughput (server workloads).
   /// </summary>
   HighThroughput = 2
}
