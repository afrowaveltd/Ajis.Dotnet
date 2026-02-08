#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
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
   public void ParseSegments_ParsesDeepMixedNestingWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"outer\":{ts:T170}},// note\n#tool hint=fast\n[{kind:\"identifier\"}]]"u8, settings).ToList();

      Assert.Equal(17, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("outer")), segments[2]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 10, 2), segments[3]);
      Assert.Equal(AjisSegment.Name(11, 3, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[4]);
      Assert.Equal(AjisSegment.Value(14, 3, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 18, 2), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 19, 1), segments[7]);
      Assert.Equal(AjisSegment.Comment(21, 1, Slice(" note")), segments[8]);
      Assert.Equal(AjisSegment.Directive(29, 1, Slice("tool hint=fast")), segments[9]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 45, 1), segments[10]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 46, 2), segments[11]);
      Assert.Equal(AjisSegment.Name(47, 3, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[12]);
      Assert.Equal(AjisSegment.Value(52, 3, AjisValueKind.String, Slice("identifier")), segments[13]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 64, 2), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 65, 1), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 66, 0), segments[16]);
   }

   [Fact]
   public void ParseSegments_ParsesMixedDepthWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"outer\":[T170]},// note\n#tool hint=fast\n{inner:{\"value\":TS123}}]"u8, settings).ToList();

      Assert.Equal(17, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("outer")), segments[2]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 10, 2), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 3, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 15, 2), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 16, 1), segments[6]);
      Assert.Equal(AjisSegment.Comment(18, 1, Slice(" note")), segments[7]);
      Assert.Equal(AjisSegment.Directive(26, 1, Slice("tool hint=fast")), segments[8]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 42, 1), segments[9]);
      Assert.Equal(AjisSegment.Name(43, 2, Slice("inner", AjisSliceFlags.IsIdentifierStyle)), segments[10]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 49, 2), segments[11]);
      Assert.Equal(AjisSegment.Name(50, 3, Slice("value")), segments[12]);
      Assert.Equal(AjisSegment.Value(58, 3, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[13]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 63, 2), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 64, 1), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 65, 0), segments[16]);
   }

   [Fact]
   public void ParseSegments_ParsesThreeSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123],// more\n#tool hint=slow\n{kind:\"identifier\"}]"u8, settings).ToList();

      Assert.Equal(17, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(43, 1, Slice(" more")), segments[10]);
      Assert.Equal(AjisSegment.Directive(51, 1, Slice("tool hint=slow")), segments[11]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 67, 1), segments[12]);
      Assert.Equal(AjisSegment.Name(68, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[13]);
      Assert.Equal(AjisSegment.Value(73, 2, AjisValueKind.String, Slice("identifier")), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 85, 1), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 86, 0), segments[16]);
   }

   [Fact]
   public void ParseSegments_ParsesThreeSiblingContainersWithMixedQuotedUnquotedNames()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n{\"name\":\"value\"}]"u8, settings).ToList();

      Assert.Equal(18, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Name(82, 2, Slice("name")), segments[14]);
      Assert.Equal(AjisSegment.Value(89, 2, AjisValueKind.String, Slice("value")), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 96, 1), segments[16]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 97, 0), segments[17]);
   }

   [Fact]
   public void ParseSegments_ParsesFourSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123],// more\n#tool hint=slow\n{kind:\"identifier\"},// last\n#tool hint=final\n{\"name\":\"value\"}]"u8, settings).ToList();

      Assert.Equal(23, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(43, 1, Slice(" more")), segments[10]);
      Assert.Equal(AjisSegment.Directive(51, 1, Slice("tool hint=slow")), segments[11]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 67, 1), segments[12]);
      Assert.Equal(AjisSegment.Name(68, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[13]);
      Assert.Equal(AjisSegment.Value(73, 2, AjisValueKind.String, Slice("identifier")), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 85, 1), segments[15]);
      Assert.Equal(AjisSegment.Comment(87, 1, Slice(" last")), segments[16]);
      Assert.Equal(AjisSegment.Directive(95, 1, Slice("tool hint=final")), segments[17]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 112, 1), segments[18]);
      Assert.Equal(AjisSegment.Name(113, 2, Slice("name")), segments[19]);
      Assert.Equal(AjisSegment.Value(120, 2, AjisValueKind.String, Slice("value")), segments[20]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 127, 1), segments[21]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 128, 0), segments[22]);
   }

   [Fact]
   public void ParseSegments_ParsesFourSiblingContainersWithMixedNamesAndNestedArray()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n[TS123,TA7],// last\n#tool hint=final\n{\"name\":\"value\"}]"u8, settings).ToList();

      Assert.Equal(24, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Value(82, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[14]);
      Assert.Equal(AjisSegment.Value(88, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 91, 1), segments[16]);
      Assert.Equal(AjisSegment.Comment(93, 1, Slice(" last")), segments[17]);
      Assert.Equal(AjisSegment.Directive(101, 1, Slice("tool hint=final")), segments[18]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 118, 1), segments[19]);
      Assert.Equal(AjisSegment.Name(119, 2, Slice("name")), segments[20]);
      Assert.Equal(AjisSegment.Value(126, 2, AjisValueKind.String, Slice("value")), segments[21]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 133, 1), segments[22]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 134, 0), segments[23]);
   }

   [Fact]
   public void ParseSegments_ParsesFiveSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123],// more\n#tool hint=slow\n{kind:\"identifier\"},// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TA7]]"u8, settings).ToList();

      Assert.Equal(28, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(43, 1, Slice(" more")), segments[10]);
      Assert.Equal(AjisSegment.Directive(51, 1, Slice("tool hint=slow")), segments[11]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 67, 1), segments[12]);
      Assert.Equal(AjisSegment.Name(68, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[13]);
      Assert.Equal(AjisSegment.Value(73, 2, AjisValueKind.String, Slice("identifier")), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 85, 1), segments[15]);
      Assert.Equal(AjisSegment.Comment(87, 1, Slice(" last")), segments[16]);
      Assert.Equal(AjisSegment.Directive(95, 1, Slice("tool hint=final")), segments[17]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 112, 1), segments[18]);
      Assert.Equal(AjisSegment.Name(113, 2, Slice("name")), segments[19]);
      Assert.Equal(AjisSegment.Value(120, 2, AjisValueKind.String, Slice("value")), segments[20]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 127, 1), segments[21]);
      Assert.Equal(AjisSegment.Comment(129, 1, Slice(" end")), segments[22]);
      Assert.Equal(AjisSegment.Directive(136, 1, Slice("tool hint=wrap")), segments[23]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 152, 1), segments[24]);
      Assert.Equal(AjisSegment.Value(153, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[25]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 156, 1), segments[26]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 157, 0), segments[27]);
   }

   [Fact]
   public void ParseSegments_ParsesFiveSiblingContainersWithMixedNamesAndNestedArrays()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n[TS123,TA7],// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TB9]]"u8, settings).ToList();

      Assert.Equal(29, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Value(82, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[14]);
      Assert.Equal(AjisSegment.Value(88, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 91, 1), segments[16]);
      Assert.Equal(AjisSegment.Comment(93, 1, Slice(" last")), segments[17]);
      Assert.Equal(AjisSegment.Directive(101, 1, Slice("tool hint=final")), segments[18]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 118, 1), segments[19]);
      Assert.Equal(AjisSegment.Name(119, 2, Slice("name")), segments[20]);
      Assert.Equal(AjisSegment.Value(126, 2, AjisValueKind.String, Slice("value")), segments[21]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 133, 1), segments[22]);
      Assert.Equal(AjisSegment.Comment(135, 1, Slice(" end")), segments[23]);
      Assert.Equal(AjisSegment.Directive(142, 1, Slice("tool hint=wrap")), segments[24]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 158, 1), segments[25]);
      Assert.Equal(AjisSegment.Value(159, 2, AjisValueKind.Number, Slice("TB9", AjisSliceFlags.IsNumberTyped)), segments[26]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 162, 1), segments[27]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 163, 0), segments[28]);
   }

   [Fact]
   public void ParseSegments_ParsesSixSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123],// more\n#tool hint=slow\n{kind:\"identifier\"},// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TA7],// tail\n#tool hint=done\n{tail:\"ok\"}]"u8, settings).ToList();

      Assert.Equal(34, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(43, 1, Slice(" more")), segments[10]);
      Assert.Equal(AjisSegment.Directive(51, 1, Slice("tool hint=slow")), segments[11]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 67, 1), segments[12]);
      Assert.Equal(AjisSegment.Name(68, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[13]);
      Assert.Equal(AjisSegment.Value(73, 2, AjisValueKind.String, Slice("identifier")), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 85, 1), segments[15]);
      Assert.Equal(AjisSegment.Comment(87, 1, Slice(" last")), segments[16]);
      Assert.Equal(AjisSegment.Directive(95, 1, Slice("tool hint=final")), segments[17]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 112, 1), segments[18]);
      Assert.Equal(AjisSegment.Name(113, 2, Slice("name")), segments[19]);
      Assert.Equal(AjisSegment.Value(120, 2, AjisValueKind.String, Slice("value")), segments[20]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 127, 1), segments[21]);
      Assert.Equal(AjisSegment.Comment(129, 1, Slice(" end")), segments[22]);
      Assert.Equal(AjisSegment.Directive(136, 1, Slice("tool hint=wrap")), segments[23]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 152, 1), segments[24]);
      Assert.Equal(AjisSegment.Value(153, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[25]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 156, 1), segments[26]);
      Assert.Equal(AjisSegment.Comment(158, 1, Slice(" tail")), segments[27]);
      Assert.Equal(AjisSegment.Directive(166, 1, Slice("tool hint=done")), segments[28]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 182, 1), segments[29]);
      Assert.Equal(AjisSegment.Name(183, 2, Slice("tail", AjisSliceFlags.IsIdentifierStyle)), segments[30]);
      Assert.Equal(AjisSegment.Value(188, 2, AjisValueKind.String, Slice("ok")), segments[31]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 192, 1), segments[32]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 193, 0), segments[33]);
   }

   [Fact]
   public void ParseSegments_ParsesSixSiblingContainersWithMixedNamesAndNestedArrays()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n[TS123,TA7],// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TB9],// tail\n#tool hint=done\n{tail:\"ok\"}]"u8, settings).ToList();

      Assert.Equal(35, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Value(82, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[14]);
      Assert.Equal(AjisSegment.Value(88, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 91, 1), segments[16]);
      Assert.Equal(AjisSegment.Comment(93, 1, Slice(" last")), segments[17]);
      Assert.Equal(AjisSegment.Directive(101, 1, Slice("tool hint=final")), segments[18]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 118, 1), segments[19]);
      Assert.Equal(AjisSegment.Name(119, 2, Slice("name")), segments[20]);
      Assert.Equal(AjisSegment.Value(126, 2, AjisValueKind.String, Slice("value")), segments[21]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 133, 1), segments[22]);
      Assert.Equal(AjisSegment.Comment(135, 1, Slice(" end")), segments[23]);
      Assert.Equal(AjisSegment.Directive(142, 1, Slice("tool hint=wrap")), segments[24]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 158, 1), segments[25]);
      Assert.Equal(AjisSegment.Value(159, 2, AjisValueKind.Number, Slice("TB9", AjisSliceFlags.IsNumberTyped)), segments[26]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 162, 1), segments[27]);
      Assert.Equal(AjisSegment.Comment(164, 1, Slice(" tail")), segments[28]);
      Assert.Equal(AjisSegment.Directive(172, 1, Slice("tool hint=done")), segments[29]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 188, 1), segments[30]);
      Assert.Equal(AjisSegment.Name(189, 2, Slice("tail", AjisSliceFlags.IsIdentifierStyle)), segments[31]);
      Assert.Equal(AjisSegment.Value(194, 2, AjisValueKind.String, Slice("ok")), segments[32]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 198, 1), segments[33]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 199, 0), segments[34]);
   }

   [Fact]
   public void ParseSegments_ParsesSevenSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{ts:T170},// note\n#tool hint=fast\n[TS123],// more\n#tool hint=slow\n{kind:\"identifier\"},// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TA7],// tail\n#tool hint=done\n{tail:\"ok\"},// done\n#tool hint=extra\n[TZ5]]"u8, settings).ToList();

      Assert.Equal(39, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(43, 1, Slice(" more")), segments[10]);
      Assert.Equal(AjisSegment.Directive(51, 1, Slice("tool hint=slow")), segments[11]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 67, 1), segments[12]);
      Assert.Equal(AjisSegment.Name(68, 2, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[13]);
      Assert.Equal(AjisSegment.Value(73, 2, AjisValueKind.String, Slice("identifier")), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 85, 1), segments[15]);
      Assert.Equal(AjisSegment.Comment(87, 1, Slice(" last")), segments[16]);
      Assert.Equal(AjisSegment.Directive(95, 1, Slice("tool hint=final")), segments[17]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 112, 1), segments[18]);
      Assert.Equal(AjisSegment.Name(113, 2, Slice("name")), segments[19]);
      Assert.Equal(AjisSegment.Value(120, 2, AjisValueKind.String, Slice("value")), segments[20]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 127, 1), segments[21]);
      Assert.Equal(AjisSegment.Comment(129, 1, Slice(" end")), segments[22]);
      Assert.Equal(AjisSegment.Directive(136, 1, Slice("tool hint=wrap")), segments[23]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 152, 1), segments[24]);
      Assert.Equal(AjisSegment.Value(153, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[25]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 156, 1), segments[26]);
      Assert.Equal(AjisSegment.Comment(158, 1, Slice(" tail")), segments[27]);
      Assert.Equal(AjisSegment.Directive(166, 1, Slice("tool hint=done")), segments[28]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 182, 1), segments[29]);
      Assert.Equal(AjisSegment.Name(183, 2, Slice("tail", AjisSliceFlags.IsIdentifierStyle)), segments[30]);
      Assert.Equal(AjisSegment.Value(188, 2, AjisValueKind.String, Slice("ok")), segments[31]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 192, 1), segments[32]);
      Assert.Equal(AjisSegment.Comment(194, 1, Slice(" done")), segments[33]);
      Assert.Equal(AjisSegment.Directive(202, 1, Slice("tool hint=extra")), segments[34]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 219, 1), segments[35]);
      Assert.Equal(AjisSegment.Value(220, 2, AjisValueKind.Number, Slice("TZ5", AjisSliceFlags.IsNumberTyped)), segments[36]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 223, 1), segments[37]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 224, 0), segments[38]);
   }

   [Fact]
   public void ParseSegments_ParsesSevenSiblingContainersWithMixedNamesAndNestedArrays()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n[TS123,TA7],// last\n#tool hint=final\n{\"name\":\"value\"},// end\n#tool hint=wrap\n[TB9],// tail\n#tool hint=done\n{tail:\"ok\"},// done\n#tool hint=extra\n[TZ5]]"u8, settings).ToList();

      Assert.Equal(40, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Value(82, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[14]);
      Assert.Equal(AjisSegment.Value(88, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 91, 1), segments[16]);
      Assert.Equal(AjisSegment.Comment(93, 1, Slice(" last")), segments[17]);
      Assert.Equal(AjisSegment.Directive(101, 1, Slice("tool hint=final")), segments[18]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 118, 1), segments[19]);
      Assert.Equal(AjisSegment.Name(119, 2, Slice("name")), segments[20]);
      Assert.Equal(AjisSegment.Value(126, 2, AjisValueKind.String, Slice("value")), segments[21]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 133, 1), segments[22]);
      Assert.Equal(AjisSegment.Comment(135, 1, Slice(" end")), segments[23]);
      Assert.Equal(AjisSegment.Directive(142, 1, Slice("tool hint=wrap")), segments[24]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 158, 1), segments[25]);
      Assert.Equal(AjisSegment.Value(159, 2, AjisValueKind.Number, Slice("TB9", AjisSliceFlags.IsNumberTyped)), segments[26]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 162, 1), segments[27]);
      Assert.Equal(AjisSegment.Comment(164, 1, Slice(" tail")), segments[28]);
      Assert.Equal(AjisSegment.Directive(172, 1, Slice("tool hint=done")), segments[29]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 188, 1), segments[30]);
      Assert.Equal(AjisSegment.Name(189, 2, Slice("tail", AjisSliceFlags.IsIdentifierStyle)), segments[31]);
      Assert.Equal(AjisSegment.Value(194, 2, AjisValueKind.String, Slice("ok")), segments[32]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 198, 1), segments[33]);
      Assert.Equal(AjisSegment.Comment(200, 1, Slice(" done")), segments[34]);
      Assert.Equal(AjisSegment.Directive(208, 1, Slice("tool hint=extra")), segments[35]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 225, 1), segments[36]);
      Assert.Equal(AjisSegment.Value(226, 2, AjisValueKind.Number, Slice("TZ5", AjisSliceFlags.IsNumberTyped)), segments[37]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 229, 1), segments[38]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 230, 0), segments[39]);
   }

   [Fact]
   public void ParseSegments_ParsesThreeSiblingContainersWithMixedNamesAndNestedArray()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"ts\":T170},// note\n#tool hint=fast\n{kind:\"identifier\"},// more\n#tool hint=slow\n[TS123,TA7]]"u8, settings).ToList();

      Assert.Equal(18, segments.Count);
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
      Assert.Equal(AjisSegment.Comment(57, 1, Slice(" more")), segments[11]);
      Assert.Equal(AjisSegment.Directive(65, 1, Slice("tool hint=slow")), segments[12]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 81, 1), segments[13]);
      Assert.Equal(AjisSegment.Value(82, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[14]);
      Assert.Equal(AjisSegment.Value(88, 2, AjisValueKind.Number, Slice("TA7", AjisSliceFlags.IsNumberTyped)), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 91, 1), segments[16]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 92, 0), segments[17]);
   }

   [Fact]
   public void ParseSegments_ParsesMixedDepthSiblingContainersWithCommentAndDirective()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         AllowDirectives = true,
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("[{\"outer\":[T170]},// note\n#tool hint=fast\n{inner:{\"value\":TS123}}]"u8, settings).ToList();

      Assert.Equal(17, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Name(2, 2, Slice("outer")), segments[2]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 10, 2), segments[3]);
      Assert.Equal(AjisSegment.Value(11, 3, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 15, 2), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 16, 1), segments[6]);
      Assert.Equal(AjisSegment.Comment(18, 1, Slice(" note")), segments[7]);
      Assert.Equal(AjisSegment.Directive(26, 1, Slice("tool hint=fast")), segments[8]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 42, 1), segments[9]);
      Assert.Equal(AjisSegment.Name(43, 2, Slice("inner", AjisSliceFlags.IsIdentifierStyle)), segments[10]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 49, 2), segments[11]);
      Assert.Equal(AjisSegment.Name(50, 3, Slice("value")), segments[12]);
      Assert.Equal(AjisSegment.Value(58, 3, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[13]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 63, 2), segments[14]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 64, 1), segments[15]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 65, 0), segments[16]);
   }

   [Fact]
   public void ParseSegments_ParsesPrefixedNumbers()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Numbers = new global::Afrowave.AJIS.Core.AjisNumberOptions
         {
            EnableBasePrefixes = true
         }
      };

      var segments = AjisParse.ParseSegments("[0xFF,0b1010]"u8, settings).ToList();

      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, Slice("0xFF", AjisSliceFlags.IsNumberHex)), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("0b1010", AjisSliceFlags.IsNumberBinary)), segments[2]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralFlag()
   {
      var segments = AjisParse.ParseSegments("T170"u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralWithPrefix()
   {
      var segments = AjisParse.ParseSegments("TS123"u8).ToList();

      Assert.Single(segments);
      Assert.Equal(AjisSegment.Value(0, 0, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[0]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralInObject()
   {
      var segments = AjisParse.ParseSegments("{\"ts\":T170}"u8).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts")), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 10, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralWithUnquotedPropertyName()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{ts:T170}"u8, settings).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[1]);
      Assert.Equal(AjisSegment.Value(4, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 8, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesMixedIdentifierAndTypedLiteralValues()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = true
         }
      };

      var segments = AjisParse.ParseSegments("{ts:T170,kind:\"identifier\"}"u8, settings).ToList();

      Assert.Equal(6, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Object, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Name(1, 1, Slice("ts", AjisSliceFlags.IsIdentifierStyle)), segments[1]);
      Assert.Equal(AjisSegment.Value(4, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Name(9, 1, Slice("kind", AjisSliceFlags.IsIdentifierStyle)), segments[3]);
      Assert.Equal(AjisSegment.Value(14, 1, AjisValueKind.String, Slice("identifier")), segments[4]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Object, 26, 0), segments[5]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralInArray()
   {
      var segments = AjisParse.ParseSegments("[T170]"u8).ToList();

      Assert.Equal(3, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[1]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 5, 0), segments[2]);
   }

   [Fact]
   public void ParseSegments_ParsesTypedLiteralAndPrefixedNumber()
   {
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Numbers = new global::Afrowave.AJIS.Core.AjisNumberOptions
         {
            EnableBasePrefixes = true
         }
      };

      var segments = AjisParse.ParseSegments("[T170,0xFF]"u8, settings).ToList();

      Assert.Equal(4, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Value(1, 1, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[1]);
      Assert.Equal(AjisSegment.Value(6, 1, AjisValueKind.Number, Slice("0xFF", AjisSliceFlags.IsNumberHex)), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 10, 0), segments[3]);
   }

   [Fact]
   public void ParseSegments_ParsesNestedArrayTypedLiterals()
   {
      var segments = AjisParse.ParseSegments("[[T170],[TS123]]"u8).ToList();

      Assert.Equal(8, segments.Count);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 0, 0), segments[0]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 1, 1), segments[1]);
      Assert.Equal(AjisSegment.Value(2, 2, AjisValueKind.Number, Slice("T170", AjisSliceFlags.IsNumberTyped)), segments[2]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 6, 1), segments[3]);
      Assert.Equal(AjisSegment.Enter(AjisContainerKind.Array, 8, 1), segments[4]);
      Assert.Equal(AjisSegment.Value(9, 2, AjisValueKind.Number, Slice("TS123", AjisSliceFlags.IsNumberTyped)), segments[5]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 14, 1), segments[6]);
      Assert.Equal(AjisSegment.Exit(AjisContainerKind.Array, 15, 0), segments[7]);
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
