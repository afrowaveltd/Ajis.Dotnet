#nullable enable

namespace Afrowave.AJIS.Streaming.Walk.Input;

/// <summary>
/// Abstract AJIS/JSON input source. Allows Span (in-memory) and Stream/File based sources.
/// </summary>
public interface IAjisInput
{
   /// <summary>Best effort length in bytes (UTF-8). -1 when unknown.</summary>
   long LengthBytes { get; }

   /// <summary>
   /// True if the input can provide a contiguous UTF-8 span without allocations.
   /// </summary>
   bool TryGetUtf8Span(out ReadOnlySpan<byte> utf8);

   /// <summary>
   /// Optional stream access (future engines). May return null.
   /// </summary>
   Stream? OpenStream();
}
