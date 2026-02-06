#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisSegmentMapTests
{
   [Fact]
   public void RenameProperties_RewritesPropertyNames()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("firstName")),
         AjisSegment.Value(11, 1, AjisValueKind.String, Slice("Jane")),
         AjisSegment.Name(18, 1, Slice("lastName")),
         AjisSegment.Value(27, 1, AjisValueKind.String, Slice("Doe")),
         AjisSegment.Exit(AjisContainerKind.Object, 32, 0)
      };

      List<AjisSegment> mapped = AjisSegmentMap.RenameProperties(segments, ToUpper).ToList();

      string[] names = mapped
         .Where(s => s.Kind == AjisSegmentKind.PropertyName)
         .Select(s => Encoding.UTF8.GetString(s.Slice!.Value.Bytes.Span))
         .ToArray();

      Assert.Equal(new[] { "FIRSTNAME", "LASTNAME" }, names);
   }

   private static string ToUpper(string value)
      => value.ToUpperInvariant();

   private static AjisSliceUtf8 Slice(string text)
      => new(Encoding.UTF8.GetBytes(text), AjisSliceFlags.None);
}
