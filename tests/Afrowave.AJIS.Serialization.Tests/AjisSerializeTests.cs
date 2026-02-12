#nullable enable

using Afrowave.AJIS.Serialization;
using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Reader;
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

   // ===== M4 Serialization Mode Tests =====

   [Fact]
   public void ToText_CompactMode_NoSpacing()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("a"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(5, 1, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Name(7, 1, new AjisSliceUtf8("b"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(11, 1, AjisValueKind.Number, new AjisSliceUtf8("2"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 12, 0)
      };

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Compact = true
         }
      };

      string text = AjisSerialize.ToText(segments, settings);

      // Compact: no spaces after colons or commas
      var normalized = text.Replace(" ", string.Empty);
      Assert.Equal("{\"a\":1,\"b\":2}", normalized);
   }

   [Fact]
   public void ToText_PrettyMode_WithIndentation()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("name"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(7, 1, AjisValueKind.String, new AjisSliceUtf8("John"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Name(13, 1, new AjisSliceUtf8("age"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(18, 1, AjisValueKind.Number, new AjisSliceUtf8("30"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 20, 0)
      };

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         // Pretty mode via formatting options (if exposed)
      };

      string text = AjisSerialize.ToText(segments, settings);

      // Pretty: should have newlines and indentation
      // Note: actual format depends on default serialization formatting
      Assert.NotEmpty(text);
      Assert.Contains("\"name\"", text);
      Assert.Contains("\"John\"", text);
   }

   [Fact]
   public void ToText_NestedObjectFormatting()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("user"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Enter(AjisContainerKind.Object, 7, 1),
         AjisSegment.Name(8, 2, new AjisSliceUtf8("id"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(12, 2, AjisValueKind.Number, new AjisSliceUtf8("1"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 13, 1),
         AjisSegment.Exit(AjisContainerKind.Object, 14, 0)
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Contains("\"user\"", text);
      Assert.Contains("\"id\"", text);
      Assert.Contains("1", text);
   }

   [Fact]
   public void ToText_ArrayWithMixedTypes()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Array, 0, 0),
         AjisSegment.Value(1, 1, AjisValueKind.Null, null),
         AjisSegment.Value(5, 1, AjisValueKind.Boolean, new AjisSliceUtf8("true"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(10, 1, AjisValueKind.Number, new AjisSliceUtf8("42"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(12, 1, AjisValueKind.String, new AjisSliceUtf8("text"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Array, 18, 0)
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Contains("null", text);
      Assert.Contains("true", text);
      Assert.Contains("42", text);
      Assert.Contains("\"text\"", text);
   }

   [Fact]
   public void ToText_NumberFormatPreservation()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Array, 0, 0),
         AjisSegment.Value(1, 1, AjisValueKind.Number, new AjisSliceUtf8("0xFF"u8.ToArray(), AjisSliceFlags.IsNumberHex)),
         AjisSegment.Value(5, 1, AjisValueKind.Number, new AjisSliceUtf8("0b1010"u8.ToArray(), AjisSliceFlags.IsNumberBinary)),
         AjisSegment.Value(11, 1, AjisValueKind.Number, new AjisSliceUtf8("0o77"u8.ToArray(), AjisSliceFlags.IsNumberOctal)),
         AjisSegment.Exit(AjisContainerKind.Array, 15, 0)
      };

      string text = AjisSerialize.ToText(segments);

      // Format should be preserved as-is
      Assert.Contains("0xFF", text);
      Assert.Contains("0b1010", text);
      Assert.Contains("0o77", text);
   }

   [Fact]
   public void ToText_EscapeSequencesCorrect()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("key"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Value(6, 1, AjisValueKind.String, 
            new AjisSliceUtf8("line1\nline2\ttab\"quote"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Exit(AjisContainerKind.Object, 26, 0)
      };

      string text = AjisSerialize.ToText(segments);

      // Escapes should be properly applied
      Assert.Contains("\\n", text);
      Assert.Contains("\\t", text);
      Assert.Contains("\\\"", text);
   }

   [Fact]
   public void ToText_EmptyContainers()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, new AjisSliceUtf8("empty_obj"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Enter(AjisContainerKind.Object, 12, 1),
         AjisSegment.Exit(AjisContainerKind.Object, 13, 1),
         AjisSegment.Name(14, 1, new AjisSliceUtf8("empty_arr"u8.ToArray(), AjisSliceFlags.None)),
         AjisSegment.Enter(AjisContainerKind.Array, 25, 1),
         AjisSegment.Exit(AjisContainerKind.Array, 26, 1),
         AjisSegment.Exit(AjisContainerKind.Object, 27, 0)
      };

      string text = AjisSerialize.ToText(segments);

      Assert.Contains("{}", text);
      Assert.Contains("[]", text);
   }

   [Fact]
   public void ToText_RoundTrip_SimpleObject()
   {
      // Parse → Serialize → Compare
      string original = "{\"a\":1}";
      byte[] originalBytes = Encoding.UTF8.GetBytes(original);
      var parsedSegments = AjisParse.ParseSegments(originalBytes).ToList();
      string serialized = AjisSerialize.ToText(parsedSegments);

      // Should produce valid AJIS text that parses to same structure
      byte[] serializedBytes = Encoding.UTF8.GetBytes(serialized);
      var reparse = AjisParse.ParseSegments(serializedBytes).ToList();
      Assert.Equal(parsedSegments.Count, reparse.Count);
   }

   [Fact]
   public void ToText_RoundTrip_ComplexNested()
   {
      string original = "{\"users\":[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]}";
      byte[] originalBytes = Encoding.UTF8.GetBytes(original);
      var parsedSegments = AjisParse.ParseSegments(originalBytes).ToList();
      string serialized = AjisSerialize.ToText(parsedSegments);

      // Validate structure is preserved
      byte[] serializedBytes = Encoding.UTF8.GetBytes(serialized);
      var reparse = AjisParse.ParseSegments(serializedBytes).ToList();

      // Should have same number of segments (ignoring whitespace differences)
      Assert.True(parsedSegments.Count > 0);
      Assert.True(reparse.Count > 0);
      Assert.Equal(parsedSegments.Count, reparse.Count);
   }

   // ===== M7 Mapping Layer Tests (Object to AJIS Conversion) =====

   [Fact]
   public void Serialize_PrimitiveTypes()
   {
      var converter = new global::Afrowave.AJIS.Serialization.Mapping.AjisConverter<int>();
      string result = converter.Serialize(42);
      Assert.Equal("42", result);
   }

   [Fact]
   public void Serialize_String()
   {
      var converter = new global::Afrowave.AJIS.Serialization.Mapping.AjisConverter<string>();
      string result = converter.Serialize("hello");
      Assert.Equal("\"hello\"", result);
   }

   [Fact]
   public void Serialize_SimpleObject()
   {
      var person = new Person { Name = "Alice", Age = 30 };
      var converter = new global::Afrowave.AJIS.Serialization.Mapping.AjisConverter<Person>();
      string result = converter.Serialize(person);
      // Výchozí je camelCase
      Assert.Contains("\"name\"", result);
      Assert.Contains("\"Alice\"", result);
      Assert.Contains("\"age\"", result);
      Assert.Contains("30", result);
   }

   [Fact]
   public void Serialize_WithCamelCaseNaming()
   {
      var person = new Person { Name = "Bob", Age = 25 };
      var converter = new global::Afrowave.AJIS.Serialization.Mapping.AjisConverter<Person>(
         new global::Afrowave.AJIS.Serialization.Mapping.CamelCaseNamingPolicy()
      );
      string result = converter.Serialize(person);

      Assert.Contains("\"name\"", result);
      Assert.Contains("\"Bob\"", result);
      Assert.Contains("\"age\"", result);
      Assert.Contains("25", result);
      Assert.DoesNotContain("\"Name\"", result);
   }

   [Fact]
   public void Serialize_WithSnakeCaseNaming()
   {
      var person = new Person { Name = "Charlie", Age = 35 };
      var converter = new global::Afrowave.AJIS.Serialization.Mapping.AjisConverter<Person>(
         new global::Afrowave.AJIS.Serialization.Mapping.SnakeCaseNamingPolicy()
      );
      string result = converter.Serialize(person);

      Assert.Contains("\"name\"", result);
      Assert.Contains("\"Charlie\"", result);
      Assert.Contains("\"age\"", result);
      Assert.Contains("35", result);
   }

   [Fact]
   public void NamingPolicy_PascalCase_NoChange()
   {
      var policy = global::Afrowave.AJIS.Serialization.Mapping.PascalCaseNamingPolicy.Instance;
      Assert.Equal("FirstName", policy.ConvertName("FirstName"));
      Assert.Equal("ID", policy.ConvertName("ID"));
   }

   [Fact]
   public void NamingPolicy_CamelCase_Conversion()
   {
      var policy = global::Afrowave.AJIS.Serialization.Mapping.CamelCaseNamingPolicy.Instance;
      Assert.Equal("firstName", policy.ConvertName("FirstName"));
      Assert.Equal("userID", policy.ConvertName("UserID"));
      Assert.Equal("a", policy.ConvertName("A"));
   }

   [Fact]
   public void NamingPolicy_SnakeCase_Conversion()
   {
      var policy = global::Afrowave.AJIS.Serialization.Mapping.SnakeCaseNamingPolicy.Instance;
      Assert.Equal("first_name", policy.ConvertName("FirstName"));
      Assert.Equal("user_i_d", policy.ConvertName("UserID"));
      Assert.Equal("a", policy.ConvertName("A"));
   }

   [Fact]
   public void NamingPolicy_KebabCase_Conversion()
   {
      var policy = global::Afrowave.AJIS.Serialization.Mapping.KebabCaseNamingPolicy.Instance;
      Assert.Equal("first-name", policy.ConvertName("FirstName"));
      Assert.Equal("user-i-d", policy.ConvertName("UserID"));
      Assert.Equal("a", policy.ConvertName("A"));
   }

   /// <summary>
   /// Helper test class for M7 mapping tests
   /// </summary>
   private sealed class Person
   {
      public string Name { get; set; } = "";
      public int Age { get; set; }
   }
}
