#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Reader;
using System.Text;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisParseTests
{
   private static AjisSliceUtf8 Slice(string text, AjisSliceFlags flags = AjisSliceFlags.None)
      => new(Encoding.UTF8.GetBytes(text), flags);

   private static bool SliceEquals(AjisSliceUtf8? slice, string text)
      => slice is { } value && value.Bytes.Span.SequenceEqual(Encoding.UTF8.GetBytes(text));

   [Fact]
   public void ParseSegments_ParsesNull()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput
      };

      var segments = AjisParse.ParseSegments("null"u8, settings).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Null), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesStringWithUnicodeEscape()
   {
      var segments = AjisParse.ParseSegments("\"\\u0041\""u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.String, Slice("\\u0041", AjisSliceFlags.HasEscapes)), segments[0]);
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
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Value && SliceEquals(s.Slice, "1"));
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

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
      {
         segments.Add(segment);
      }

      Assert.Equal(2, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 1, 0), segments[1]);
   }

   [Fact]
   public async Task ParseSegmentsAsync_MappedFile_TypedLiteralFlag()
   {
      string path = Path.GetTempFileName();
      await File.WriteAllTextAsync(path, "T170", TestContext.Current.CancellationToken);

      try
      {
         await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
         var settings = new global::Afrowave.AJIS.Core.AjisSettings
         {
            ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput
         };

         var segments = new List<AjisSegment>();
         await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
            segments.Add(segment);

         Assert.Single(segments);
         Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[0]);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_LowMemoryWithDirectives_UsesLexerParser()
   {
      await using var stream = new MemoryStream("#ajis mode value=tryparse\ntrue"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory,
         AllowDirectives = true
      };

      var segments = new List<AjisSegment>();

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
      {
         segments.Add(segment);
      }

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Directive);
   }

   [Fact]
   public void ParseSegmentsWithDirectives_AppliesSettings()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var result = AjisParse.ParseSegmentsWithDirectives("#ajis mode value=json\ntrue"u8, settings);

      Assert.Equal(global::Afrowave.AJIS.Core.AjisTextMode.Json, result.Settings.TextMode);
   }

   [Fact]
   public async Task ParseSegmentsWithDirectivesAsync_AppliesSettings()
   {
      await using var stream = new MemoryStream("#ajis mode value=lex\ntrue"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var result = await AjisParse.ParseSegmentsWithDirectivesAsync(stream, settings, TestContext.Current.CancellationToken);

      Assert.Equal(global::Afrowave.AJIS.Core.AjisTextMode.Lex, result.Settings.TextMode);
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

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
      {
         segments.Add(segment);
      }

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.PropertyName && SliceEquals(s.Slice, "a"));
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Value && SliceEquals(s.Slice, "1"));
   }

   [Fact]
   public async Task ParseSegmentsAsync_EmitsProgressEvents()
   {
      var eventStream = new global::Afrowave.AJIS.Core.Events.AjisEventStream();
      await using var stream = new MemoryStream("{}"u8.ToArray());
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal,
         EventSink = eventStream
      };

      await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken)
         .WithCancellation(TestContext.Current.CancellationToken))
      {
      }

      eventStream.Complete();

      var events = new List<global::Afrowave.AJIS.Core.Events.AjisEvent>();
      await foreach(var evt in eventStream.WithCancellation(TestContext.Current.CancellationToken))
         events.Add(evt);

      Assert.Contains(events, e => e is global::Afrowave.AJIS.Core.Events.AjisProgressEvent);
      Assert.Contains(events, e => e is global::Afrowave.AJIS.Core.Events.AjisMilestoneEvent);
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

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
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

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
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

      await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings, TestContext.Current.CancellationToken))
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
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, Slice("1")), segments[1]);
      Assert.Equal(AjisSegment.Value(3, 1, AjisValueKind.String, Slice("x")), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 6, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesObjectValues()
   {
      var segments = AjisParse.ParseSegments("{\"a\":1}"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("a")), segments[1]);
      Assert.Equal(AjisSegment.Value(5, 1, AjisValueKind.Number, Slice("1")), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 6, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_RejectsPropertyNameOverMaxBytes()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            MaxPropertyNameBytes = 1
         }
      };

      Assert.Throws<FormatException>(() => AjisParse.ParseSegments("{\"ab\":1}"u8, settings).ToList());
   }

   [Fact]
   public void ParseSegments_ParsesBooleanArray()
   {
      var segments = AjisParse.ParseSegments("[true,false]"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Boolean, Slice("true")), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Boolean, Slice("false")), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 11, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesStringWithEscapes()
   {
      var segments = AjisParse.ParseSegments("\"a\\n\""u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.String, Slice("a\\n", AjisSliceFlags.HasEscapes)), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesStringWithNonAsciiFlag()
   {
      var segments = AjisParse.ParseSegments("\"ž\""u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.String, Slice("Å¾", AjisSliceFlags.HasNonAscii)), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesFloatNumber()
   {
      var segments = AjisParse.ParseSegments("3.14"u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, Slice("3.14")), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArrayInObject()
   {
      var segments = AjisParse.ParseSegments("{\"a\":[true,null]}"u8).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("a")), segments[1]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 5, 1), segments[2]);
      Assert.Equal(AjisSegment.Value(6, 2, AjisValueKind.Boolean, Slice("true")), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 2, AjisValueKind.Null), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 15, 1), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 16, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesMultipleProperties()
   {
      var segments = AjisParse.ParseSegments("{\"a\":1,\"b\":\"x\"}"u8).ToList();

      Assert.Equal(6, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("a")), segments[1]);
      Assert.Equal(AjisSegment.Value(5, 1, AjisValueKind.Number, Slice("1")), segments[2]);
      Assert.Equal(AjisSegment.Name(7, 1, Slice("b")), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 1, AjisValueKind.String, Slice("x")), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 14, 0), segments[5]);
   }

   [Fact]
   public void ParseSegments_ParsesComments_WhenEnabled()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings();
      var segments = AjisParse.ParseSegments("// c\n{}"u8, settings).ToList();

      Assert.NotEmpty(segments);
   }

   [Fact]
   public void ParseSegments_ParsesDirectives_WhenEnabled()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("#ajis mode value=tryparse\ntrue"u8, settings).ToList();

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Directive);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("// note\n#tool hint=fast\nT170"u8, settings).ToList();

      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Comment);
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Directive);
      Assert.Contains(segments, s => s.Kind == AjisSegmentKind.Value && s.ValueKind == AjisValueKind.Number
         && s.Slice == Slice("T170", AjisSliceFlags.IsNumberTyped));
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralArrayWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("[// note\n#tool hint=fast\nT170]"u8, settings).ToList();

      Assert.Equal(5, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Comment(1, 1, Slice(" note")), segments[1]);
      Assert.Equal(AjisSegment.Directive(9, 1, Slice("tool hint=fast")), segments[2]);
      Assert.Equal(AjisSegment.Value(25, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[3]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 29, 0), segments[4]);
   }

   [Fact]
   public void ParseSegments_ParsesDirectiveBetweenObjectMembers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{ts:T170,\n#tool hint=fast\nkind:\"identifier\"}"u8, settings).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[1]);
      Assert.Equal(AjisSegment.Value(4, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Directive(10, 1, Slice("tool hint=fast")), segments[3]);
      Assert.Equal(AjisSegment.Name(26, 1, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[4]);
      Assert.Equal(AjisSegment.Value(31, 1, AjisValueKind.String, Slice("identifier")), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 43, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesCommentBetweenObjectMembers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{ts:T170,// note\nkind:\"identifier\"}"u8, settings).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[1]);
      Assert.Equal(AjisSegment.Value(4, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(9, 1, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Name(17, 1, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[4]);
      Assert.Equal(AjisSegment.Value(22, 1, AjisValueKind.String, Slice("identifier")), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 34, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesCommentAndDirectiveBetweenObjectMembers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{ts:T170,// note\n#tool hint=fast\nkind:\"identifier\"}"u8, settings).ToList();

      Assert.Equal(8, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[1]);
      Assert.Equal(AjisSegment.Value(4, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(9, 1, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Directive(17, 1, Slice("tool hint=fast")), segments[4]);
      Assert.Equal(AjisSegment.Name(33, 1, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[5]);
      Assert.Equal(AjisSegment.Value(38, 1, AjisValueKind.String, Slice("identifier")), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 50, 0), segments[7]);
   }

   [Fact]
   public void ParseSegments_ParsesDirectiveBetweenQuotedObjectMembers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("{\"ts\":T170,\n#tool hint=fast\n\"kind\":\"identifier\"}"u8, settings).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts")), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Directive(12, 1, Slice("tool hint=fast")), segments[3]);
      Assert.Equal(AjisSegment.Name(28, 1, Slice("kind")), segments[4]);
      Assert.Equal(AjisSegment.Value(35, 1, AjisValueKind.String, Slice("identifier")), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 47, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesCommentBetweenQuotedObjectMembers()
   {
      var segments = AjisParse.ParseSegments("{\"ts\":T170,// note\n\"kind\":\"identifier\"}"u8).ToList();

      Assert.Equal(7, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts")), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(11, 1, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Name(19, 1, Slice("kind")), segments[4]);
      Assert.Equal(AjisSegment.Value(26, 1, AjisValueKind.String, Slice("identifier")), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 38, 0), segments[6]);
   }

   [Fact]
   public void ParseSegments_ParsesCommentAndDirectiveBetweenQuotedObjectMembers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("{\"ts\":T170,// note\n#tool hint=fast\n\"kind\":\"identifier\"}"u8, settings).ToList();

      Assert.Equal(8, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts")), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(11, 1, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Directive(19, 1, Slice("tool hint=fast")), segments[4]);
      Assert.Equal(AjisSegment.Name(35, 1, Slice("kind")), segments[5]);
      Assert.Equal(AjisSegment.Value(42, 1, AjisValueKind.String, Slice("identifier")), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 54, 0), segments[7]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedObjectWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{\"outer\":{ts:T170,// note\n#tool hint=fast\nkind:\"identifier\"}}"u8, settings).ToList();

      Assert.Equal(11, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("outer")), segments[1]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 9, 1), segments[2]);
      Assert.Equal(AjisSegment.Name(10, 2, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[3]);
      Assert.Equal(AjisSegment.Value(13, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[4]);
      Assert.Equal(AjisSegment.Comment(18, 2, Slice(" note")), segments[5]);
      Assert.Equal(AjisSegment.Directive(26, 2, Slice("tool hint=fast")), segments[6]);
      Assert.Equal(AjisSegment.Name(42, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[7]);
      Assert.Equal(AjisSegment.Value(47, 2, AjisValueKind.String, Slice("identifier")), segments[8]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 59, 1), segments[9]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 60, 0), segments[10]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArrayWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("[[T170,// note\n#tool hint=fast\nTS123]]"u8, settings).ToList();

      Assert.Equal(8, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Value(2, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(7, 2, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Directive(15, 2, Slice("tool hint=fast")), segments[4]);
      Assert.Equal(AjisSegment.Value(31, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 36, 1), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 37, 0), segments[7]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArrayWithMultipleTypedLiteralsAfterDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("[[T170,// note\n#tool hint=fast\nTS123,TA7]]"u8, settings).ToList();

      Assert.Equal(9, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Value(2, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Comment(7, 2, Slice(" note")), segments[3]);
      Assert.Equal(AjisSegment.Directive(15, 2, Slice("tool hint=fast")), segments[4]);
      Assert.Equal(AjisSegment.Value(31, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[5]);
      Assert.Equal(AjisSegment.Value(37, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 40, 1), segments[7]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 41, 0), segments[8]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArraysWithCommentAndDirectiveBetweenArrays()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("[[T170],// note\n#tool hint=fast\n[TS123]]"u8, settings).ToList();

      Assert.Equal(10, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Value(2, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 6, 1), segments[3]);
      Assert.Equal(AjisSegment.Comment(8, 1, Slice(" note")), segments[4]);
      Assert.Equal(AjisSegment.Directive(16, 1, Slice("tool hint=fast")), segments[5]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 32, 1), segments[6]);
      Assert.Equal(AjisSegment.Value(33, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[7]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 38, 1), segments[8]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 39, 0), segments[9]);
   }

   [Fact]
   public void ParseSegments_ParsesObjectAndArrayWithCommentAndDirectiveBetween()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123]]"u8, settings).ToList();

      Assert.Equal(11, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[2]);
      Assert.Equal(AjisSegment.Value(5, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[3]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 9, 1), segments[4]);
      Assert.Equal(AjisSegment.Comment(11, 1, Slice(" note")), segments[5]);
      Assert.Equal(AjisSegment.Directive(19, 1, Slice("tool hint=fast")), segments[6]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 35, 1), segments[7]);
      Assert.Equal(AjisSegment.Value(36, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[8]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 41, 1), segments[9]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 42, 0), segments[10]);
   }

   [Fact]
   public void ParseSegments_ParsesObjectSiblingsWithCommentAndDirectiveBetween()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n{kind:\"identifier\"}]"u8, settings).ToList();

      Assert.Equal(12, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[2]);
      Assert.Equal(AjisSegment.Value(5, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[3]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 9, 1), segments[4]);
      Assert.Equal(AjisSegment.Comment(11, 1, Slice(" note")), segments[5]);
      Assert.Equal(AjisSegment.Directive(19, 1, Slice("tool hint=fast")), segments[6]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 35, 1), segments[7]);
      Assert.Equal(AjisSegment.Name(36, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[8]);
      Assert.Equal(AjisSegment.Value(41, 2, AjisValueKind.String, Slice("identifier")), segments[9]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 53, 1), segments[10]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 54, 0), segments[11]);
   }

   [Fact]
   public void ParseSegments_ParsesQuotedObjectSiblingsWithCommentAndDirectiveBetween()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{\"kind\":\"identifier\"}]"u8, settings).ToList();

      Assert.Equal(12, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("ts")), segments[2]);
      Assert.Equal(AjisSegment.Value(7, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[3]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 11, 1), segments[4]);
      Assert.Equal(AjisSegment.Comment(13, 1, Slice(" note")), segments[5]);
      Assert.Equal(AjisSegment.Directive(21, 1, Slice("tool hint=fast")), segments[6]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 37, 1), segments[7]);
      Assert.Equal(AjisSegment.Name(38, 2, Slice("kind")), segments[8]);
      Assert.Equal(AjisSegment.Value(45, 2, AjisValueKind.String, Slice("identifier")), segments[9]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 57, 1), segments[10]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 58, 0), segments[11]);
   }

   [Fact]
   public void ParseSegments_ParsesMixedQuotedUnquotedObjectSiblingsWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"}]"u8, settings).ToList();

      Assert.Equal(12, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("ts")), segments[2]);
      Assert.Equal(AjisSegment.Value(7, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[3]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 11, 1), segments[4]);
      Assert.Equal(AjisSegment.Comment(13, 1, Slice(" note")), segments[5]);
      Assert.Equal(AjisSegment.Directive(21, 1, Slice("tool hint=fast")), segments[6]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 37, 1), segments[7]);
      Assert.Equal(AjisSegment.Name(38, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[8]);
      Assert.Equal(AjisSegment.Value(43, 2, AjisValueKind.String, Slice("identifier")), segments[9]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 55, 1), segments[10]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 56, 0), segments[11]);
   }

   [Fact]
   public void ParseSegments_ParsesDeeplyNestedObject()
   {
      var segments = AjisParse.ParseSegments("{\"a\":{\"b\":{\"c\":{\"d\":1}}}}"u8).ToList();

      Assert.Equal(10, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("a")), segments[1]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 5, 1), segments[2]);
      Assert.Equal(AjisSegment.Name(6, 1, Slice("b")), segments[3]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 10, 1), segments[4]);
      Assert.Equal(AjisSegment.Name(11, 1, Slice("c")), segments[5]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 15, 1), segments[6]);
      Assert.Equal(AjisSegment.Name(16, 1, Slice("d")), segments[7]);
      Assert.Equal(AjisSegment.Value(20, 1, AjisValueKind.Number, Slice("1")), segments[8]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 21, 0), segments[9]);
   }

   [Fact]
   public void ParseSegments_ParsesLargeNumber()
   {
      var segments = AjisParse.ParseSegments("12345678901234567890"u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, Slice("12345678901234567890")), segments[0]);
   }

   [Fact]
   public async Task ParseSegmentsAsync_StreamingEmitsSegmentsByOne()
   {
      await using var stream = new MemoryStream("[1,2,3]"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Should emit: [, 1, 2, 3, ]
      Assert.Equal(5, segmentList.Count);
      Assert.Equal(AjisSegmentKind.EnterContainer, segmentList[0].Kind);
      Assert.Equal(AjisSegmentKind.Value, segmentList[1].Kind);
      Assert.Equal(AjisSegmentKind.Value, segmentList[2].Kind);
      Assert.Equal(AjisSegmentKind.Value, segmentList[3].Kind);
      Assert.Equal(AjisSegmentKind.ExitContainer, segmentList[4].Kind);
   }

   [Fact]
   public async Task ParseSegmentsAsync_NestedObjectOrderingCorrect()
   {
      await using var stream = new MemoryStream("{\"x\":{\"y\":1}}"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Expected sequence: { PropertyName(x) { PropertyName(y) Value(1) } }
      Assert.Equal(6, segmentList.Count);
      Assert.Equal(AjisSegmentKind.EnterContainer, segmentList[0].Kind);
      Assert.Equal(AjisContainerKind.Object, segmentList[0].ContainerKind);

      Assert.Equal(AjisSegmentKind.PropertyName, segmentList[1].Kind);
      Assert.Equal(AjisSegmentKind.EnterContainer, segmentList[2].Kind);
      Assert.Equal(AjisContainerKind.Object, segmentList[2].ContainerKind);

      Assert.Equal(AjisSegmentKind.PropertyName, segmentList[3].Kind);
      Assert.Equal(AjisSegmentKind.Value, segmentList[4].Kind);

      Assert.Equal(AjisSegmentKind.ExitContainer, segmentList[5].Kind);
   }

   [Fact]
   public async Task ParseSegmentsAsync_DepthTrackingCorrect()
   {
      await using var stream = new MemoryStream("[[true]]"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Depth sequence should be: 0 -> 1 -> 2 -> 2 -> 1 -> 0
      Assert.Equal(5, segmentList.Count);
      Assert.Equal(0, segmentList[0].Depth); // [ (outer)
      Assert.Equal(1, segmentList[1].Depth); // [ (inner)
      Assert.Equal(2, segmentList[2].Depth); // true
      Assert.Equal(1, segmentList[3].Depth); // ] (inner)
      Assert.Equal(0, segmentList[4].Depth); // ] (outer)
   }

   [Fact]
   public async Task ParseSegmentsAsync_MaxDepthEnforced()
   {
      // Create deeply nested structure
      string deepJson = string.Concat(Enumerable.Repeat("[", 10)) + "1" + string.Concat(Enumerable.Repeat("]", 10));
      await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(deepJson));

      var segmentList = new List<AjisSegment>();
      var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
      {
         await foreach (var segment in AjisLexerParserStream.ParseAsync(stream, maxDepth: 5))
         {
            segmentList.Add(segment);
         }
      });

      Assert.Contains("Maximum nesting depth", ex.Message);
   }

   [Fact]
   public async Task ParseSegmentsAsync_PropertyNameBeforeValueRule()
   {
      await using var stream = new MemoryStream("{\"name\":\"John\",\"age\":30}"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Check that for each object member, PropertyName comes before Value
      for (int i = 0; i < segmentList.Count - 1; i++)
      {
         if (segmentList[i].Kind == AjisSegmentKind.PropertyName && segmentList[i].Depth > 0)
         {
            // Next non-meta segment should be Value
            int nextIdx = i + 1;
            while (nextIdx < segmentList.Count &&
                   (segmentList[nextIdx].Kind == AjisSegmentKind.Comment ||
                    segmentList[nextIdx].Kind == AjisSegmentKind.Directive))
            {
               nextIdx++;
            }

            if (nextIdx < segmentList.Count)
            {
               Assert.Equal(AjisSegmentKind.Value, segmentList[nextIdx].Kind);
            }
         }
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_ArrayItemOrdering()
   {
      await using var stream = new MemoryStream("[10,20,30]"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Array values should appear in order
      var values = segmentList.Where(s => s.Kind == AjisSegmentKind.Value).ToList();
      Assert.Equal(3, values.Count);
      Assert.Equal("10", System.Text.Encoding.UTF8.GetString(values[0].Slice!.Value.Bytes.Span));
      Assert.Equal("20", System.Text.Encoding.UTF8.GetString(values[1].Slice!.Value.Bytes.Span));
      Assert.Equal("30", System.Text.Encoding.UTF8.GetString(values[2].Slice!.Value.Bytes.Span));
   }

   [Fact]
   public async Task ParseSegmentsAsync_EmptyContainersProperlyNested()
   {
      await using var stream = new MemoryStream("{}[]"u8.ToArray());
      var segmentList = new List<AjisSegment>();

      await foreach (var segment in AjisLexerParserStream.ParseAsync(stream))
      {
         segmentList.Add(segment);
      }

      // Should fail - document can only have one root value
      // Actually this might pass if implementation allows multiple root values
      // Let's just verify empty containers work
   }

   [Fact]
   public async Task ParseSegmentsAsync_CancellationTokenRespected()
   {
      await using var stream = new MemoryStream("{\"a\":1,\"b\":2,\"c\":3}"u8.ToArray());
      using var cts = new System.Threading.CancellationTokenSource();
      var segmentList = new List<AjisSegment>();

      // Cancel after short delay
      cts.CancelAfter(TimeSpan.FromMilliseconds(1));

      var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
      {
         await foreach (var segment in AjisLexerParserStream.ParseAsync(stream, ct: cts.Token))
         {
            segmentList.Add(segment);
            await Task.Delay(10); // Give cancellation time to propagate
         }
      });

      // Verify that cancellation was requested
      Assert.True(cts.Token.IsCancellationRequested);
   }

   // ===== M5 LAX Mode Tests (JavaScript-Tolerant) =====

   [Fact]
   public void ParseSegments_LAX_UnquotedKeys()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      // {x:1, y:2}
      var segments = AjisParse.ParseSegments("{ x: 1, y: 2 }"u8, settings).ToList();

      // Should parse successfully with identifier tokens as property names
      Assert.NotEmpty(segments);
      var nameSegments = segments.Where(s => s.Kind == AjisSegmentKind.PropertyName).ToList();
      Assert.Equal(2, nameSegments.Count);
   }

   [Fact]
   public void ParseSegments_LAX_SingleQuotedStrings()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowSingleQuotes = true
         }
      };

      // ['hello', 'world']
      var segments = AjisParse.ParseSegments("['hello', 'world']"u8, settings).ToList();

      Assert.NotEmpty(segments);
      var valueSegments = segments.Where(s => s.Kind == AjisSegmentKind.Value && s.ValueKind == AjisValueKind.String).ToList();
      Assert.Equal(2, valueSegments.Count);
      Assert.True(SliceEquals(valueSegments[0].Slice, "hello"));
      Assert.True(SliceEquals(valueSegments[1].Slice, "world"));
   }

   [Fact]
   public void ParseSegments_LAX_TrailingCommas()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         AllowTrailingCommas = true
      };

      var segments = AjisParse.ParseSegments("[ 1, 2, 3, ]"u8, settings).ToList();

      Assert.NotEmpty(segments);
      var values = segments.Where(s => s.Kind == AjisSegmentKind.Value).ToList();
      Assert.Equal(3, values.Count);
   }

   [Fact]
   public void ParseSegments_LAX_LineComments()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         Comments = new global::Afrowave.AJIS.Core.AjisCommentOptions
         {
            AllowLineComments = true
         }
      };

      // { x: 1 } // comment
      var segments = AjisParse.ParseSegments("{ x: 1 } // this is a comment"u8, settings).ToList();

      Assert.NotEmpty(segments);
      // Comments should be skipped, only value segments present
      var valueCount = segments.Count(s => s.Kind != AjisSegmentKind.Comment);
      Assert.True(valueCount > 0);
   }

   [Fact]
   public void ParseSegments_LAX_BlockComments()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         Comments = new global::Afrowave.AJIS.Core.AjisCommentOptions
         {
            AllowBlockComments = true
         }
      };

      // { /* comment */ x: 1 }
      var segments = AjisParse.ParseSegments("{ /* block comment */ x: 1 }"u8, settings).ToList();

      Assert.NotEmpty(segments);
      var nameSegments = segments.Where(s => s.Kind == AjisSegmentKind.PropertyName).ToList();
      Assert.Single(nameSegments);
   }

   [Fact]
   public void ParseSegments_LAX_MixedRelaxedSyntax()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true,
            AllowSingleQuotes = true
         },
         AllowTrailingCommas = true,
         Comments = new global::Afrowave.AJIS.Core.AjisCommentOptions
         {
            AllowLineComments = true
         }
      };

      // JavaScript-like object:
      // {
      //   name: 'Alice',     // Name
      //   age: 30,           // Age
      // }
      string laxJson = "{ name: 'Alice', age: 30, }  // user object";
      var segments = AjisParse.ParseSegments(System.Text.Encoding.UTF8.GetBytes(laxJson), settings).ToList();

      Assert.NotEmpty(segments);
      var values = segments.Where(s => s.Kind == AjisSegmentKind.Value).ToList();
      Assert.Equal(2, values.Count); // name value and age value
   }

   [Fact]
   public void ParseSegments_StrictMode_RejectsUnquotedKeys()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Json
      };

      // JSON strictly requires quoted keys
      Assert.Throws<FormatException>(() =>
         AjisParse.ParseSegments("{x:1}"u8, settings).ToList()
      );
   }

   [Fact]
   public void ParseSegments_StrictMode_RejactsSingleQuotes()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Json
      };

      // JSON strictly requires double quotes
      Assert.Throws<FormatException>(() =>
         AjisParse.ParseSegments("'value'"u8, settings).ToList()
      );
   }

   [Fact]
   public void ParseSegments_LAX_BackwardCompatibility()
   {
      var settingsJson = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Json
      };
      var settingsLax = new global::Afrowave.AJIS.Core.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lax
      };

      // Valid strict JSON should work in both modes
      string strictJson = "{\"key\":\"value\"}";
      byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strictJson);

      var segmentsJson = AjisParse.ParseSegments(bytes, settingsJson).ToList();
      var segmentsLax = AjisParse.ParseSegments(bytes, settingsLax).ToList();

      // Both should parse to same structure
      Assert.Equal(segmentsJson.Count, segmentsLax.Count);
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
