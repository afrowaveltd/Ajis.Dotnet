#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Streaming segment filter helpers.
/// </summary>
public static class AjisSegmentFilter
{
   /// <summary>
   /// Filters segments using a predicate, skipping subtrees when requested.
   /// </summary>
   public static IEnumerable<AjisSegment> Filter(
      IEnumerable<AjisSegment> segments,
      Func<AjisSegment, bool> predicate)
   {
      ArgumentNullException.ThrowIfNull(segments);
      ArgumentNullException.ThrowIfNull(predicate);

      int? skipDepth = null;
      bool skipNextValue = false;

      foreach(AjisSegment segment in segments)
      {
         if(skipDepth is not null)
         {
            if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == skipDepth)
               skipDepth = null;

            continue;
         }

         if(skipNextValue)
         {
            if(segment.Kind == AjisSegmentKind.Comment || segment.Kind == AjisSegmentKind.Directive)
               continue;

            if(segment.Kind == AjisSegmentKind.EnterContainer)
            {
               skipDepth = segment.Depth;
               skipNextValue = false;
               continue;
            }

            if(segment.Kind == AjisSegmentKind.Value)
            {
               skipNextValue = false;
               continue;
            }

            if(segment.Kind == AjisSegmentKind.ExitContainer)
            {
               skipNextValue = false;
               continue;
            }
         }

         if(!predicate(segment))
         {
            if(segment.Kind == AjisSegmentKind.EnterContainer)
               skipDepth = segment.Depth;
            else if(segment.Kind == AjisSegmentKind.PropertyName)
               skipNextValue = true;

            continue;
         }

         yield return segment;
      }
   }

   /// <summary>
   /// Drops properties by name, including their associated value segments.
   /// </summary>
   public static IEnumerable<AjisSegment> DropPropertyByName(
      IEnumerable<AjisSegment> segments,
      string propertyName)
   {
      if(string.IsNullOrWhiteSpace(propertyName))
         throw new ArgumentException(null, nameof(propertyName));

      return Filter(segments, segment =>
      {
         if(segment.Kind != AjisSegmentKind.PropertyName)
            return true;

         if(segment.Slice is null)
            return true;

         string name = Encoding.UTF8.GetString(segment.Slice.Value.Bytes.Span);
         return !string.Equals(name, propertyName, StringComparison.Ordinal);
      });
   }
}
