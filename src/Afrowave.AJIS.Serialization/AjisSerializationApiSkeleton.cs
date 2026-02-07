#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Segments; // AjisSegment
using System.Text;
using Afrowave.AJIS.Serialization.Engines;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Serialization entry points (API skeleton).
/// </summary>
public static class AjisSerialize
{
   public static string ToText(
      IEnumerable<AjisSegment> segments,
      AjisSettings? settings = null)
   {
      ArgumentNullException.ThrowIfNull(segments);
      _ = AjisSerializationProfileSelector.Select(settings);
      _ = AjisSerializationEngineSelector.Select(AjisSerializationProfileSelector.Select(settings));
      AjisSerializationFormattingOptions format = AjisSerializationFormatting.GetOptions(settings);
      if(format.Canonicalize)
         segments = global::Afrowave.AJIS.Streaming.Segments.Transforms.AjisSegmentCanonicalizer.Canonicalize(segments);

      return new AjisSegmentTextWriter(format).Write(segments);
   }

   public static async Task ToStreamAsync(
      Stream output,
      IAsyncEnumerable<AjisSegment> segments,
      AjisSettings? settings = null,
      CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(segments);
      _ = AjisSerializationProfileSelector.Select(settings);
      _ = AjisSerializationEngineSelector.Select(AjisSerializationProfileSelector.Select(settings));
      AjisSerializationFormattingOptions format = AjisSerializationFormatting.GetOptions(settings);
      var eventSink = settings?.EventSink ?? global::Afrowave.AJIS.Core.Events.NullAjisEventSink.Instance;
      await AjisSerializationEventEmitter.EmitPhaseAsync(eventSink, "serialize", "start", ct).ConfigureAwait(false);
      await AjisSerializationEventEmitter.EmitProgressAsync(eventSink, "serialize", 0, ct).ConfigureAwait(false);

      if(format.Canonicalize)
      {
         var materialized = new List<AjisSegment>();
         await foreach(var segment in segments.WithCancellation(ct).ConfigureAwait(false))
            materialized.Add(segment);

         IEnumerable<AjisSegment> canonical = global::Afrowave.AJIS.Streaming.Segments.Transforms.AjisSegmentCanonicalizer.Canonicalize(materialized);
         await new AjisSegmentTextWriter(format).WriteAsync(output, ToAsyncEnumerable(canonical), ct).ConfigureAwait(false);
      }
      else
      {
         await new AjisSegmentTextWriter(format).WriteAsync(output, segments, ct).ConfigureAwait(false);
      }

      await AjisSerializationEventEmitter.EmitProgressAsync(eventSink, "serialize", 100, ct).ConfigureAwait(false);
      await AjisSerializationEventEmitter.EmitPhaseAsync(eventSink, "serialize", "end", ct).ConfigureAwait(false);
   }

   private static async IAsyncEnumerable<AjisSegment> ToAsyncEnumerable(IEnumerable<AjisSegment> segments)
   {
      foreach(AjisSegment segment in segments)
         yield return segment;
      await Task.CompletedTask;
   }
}


/// <summary>
/// High-level serializer facade.
/// </summary>
/// <remarks>
/// AJIS intentionally keeps the first public surface small.
/// More advanced mapping (POCO, source generators, etc.) will live in optional packages.
/// </remarks>
public static class AjisSerializer
{
   /// <summary>
   /// Serializes an AJIS value expressed as primitive/object/array in a minimal manner.
   /// </summary>
   /// <remarks>
   /// This overload exists mostly for tools and tests.
   /// A richer object model may be introduced later.
   /// </remarks>
   public static void Serialize(
       Stream output,
       AjisValue value,
       AjisSettings? settings = null)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(value);
      _ = AjisSerializationProfileSelector.Select(settings);
      _ = AjisSerializationEngineSelector.Select(AjisSerializationProfileSelector.Select(settings));
      AjisSerializationFormattingOptions format = AjisSerializationFormatting.GetOptions(settings);
      string text = new AjisValueTextWriter(format).Write(value);
      byte[] bytes = Encoding.UTF8.GetBytes(text);
      output.Write(bytes, 0, bytes.Length);
   }

   /// <summary>
   /// Serializes an AJIS value asynchronously.
   /// </summary>
   public static ValueTask SerializeAsync(
       Stream output,
       AjisValue value,
       AjisSettings? settings = null,
       CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(value);
      _ = AjisSerializationProfileSelector.Select(settings);
      _ = AjisSerializationEngineSelector.Select(AjisSerializationProfileSelector.Select(settings));
      AjisSerializationFormattingOptions format = AjisSerializationFormatting.GetOptions(settings);
      return SerializeValueAsync(output, value, format, settings, ct);
   }

   private static async ValueTask SerializeValueAsync(
      Stream output,
      AjisValue value,
      AjisSerializationFormattingOptions format,
      AjisSettings? settings,
      CancellationToken ct)
   {
      var eventSink = settings?.EventSink ?? global::Afrowave.AJIS.Core.Events.NullAjisEventSink.Instance;
      await AjisSerializationEventEmitter.EmitPhaseAsync(eventSink, "serialize", "start", ct).ConfigureAwait(false);
      await AjisSerializationEventEmitter.EmitProgressAsync(eventSink, "serialize", 0, ct).ConfigureAwait(false);

      await new AjisValueTextWriter(format).WriteAsync(output, value, ct).ConfigureAwait(false);

      await AjisSerializationEventEmitter.EmitProgressAsync(eventSink, "serialize", 100, ct).ConfigureAwait(false);
      await AjisSerializationEventEmitter.EmitPhaseAsync(eventSink, "serialize", "end", ct).ConfigureAwait(false);
   }

   /// <summary>
   /// Serializes an AJIS value to a UTF-8 byte array.
   /// </summary>
   public static byte[] SerializeToUtf8Bytes(AjisValue value, AjisSettings? settings = null)
   {
      ArgumentNullException.ThrowIfNull(value);
      _ = AjisSerializationProfileSelector.Select(settings);
      _ = AjisSerializationEngineSelector.Select(AjisSerializationProfileSelector.Select(settings));
      AjisSerializationFormattingOptions format = AjisSerializationFormatting.GetOptions(settings);
      string text = new AjisValueTextWriter(format).Write(value);
      return Encoding.UTF8.GetBytes(text);
   }

}

/// <summary>
/// Minimal value representation for tools/tests.
/// </summary>
/// <remarks>
/// This is NOT intended as a full DOM. It is a small helper type so tools can be built
/// without bringing a large object model into Core.
/// </remarks>
public abstract record AjisValue
{
   private AjisValue() { }

   /// <summary>
   /// Null value.
   /// </summary>
   public sealed record NullValue() : AjisValue;

   /// <summary>
   /// Boolean value.
   /// </summary>
   public sealed record BoolValue(bool Value) : AjisValue;

   /// <summary>
   /// Number value stored as original text (to preserve base prefixes and separators).
   /// </summary>
   public sealed record NumberValue(string Text) : AjisValue;

   /// <summary>
   /// String value.
   /// </summary>
   public sealed record StringValue(string Value) : AjisValue;

   /// <summary>
   /// Array value.
   /// </summary>
   public sealed record ArrayValue(IReadOnlyList<AjisValue> Items) : AjisValue;

   /// <summary>
   /// Object value.
   /// </summary>
   public sealed record ObjectValue(IReadOnlyList<KeyValuePair<string, AjisValue>> Properties) : AjisValue;

   /// <summary>
   /// Creates a null.
   /// </summary>
   public static AjisValue Null() => new NullValue();

   /// <summary>
   /// Creates a boolean.
   /// </summary>
   public static AjisValue Bool(bool value) => new BoolValue(value);

   /// <summary>
   /// Creates a number from raw text.
   /// </summary>
   public static AjisValue Number(string text) => new NumberValue(text);

   /// <summary>
   /// Creates a string.
   /// </summary>
   public static AjisValue String(string value) => new StringValue(value);

   /// <summary>
   /// Creates an array.
   /// </summary>
   public static AjisValue Array(params AjisValue[] items) => new ArrayValue(items);

   /// <summary>
   /// Creates an object.
   /// </summary>
   public static AjisValue Object(params KeyValuePair<string, AjisValue>[] props) => new ObjectValue(props);
}
