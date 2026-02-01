#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming.Segments; // AjisSegment

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
      _ = segments;
      _ = settings;
      throw new NotImplementedException("Serializer not implemented yet. This is an API skeleton.");
   }

   public static async Task ToStreamAsync(
      Stream output,
      IAsyncEnumerable<AjisSegment> segments,
      AjisSettings? settings = null,
      CancellationToken ct = default)
   {
      _ = output;
      _ = segments;
      _ = settings;
      _ = ct;
      await Task.CompletedTask;
      throw new NotImplementedException("Serializer not implemented yet. This is an API skeleton.");
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
      _ = output;
      _ = value;
      _ = settings;
      throw new NotImplementedException("Value serializer not implemented yet. This is an API skeleton.");
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
      _ = output;
      _ = value;
      _ = settings;
      _ = ct;
      throw new NotImplementedException("Value serializer not implemented yet. This is an API skeleton.");
   }

   /// <summary>
   /// Serializes an AJIS value to a UTF-8 byte array.
   /// </summary>
   public static byte[] SerializeToUtf8Bytes(AjisValue value, AjisSettings? settings = null)
   {
      _ = value;
      _ = settings;
      throw new NotImplementedException("Value serializer not implemented yet. This is an API skeleton.");
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
