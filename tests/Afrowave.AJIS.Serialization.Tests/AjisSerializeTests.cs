#nullable enable

using Afrowave.AJIS.Serialization;
using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializeTests
{
   [Fact]
   public void ToText_SerializesSimpleObject()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("a"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(5, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 6, 0)
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Equal("{\"a\":1}", text);
   }

   [Fact]
   public void ToText_EscapesStringSlice()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Value(0, 0, AjisValueKind.String, new AjisSliceUtf8("a\n\"b"u8.ToArray(), AjisSliceFlags.None))
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Equal("\"a\\n\\\"b\"", text);
   }

   [Fact]
   public void ToText_IgnoresCommentAndDirectiveSegments()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Comment(0, 0, new AjisSliceUtf8("note"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(1, 0, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Directive(2, 0, new AjisSliceUtf8("dir"u8.ToArray(), AjisSliceFlags.None))
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Equal("1", text);
   }

   [Fact]
   public async Task ToStreamAsync_SerializesSimpleArray()
   {
      await using var stream = new MemoryStream();
      var segments = GetSegments();

      await AjisSerialize.ToStreamAsync(stream, segments, ct: TestContext.Current.CancellationToken);

      string text = Encoding.UTF8.GetString(stream.ToArray());
      Assert.Equal("[1]", text);
   }

   [Fact]
   public async Task ToStreamAsync_EmitsProgressEvents()
   {
      var eventStream = new global::Afrowave.AJIS.Core.Events.AjisEventStream();
      await using var stream = new MemoryStream();
      var segments = GetSegments();
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         EventSink = eventStream
      };

      await AjisSerialize.ToStreamAsync(stream, segments, settings, TestContext.Current.CancellationToken);

      eventStream.Complete();

      var events = new List<global::Afrowave.AJIS.Core.Events.AjisEvent>();
      await foreach(var evt in eventStream.WithCancellation(TestContext.Current.CancellationToken))
         events.Add(evt);

      Assert.Contains(events, e => e is global::Afrowave.AJIS.Core.Events.AjisProgressEvent);
   }

   [Fact]
   public void ToText_RespectsNonCompactSettings()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Array, 0, 0),
         AjisSegment.Value(1, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(2, 1, AjisValueKind.Number, new AjisSliceUtf8("2"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Array, 3, 0)
      };

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Compact = false
         }
      };

      string text = AjisSerialize.ToText(segments, settings);

      Assert.Equal("[1, 2]", text);
   }

   [Fact]
   public void ToText_RespectsPrettySettings()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Array, 0, 0),
         AjisSegment.Value(1, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(2, 1, AjisValueKind.Number, new AjisSliceUtf8("2"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Array, 3, 0)
      };

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Pretty = true,
            IndentSize = 2
         }
      };

      string text = AjisSerialize.ToText(segments, settings);

      string expected = string.Join(Environment.NewLine,
         "[",
         "  1,",
         "  2",
         "]");

      Assert.Equal(expected, text);
   }

   [Fact]
   public void ToText_RespectsCanonicalOrdering()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("b"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(5, 1, AjisValueKind.Number, new AjisSliceUtf8("2"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Name(7, 1, new AjisSliceUtf8("a"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(11, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 12, 0)
      };

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Canonicalize = true
         }
      };

      string text = AjisSerialize.ToText(segments, settings);

      Assert.Equal("{\"a\":1,\"b\":2}", text);
   }

   private static async IAsyncEnumerable<AjisSegment> GetSegments()
   {
      yield return AjisSegment.Enter(AjisContainerKind.Array, 0, 0);
      yield return AjisSegment.Value(1, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None));
      yield return AjisSegment.Exit(AjisContainerKind.Array, 2, 0);
      await Task.CompletedTask;
   }
}
