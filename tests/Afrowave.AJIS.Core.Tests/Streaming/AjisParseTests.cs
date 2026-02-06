#nullable enable

using Afrowave.AJIS.Streaming.Segments;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisParseTests
{
   [Fact]
   public void ParseSegments_ParsesNull()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput
      };

      var segments = AjisParse.ParseSegments("null"u8, settings).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Null, null), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesStringWithUnicodeEscape()
   {
      var segments = AjisParse.ParseSegments("\"\\u0041\""u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.String, "A"), segments[0]);
   }

   [Fact]
   public void ParseSegments_AllowsTrailingCommas_WhenEnabled()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowTrailingCommas = true
      };

      var segments = AjisParse.ParseSegments("{\"a\":1,}"u8, settings).ToList();

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.PropertyName);
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Value && s.Text == "1");
   }

   [Fact]
   public void ParseSegments_RejectsTrailingCommas_WhenDisabled()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowTrailingCommas = false,
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Ajis
      };

      Assert.Throws<FormatException>(() => AjisParse.ParseSegments("{\"a\":1,}"u8, settings).ToList());
   }

   [Fact]
   public async Task ParseSegmentsAsync_ParsesEmptyObject()
   {
      await using var stream = new MemoryStream("{}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory
      };
      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
      {
         segments.Add(segment);
      }

      Assert.Equal(2, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 1, 0), segments[1]);
   }

   [Fact]
   public async Task ParseSegmentsAsync_UniversalProfile_UsesLexerParser()
   {
      await using var stream = new MemoryStream("{\"a\":1}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal
      };

      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
      {
         segments.Add(segment);
      }

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.PropertyName && s.Text == "a");
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Value && s.Text == "1");
   }

   [Fact]
   public async Task ParseSegmentsAsync_UniversalWithoutBuffer_UsesMappedFile()
   {
      await using var stream = new NonBufferStream("{}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal
      };

      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
      {
         segments.Add(segment);
      }

      Assert.Equal(2, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 1, 0), segments[1]);
   }

   [Fact]
   public async Task ParseSegmentsAsync_HighThroughputWithoutBuffer_UsesMappedFile()
   {
      await using var stream = new NonBufferStream("{}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput
      };

      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
      {
         segments.Add(segment);
      }

      Assert.Equal(2, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 1, 0), segments[1]);
   }

   [Fact]
   public async Task ParseSegmentsAsync_LowMemoryWithoutBuffer_UsesMappedFile()
   {
      await using var stream = new NonBufferStream("{}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory
      };

      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
      {
         segments.Add(segment);
      }

      Assert.Equal(2, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 1, 0), segments[1]);
   }

   [Fact]
   public void ParseSegments_ParsesArrayValues()
   {
      var segments = AjisParse.ParseSegments("[1,\"x\"]"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, "1"), segments[1]);
      Assert.Equal(AjisSegment.Value(3, 1, AjisValueKind.String, "x"), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 6, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesObjectValues()
   {
      var segments = AjisParse.ParseSegments("{\"a\":1}"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, "a"), segments[1]);
      Assert.Equal(AjisSegment.Value(5, 1, AjisValueKind.Number, "1"), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 6, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesBooleanArray()
   {
      var segments = AjisParse.ParseSegments("[true,false]"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Boolean, "true"), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Boolean, "false"), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 11, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesStringWithEscapes()
   {
      var segments = AjisParse.ParseSegments("\"a\\n\""u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.String, "a\\n"), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesFloatNumber()
   {
      var segments = AjisParse.ParseSegments("3.14"u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, "3.14"), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArrayInObject()
   {
      var segments = AjisParse.ParseSegments("{\"a\":[true,null]}"u8).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, "a"), segments[1]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 5, 1), segments[2]);
      Assert.Equal(AjisSegment.Value(6, 2, AjisValueKind.Boolean, "true"), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 2, AjisValueKind.Null, null), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 15, 1), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 16, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesMultipleProperties()
   {
      var segments = AjisParse.ParseSegments("{\"a\":1,\"b\":\"x\"}"u8).ToList();

      Assert.Equal(6, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, "a"), segments[1]);
      Assert.Equal(AjisSegment.Value(5, 1, AjisValueKind.Number, "1"), segments[2]);
      Assert.Equal(AjisSegment.Name(7, 1, "b"), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 1, AjisValueKind.String, "x"), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 14, 0), segments[5]);
   }

   [Fact(Skip = "AJIS extensions not implemented yet (comments/directives)")]
   public void ParseSegments_ParsesComments_WhenEnabled()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings();
      var segments = AjisParse.ParseSegments("// c\n{}"u8, settings).ToList();

      Assert.NotEmpty(segments);
   }

   [Fact(Skip = "AJIS extensions not implemented yet (hex/binary numbers)")]
   public void ParseSegments_ParsesPrefixedNumbers()
   {
      var segments = AjisParse.ParseSegments("[0xFF,0b1010]"u8).ToList();

      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, "0xFF"), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, "0b1010"), segments[2]);
   }

   private sealed class NonBufferStream(byte[] data) : Stream
   {
      private readonly MemoryStream _inner = new(data);

      public override bool CanRead => true;
      public override bool CanSeek => false;
      public override bool CanWrite => false;
      public override long Length => _inner.Length;
      public override long Position
      {
         get => _inner.Position;
         set => throw new NotSupportedException();
      }

      public override void Flush() => _inner.Flush();
      public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
      public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
      public override void SetLength(long value) => throw new NotSupportedException();
      public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

      public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
         => _inner.ReadAsync(buffer, cancellationToken);

      protected override void Dispose(bool disposing)
      {
         if(disposing) _inner.Dispose();
         base.Dispose(disposing);
      }
   }
}
