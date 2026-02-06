#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Streaming segment map helpers.
/// </summary>
public static class AjisSegmentMap
{
   /// <summary>
   /// Rewrites property names using a mapping function.
   /// </summary>
   public static IEnumerable<AjisSegment> RenameProperties(
      IEnumerable<AjisSegment> segments,
      Func<string, string> rename)
   {
      ArgumentNullException.ThrowIfNull(segments);
      ArgumentNullException.ThrowIfNull(rename);

      foreach(AjisSegment segment in segments)
      {
         if(segment.Kind != AjisSegmentKind.PropertyName || segment.Slice is null)
         {
            yield return segment;
            continue;
         }

         string current = Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);
         string updated = rename(current);
         if(string.IsNullOrWhiteSpace(updated))
            throw new ArgumentException(null, nameof(rename));

         AjisSliceUtf8 slice = new(Encoding.UTF8.GetBytes(updated), segment.Slice.Value.Flags);
         yield return segment with { Slice = slice };
      }
   }
}
