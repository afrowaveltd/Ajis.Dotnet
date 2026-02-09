namespace Afrowave.AJIS;

/// <summary>
/// Represents a location in the source text.
/// </summary>
public readonly struct AjisLocation
{
    /// <summary>
    /// Zero-based offset from the start of the input.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// One-based line number.
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// One-based column number.
    /// </summary>
    public int Column { get; init; }

    public AjisLocation(int offset, int line, int column)
    {
        Offset = offset;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"Line {Line}, Column {Column}";
}
