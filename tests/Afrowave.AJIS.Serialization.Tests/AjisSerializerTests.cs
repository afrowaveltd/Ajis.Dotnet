#nullable enable

using Afrowave.AJIS.Serialization;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisSerializerTests
{
   [Fact]
   public void Serialize_WritesNull()
   {
      var value = AjisValue.Null();
      using var stream = new MemoryStream();

      AjisSerializer.Serialize(stream, value);

      Assert.Equal("null", Encoding.UTF8.GetString(stream.ToArray()));
   }

   [Fact]
   public async Task SerializeAsync_WritesBoolean()
   {
      var value = AjisValue.Bool(true);
      await using var stream = new MemoryStream();

      await AjisSerializer.SerializeAsync(stream, value, ct: TestContext.Current.CancellationToken).AsTask();

      Assert.Equal("true", Encoding.UTF8.GetString(stream.ToArray()));
   }

   [Fact]
   public async Task SerializeAsync_EmitsProgressEvents()
   {
      var eventStream = new global::Afrowave.AJIS.Core.Events.AjisEventStream();
      var value = AjisValue.Bool(true);
      await using var stream = new MemoryStream();
      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         EventSink = eventStream
      };

      await AjisSerializer.SerializeAsync(stream, value, settings, TestContext.Current.CancellationToken).AsTask();

      eventStream.Complete();

      var events = new List<global::Afrowave.AJIS.Core.Events.AjisEvent>();
      await foreach(var evt in eventStream.WithCancellation(TestContext.Current.CancellationToken))
         events.Add(evt);

      Assert.Contains(events, e => e is global::Afrowave.AJIS.Core.Events.AjisProgressEvent);
   }

   [Fact]
   public void SerializeToUtf8Bytes_WritesString()
   {
      var value = AjisValue.String("hi");

      byte[] bytes = AjisSerializer.SerializeToUtf8Bytes(value);

      Assert.Equal("\"hi\"", Encoding.UTF8.GetString(bytes));
   }

   [Fact]
   public void SerializeToUtf8Bytes_EscapesString()
   {
      var value = AjisValue.String("a\n\"b");

      byte[] bytes = AjisSerializer.SerializeToUtf8Bytes(value);

      Assert.Equal("\"a\\n\\\"b\"", Encoding.UTF8.GetString(bytes));
   }

   [Fact]
   public void SerializeToUtf8Bytes_RespectsNonCompactSettings()
   {
      var value = AjisValue.Object(
         new KeyValuePair<string, AjisValue>("a", AjisValue.Number("1")),
         new KeyValuePair<string, AjisValue>("b", AjisValue.Number("2")));

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Compact = false
         }
      };

      byte[] bytes = AjisSerializer.SerializeToUtf8Bytes(value, settings);

      Assert.Equal("{\"a\": 1, \"b\": 2}", Encoding.UTF8.GetString(bytes));
   }

   [Fact]
   public void SerializeToUtf8Bytes_RespectsPrettySettings()
   {
      var value = AjisValue.Object(
         new KeyValuePair<string, AjisValue>("a", AjisValue.Number("1")),
         new KeyValuePair<string, AjisValue>("b", AjisValue.Number("2")));

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Pretty = true,
            IndentSize = 2
         }
      };

      byte[] bytes = AjisSerializer.SerializeToUtf8Bytes(value, settings);

      string expected = string.Join(Environment.NewLine,
         "{",
         "  \"a\": 1,",
         "  \"b\": 2",
         "}");

      Assert.Equal(expected, Encoding.UTF8.GetString(bytes));
   }

   [Fact]
   public void SerializeToUtf8Bytes_RespectsCanonicalOrdering()
   {
      var value = AjisValue.Object(
         new KeyValuePair<string, AjisValue>("b", AjisValue.Number("2")),
         new KeyValuePair<string, AjisValue>("a", AjisValue.Number("1")));

      var settings = new global::Afrowave.AJIS.Core.AjisSettings
      {
         Serialization = new global::Afrowave.AJIS.Core.AjisSerializationOptions
         {
            Canonicalize = true
         }
      };

      byte[] bytes = AjisSerializer.SerializeToUtf8Bytes(value, settings);

      Assert.Equal("{\"a\":1,\"b\":2}", Encoding.UTF8.GetString(bytes));
   }
}
