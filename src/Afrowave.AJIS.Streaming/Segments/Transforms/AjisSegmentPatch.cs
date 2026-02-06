#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Streaming segment patch helpers.
/// </summary>
public static class AjisSegmentPatch
{
   /// <summary>
   /// Replaces the value of properties with the specified name using a replacement value segment.
   /// </summary>
   public static IEnumerable<AjisSegment> ReplacePropertyValue(
      IEnumerable<AjisSegment> segments,
      string propertyName,
      AjisSegment replacementValue)
   {
      ArgumentNullException.ThrowIfNull(segments);
      if(string.IsNullOrWhiteSpace(propertyName))
         throw new ArgumentException(null, nameof(propertyName));

      if(replacementValue.Kind != AjisSegmentKind.Value)
         throw new ArgumentException(null, nameof(replacementValue));

      using IEnumerator<AjisSegment> enumerator = segments.GetEnumerator();
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.PropertyName && segment.Slice is { } slice)
         {
            string name = Encoding.UTF8.GetString(slice.Bytes.Span);
            if(string.Equals(name, propertyName, StringComparison.Ordinal))
            {
               yield return segment;
               AjisSegment valueSegment = ReadNextValueSegment(enumerator, out List<AjisSegment> leadingMeta);
               foreach(AjisSegment meta in leadingMeta)
                  yield return meta;

               AjisSegment patched = replacementValue with { Position = valueSegment.Position, Depth = valueSegment.Depth };
               yield return patched;
               if(valueSegment.Kind == AjisSegmentKind.EnterContainer)
                  SkipContainer(enumerator, valueSegment.Depth);
               continue;
            }
         }

         yield return segment;
      }
   }

   private static AjisSegment ReadNextValueSegment(
      IEnumerator<AjisSegment> enumerator,
      out List<AjisSegment> leadingMeta)
   {
      leadingMeta = [];
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.Comment || segment.Kind == AjisSegmentKind.Directive)
         {
            leadingMeta.Add(segment);
            continue;
         }

         return segment;
      }

      throw new InvalidOperationException("Missing value segment.");
   }

   private static void SkipContainer(IEnumerator<AjisSegment> enumerator, int depth)
   {
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == depth)
            return;
      }
   }
}
