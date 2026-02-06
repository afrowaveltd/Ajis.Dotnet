#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSegmentPatchTests
{
   [Fact]
   public void ReplacePropertyValue_ReplacesPrimitiveValue()
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

      AjisSegment replacement = AjisSegment.Value(5, 1, AjisValueKind.Number, Slice("9"));
      List<AjisSegment> patched = AjisSegmentPatch.ReplacePropertyValue(segments, "a", replacement).ToList();

      string[] rendered = patched.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:a",
         "Value:Number:9",
         "PropertyName:b",
         "Value:Number:2",
         "ExitContainer:Object"
      ];

      Assert.Equal(expected, rendered);
   }

   [Fact]
   public void ReplacePropertyValue_ReplacesContainerValue()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("a")),
         AjisSegment.Enter(AjisContainerKind.Array, 5, 1),
         AjisSegment.Value(6, 2, AjisValueKind.Number, Slice("1")),
         AjisSegment.Exit(AjisContainerKind.Array, 7, 1),
         AjisSegment.Name(8, 1, Slice("b")),
         AjisSegment.Value(12, 1, AjisValueKind.Number, Slice("2")),
         AjisSegment.Exit(AjisContainerKind.Object, 13, 0)
      };

      AjisSegment replacement = AjisSegment.Value(5, 1, AjisValueKind.Number, Slice("9"));
      List<AjisSegment> patched = AjisSegmentPatch.ReplacePropertyValue(segments, "a", replacement).ToList();

      string[] rendered = patched.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:a",
         "Value:Number:9",
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
