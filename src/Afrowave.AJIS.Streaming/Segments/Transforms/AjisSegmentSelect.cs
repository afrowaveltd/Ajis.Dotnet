#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Streaming segment selection helpers.
/// </summary>
public static class AjisSegmentSelect
{
   /// <summary>
   /// Selects a property value from the root object and emits a wrapped object containing only that property.
   /// </summary>
   public static IEnumerable<AjisSegment> SelectRootPropertyWrapped(
      IEnumerable<AjisSegment> segments,
      string propertyName)
   {
      ArgumentNullException.ThrowIfNull(segments);
      if(string.IsNullOrWhiteSpace(propertyName))
         throw new ArgumentException(null, nameof(propertyName));

      bool emitted = false;
      yield return AjisSegment.Enter(AjisContainerKind.Object, 0, 0);

      foreach(AjisSegment segment in SelectRootPropertyValue(segments, propertyName))
      {
         if(!emitted)
         {
            yield return AjisSegment.Name(segment.Position, 1, new AjisSliceUtf8(Encoding.UTF8.GetBytes(propertyName), AjisSliceFlags.None));
            emitted = true;
         }

         if(segment.Kind == AjisSegmentKind.EnterContainer)
            yield return segment with { Depth = segment.Depth + 1 };
         else if(segment.Kind == AjisSegmentKind.ExitContainer)
            yield return segment with { Depth = segment.Depth + 1 };
         else
            yield return segment with { Depth = segment.Depth + 1 };
      }

      yield return AjisSegment.Exit(AjisContainerKind.Object, 0, 0);
   }

   /// <summary>
   /// Selects a property value from the root object and emits the value as a bare segment stream.
   /// </summary>
   public static IEnumerable<AjisSegment> SelectRootPropertyValue(
      IEnumerable<AjisSegment> segments,
      string propertyName)
   {
      ArgumentNullException.ThrowIfNull(segments);
      if(string.IsNullOrWhiteSpace(propertyName))
         throw new ArgumentException(null, nameof(propertyName));

      using IEnumerator<AjisSegment> enumerator = segments.GetEnumerator();
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.PropertyName && segment.Depth == 1 && segment.Slice is { } slice)
         {
            string name = Encoding.UTF8.GetString(slice.Bytes.Span);
            if(!string.Equals(name, propertyName, StringComparison.Ordinal))
               continue;

            AjisSegment valueSegment = ReadNextValueSegment(enumerator, out List<AjisSegment> leadingMeta);
            foreach(AjisSegment meta in leadingMeta)
               yield return meta;

            yield return valueSegment;

            if(valueSegment.Kind == AjisSegmentKind.EnterContainer)
            {
               foreach(AjisSegment inner in ReadContainer(enumerator, valueSegment.Depth))
                  yield return inner;
            }

            yield break;
         }
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

   private static IEnumerable<AjisSegment> ReadContainer(IEnumerator<AjisSegment> enumerator, int depth)
   {
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         yield return segment;
         if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == depth)
            yield break;
      }
   }
}
