#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSegmentSelectTests
{
   [Fact]
   public void SelectRootPropertyValue_ReturnsBareValue()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("config")),
         AjisSegment.Enter(AjisContainerKind.Object, 9, 1),
         AjisSegment.Name(10, 2, Slice("theme")),
         AjisSegment.Value(18, 2, AjisValueKind.String, Slice("dark")),
         AjisSegment.Exit(AjisContainerKind.Object, 24, 1),
         AjisSegment.Exit(AjisContainerKind.Object, 25, 0)
      };

      List<AjisSegment> selected = AjisSegmentSelect.SelectRootPropertyValue(segments, "config").ToList();

      string[] rendered = selected.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:theme",
         "Value:String:dark",
         "ExitContainer:Object"
      ];

      Assert.Equal(expected, rendered);
   }

   [Fact]
   public void SelectRootPropertyWrapped_WrapsValue()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("config")),
         AjisSegment.Value(9, 1, AjisValueKind.Number, Slice("1")),
         AjisSegment.Exit(AjisContainerKind.Object, 10, 0)
      };

      List<AjisSegment> selected = AjisSegmentSelect.SelectRootPropertyWrapped(segments, "config").ToList();

      string[] rendered = selected.Select(Render).ToArray();
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:config",
         "Value:Number:1",
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
