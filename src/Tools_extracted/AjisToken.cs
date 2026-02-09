namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Represents a token produced by the lexer.
/// </summary>
/// <remarks>
/// This is a ref struct for zero-allocation tokenization.
/// The Text property is a slice of the original input.
/// </remarks>
public ref struct AjisToken
{
    /// <summary>
    /// The type of this token.
    /// </summary>
    public AjisTokenType Type { get; set; }

    /// <summary>
    /// The source text of this token (slice of input).
    /// </summary>
    public ReadOnlySpan<char> Text { get; set; }

    /// <summary>
    /// The start offset of this token in the source.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// The length of this token in the source.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// The location of this token in the source.
    /// </summary>
    public AjisLocation Location { get; set; }

    public AjisToken(AjisTokenType type, ReadOnlySpan<char> text, AjisLocation location, int start, int length)
    {
        Type = type;
        Text = text;
        Location = location;
        Start = start;
        Length = length;
    }

    public override string ToString() => $"{Type} '{Text.ToString()}' at {Location}";
}
