#nullable enable

namespace Afrowave.AJIS.Streaming;

/// <summary>
/// Flags describing the raw UTF-8 token representation for a slice.
/// </summary>
[Flags]
public enum AjisSliceFlags
{
   None = 0,
   HasEscapes = 1 << 0,
   HasNonAscii = 1 << 1,
   IsIdentifierStyle = 1 << 2,
   IsNumberHex = 1 << 3,
   IsNumberBinary = 1 << 4,
   IsNumberOctal = 1 << 5
}

/// <summary>
/// A UTF-8 slice representing a raw token segment with associated flags.
/// </summary>
public readonly record struct AjisSliceUtf8(ReadOnlyMemory<byte> Bytes, AjisSliceFlags Flags) : IEquatable<AjisSliceUtf8>
{
   /// <summary>
   /// Represents an empty slice.
   /// </summary>
   public static AjisSliceUtf8 Empty => new(ReadOnlyMemory<byte>.Empty, AjisSliceFlags.None);

   /// <summary>
   /// Indicates whether the slice has no content.
   /// </summary>
   public bool IsEmpty => Bytes.IsEmpty;

   public bool Equals(AjisSliceUtf8 other)
      => Flags == other.Flags && Bytes.Span.SequenceEqual(other.Bytes.Span);

   public override int GetHashCode()
   {
      var hash = new HashCode();
      hash.Add(Flags);
      hash.Add(Bytes.Length);
      if(!Bytes.IsEmpty)
      {
         var span = Bytes.Span;
         hash.Add(span[0]);
         hash.Add(span[^1]);
      }
      return hash.ToHashCode();
   }
}
