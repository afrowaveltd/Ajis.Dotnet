#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;

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

      List<AjisSegment> filtered = [.. AjisSegmentFilter.DropPropertyByName(segments, "a")];

      string[] rendered = [.. filtered.Select(Render)];
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
   public void DropPropertyByPath_RemovesNestedProperty()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("config")),
         AjisSegment.Enter(AjisContainerKind.Object, 9, 1),
         AjisSegment.Name(10, 2, Slice("theme")),
         AjisSegment.Value(18, 2, AjisValueKind.String, Slice("dark")),
         AjisSegment.Name(25, 2, Slice("mode")),
         AjisSegment.Value(33, 2, AjisValueKind.String, Slice("auto")),
         AjisSegment.Exit(AjisContainerKind.Object, 39, 1),
         AjisSegment.Exit(AjisContainerKind.Object, 40, 0)
      };

      List<AjisSegment> filtered = [.. AjisSegmentFilter.DropPropertyByPath(segments, "$.config.theme")];

      string[] rendered = [.. filtered.Select(Render)];
      string[] expected =
      [
         "EnterContainer:Object",
         "PropertyName:config",
         "EnterContainer:Object",
         "PropertyName:mode",
         "Value:String:auto",
         "ExitContainer:Object",
         "ExitContainer:Object"
      ];

      Assert.Equal(expected, rendered);
   }

   [Fact]
   public void FilterArrayItems_FiltersByPredicate()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Array, 0, 0),
         AjisSegment.Enter(AjisContainerKind.Object, 1, 1),
         AjisSegment.Name(2, 2, Slice("isActive")),
         AjisSegment.Value(12, 2, AjisValueKind.Boolean, Slice("true")),
         AjisSegment.Exit(AjisContainerKind.Object, 16, 1),
         AjisSegment.Enter(AjisContainerKind.Object, 17, 1),
         AjisSegment.Name(18, 2, Slice("isActive")),
         AjisSegment.Value(28, 2, AjisValueKind.Boolean, Slice("false")),
         AjisSegment.Exit(AjisContainerKind.Object, 33, 1),
         AjisSegment.Exit(AjisContainerKind.Array, 34, 0)
      };

      List<AjisSegment> filtered = [.. AjisSegmentFilter.FilterArrayItems(segments, HasActiveFlag)];

      string[] rendered = [.. filtered.Select(Render)];
      string[] expected =
      [
         "EnterContainer:Array",
         "EnterContainer:Object",
         "PropertyName:isActive",
         "Value:Boolean:true",
         "ExitContainer:Object",
         "ExitContainer:Array"
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

      List<AjisSegment> filtered = [.. AjisSegmentFilter.DropPropertyByName(segments, "a")];

      string[] rendered = [.. filtered.Select(Render)];
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

   private static bool HasActiveFlag(IReadOnlyList<AjisSegment> item)
   {
      for(int i = 0; i < item.Count - 1; i++)
      {
         AjisSegment segment = item[i];
         if(segment.Kind == AjisSegmentKind.PropertyName && Decode(segment.Slice) == "isActive")
         {
            AjisSegment value = item[i + 1];
            return value.Kind == AjisSegmentKind.Value
               && value.ValueKind == AjisValueKind.Boolean
               && Decode(value.Slice) == "true";
         }
      }

      return false;
   }

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
