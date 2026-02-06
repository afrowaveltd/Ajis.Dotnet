#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSegmentFilterTests
{
   [Fact]
   public void DropPropertyByName_RemovesPropertyAndValue()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("a")),
         AjisSegment.Value(5, 1, AjisValueKind.Number, Slice("1")),
         AjisSegment.Name(7, 1, Slice("b")),
         AjisSegment.Value(11, 1, AjisValueKind.Number, Slice("2")),
         AjisSegment.Exit(AjisContainerKind.Object, 12, 0)
      };

      List<AjisSegment> filtered = AjisSegmentFilter.DropPropertyByName(segments, "a").ToList();

      string[] rendered = filtered.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:b",
         "Value:Number:2",
         "ExitContainer:Object"
      ];

      Assert.Equal(expected, rendered);
   }

   [Fact]
   public void DropPropertyByName_SkipsContainerValue()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("a")),
         AjisSegment.Enter(AjisContainerKind.Object, 5, 1),
         AjisSegment.Name(6, 2, Slice("x")),
         AjisSegment.Value(10, 2, AjisValueKind.Number, Slice("1")),
         AjisSegment.Exit(AjisContainerKind.Object, 11, 1),
         AjisSegment.Name(12, 1, Slice("b")),
         AjisSegment.Value(16, 1, AjisValueKind.Number, Slice("2")),
         AjisSegment.Exit(AjisContainerKind.Object, 17, 0)
      };

      List<AjisSegment> filtered = AjisSegmentFilter.DropPropertyByName(segments, "a").ToList();

      string[] rendered = filtered.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:b",
         "Value:Number:2",
         "ExitContainer:Object"
      ];

      Assert.Equal(expected, rendered);
   }

   private static AjisSliceUtf8 Slice(string text)
      => new(Encoding.UTF8.GetBytes(text), AjisSliceFlags.None);

   private static string Render(AjisSegment segment)
   {
      string kind = segment.Kind.ToString();
      if(segment.Kind == AjisSegmentKind.EnterContainer || segment.Kind == AjisSegmentKind.ExitContainer)
         return $"{kind}:{segment.ContainerKind}";

      if(segment.Kind == AjisSegmentKind.PropertyName)
         return $"{kind}:{Decode(segment.Slice)}";

      if(segment.Kind == AjisSegmentKind.Value)
         return $"{kind}:{segment.ValueKind}:{Decode(segment.Slice)}";

      return kind;
   }

   private static string Decode(AjisSliceUtf8? slice)
      => slice is null ? string.Empty : Encoding.UTF8.GetString(slice.Value.Bytes.Span);
}
