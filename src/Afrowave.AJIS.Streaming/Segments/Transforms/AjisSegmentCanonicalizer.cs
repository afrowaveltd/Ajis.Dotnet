#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Canonicalizes segment streams by sorting object properties.
/// </summary>
public static class AjisSegmentCanonicalizer
{
   /// <summary>
   /// Reorders object properties by name within each object frame.
   /// </summary>
   public static IEnumerable<AjisSegment> Canonicalize(IEnumerable<AjisSegment> segments)
   {
      ArgumentNullException.ThrowIfNull(segments);

      using IEnumerator<AjisSegment> enumerator = segments.GetEnumerator();
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.EnterContainer && segment.ContainerKind == AjisContainerKind.Object)
         {
            foreach(AjisSegment canonical in CanonicalizeObject(segment, enumerator))
               yield return canonical;
            continue;
         }

         yield return segment;
      }
   }

   private static IEnumerable<AjisSegment> CanonicalizeObject(
      AjisSegment start,
      IEnumerator<AjisSegment> enumerator)
   {
      int depth = start.Depth;
      var blocks = new List<PropertyBlock>();
      var trailing = new List<AjisSegment>();
      var metaBuffer = new List<AjisSegment>();

      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == depth)
         {
            trailing.Add(segment);
            break;
         }

         if(segment.Kind == AjisSegmentKind.Comment || segment.Kind == AjisSegmentKind.Directive)
         {
            metaBuffer.Add(segment);
            continue;
         }

         if(segment.Kind == AjisSegmentKind.PropertyName)
         {
            AjisSegment nameSegment = segment;
            AjisSegment valueSegment = ReadNextValue(enumerator, metaBuffer, out List<AjisSegment> valueMeta);
            var blockSegments = new List<AjisSegment>();
            blockSegments.AddRange(metaBuffer);
            blockSegments.Add(nameSegment);
            blockSegments.AddRange(valueMeta);
            blockSegments.Add(valueSegment);

            if(valueSegment.Kind == AjisSegmentKind.EnterContainer)
            {
               foreach(AjisSegment child in ReadContainer(enumerator, valueSegment.Depth))
                  blockSegments.Add(child);
            }

            string name = Decode(nameSegment.Slice);
            blocks.Add(new PropertyBlock(name, blockSegments));
            metaBuffer.Clear();
            continue;
         }

         metaBuffer.Add(segment);
      }

      yield return start;

      foreach(PropertyBlock block in blocks.OrderBy(b => b.Name, StringComparer.Ordinal))
      {
         foreach(AjisSegment segment in block.Segments)
            yield return segment;
      }

      foreach(AjisSegment segment in trailing)
         yield return segment;
   }

   private static AjisSegment ReadNextValue(
      IEnumerator<AjisSegment> enumerator,
      List<AjisSegment> metaBuffer,
      out List<AjisSegment> valueMeta)
   {
      valueMeta = [];
      while(enumerator.MoveNext())
      {
         AjisSegment segment = enumerator.Current;
         if(segment.Kind == AjisSegmentKind.Comment || segment.Kind == AjisSegmentKind.Directive)
         {
            valueMeta.Add(segment);
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

   private static string Decode(AjisSliceUtf8? slice)
      => slice is null ? string.Empty : Encoding.UTF8.GetString(slice.Value.Bytes.Span);

   private readonly record struct PropertyBlock(string Name, List<AjisSegment> Segments);
}
