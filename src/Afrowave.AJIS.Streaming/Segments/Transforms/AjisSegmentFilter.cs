#nullable enable

using System.Text;
using Afrowave.AJIS.Streaming.Segments.Transforms;

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

   /// <summary>
   /// Drops properties by absolute path (e.g. <c>$.config.theme</c>).
   /// </summary>
   public static IEnumerable<AjisSegment> DropPropertyByPath(
      IEnumerable<AjisSegment> segments,
      string path)
   {
      ArgumentNullException.ThrowIfNull(segments);
      if(string.IsNullOrWhiteSpace(path))
         throw new ArgumentException(null, nameof(path));

      var tracker = new AjisSegmentPathTracker();
      int? skipDepth = null;
      bool skipNextValue = false;

      foreach(AjisSegment segment in segments)
      {
         string currentPath = tracker.Update(segment);

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

            if(segment.Kind == AjisSegmentKind.Value || segment.Kind == AjisSegmentKind.ExitContainer)
            {
               skipNextValue = false;
               continue;
            }
         }

         if(segment.Kind == AjisSegmentKind.PropertyName && string.Equals(currentPath, path, StringComparison.Ordinal))
         {
            skipNextValue = true;
            continue;
         }

         yield return segment;
      }
   }

   /// <summary>
   /// Filters array items using a predicate over buffered item segments.
   /// </summary>
   public static IEnumerable<AjisSegment> FilterArrayItems(
      IEnumerable<AjisSegment> segments,
      Func<IReadOnlyList<AjisSegment>, bool> predicate)
   {
      ArgumentNullException.ThrowIfNull(segments);
      ArgumentNullException.ThrowIfNull(predicate);

      var stack = new Stack<AjisContainerKind>();
      using IEnumerator<AjisSegment> enumerator = segments.GetEnumerator();
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(stack.TryPeek(out AjisContainerKind current) && current == AjisContainerKind.Array
            && segment.Kind is AjisSegmentKind.Value or AjisSegmentKind.EnterContainer)
         {
            List<AjisSegment> item = ReadArrayItem(segment, enumerator);
            if(predicate(item))
            {
               foreach(AjisSegment itemSegment in item)
                  yield return itemSegment;
            }
            continue;
         }

         if(segment.Kind == AjisSegmentKind.EnterContainer)
         {
            stack.Push(segment.ContainerKind ?? AjisContainerKind.Object);
            yield return segment;
            continue;
         }

         if(segment.Kind == AjisSegmentKind.ExitContainer)
         {
            if(stack.Count > 0)
               stack.Pop();
            yield return segment;
            continue;
         }

         yield return segment;
      }
   }

   private static List<AjisSegment> ReadArrayItem(AjisSegment first, IEnumerator<AjisSegment> enumerator)
   {
      var item = new List<AjisSegment> { first };
      if(first.Kind != AjisSegmentKind.EnterContainer)
         return item;

      int depth = first.Depth;
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         item.Add(segment);
         if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == depth)
            break;
      }

      return item;
   }
}
