namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Represents the type of a token in AJIS/JSON.
/// </summary>
public enum AjisTokenType : byte
{
    /// <summary>End of file</summary>
    Eof = 0,

    /// <summary>Left brace: {</summary>
    LeftBrace,

    /// <summary>Right brace: }</summary>
    RightBrace,

    /// <summary>Left bracket: [</summary>
    LeftBracket,

    /// <summary>Right bracket: ]</summary>
    RightBracket,

    /// <summary>Colon: :</summary>
    Colon,

    /// <summary>Comma: ,</summary>
    Comma,

    /// <summary>String literal</summary>
    String,

    /// <summary>Number literal (integer or float)</summary>
    Number,

    /// <summary>Boolean true</summary>
    True,

    /// <summary>Boolean false</summary>
    False,

    /// <summary>Null value</summary>
    Null,

    /// <summary>Binary attachment: bin"..."</summary>
    BinaryAttachment,

    /// <summary>Hexadecimal binary: hex"..."</summary>
    HexBinary,

    /// <summary>Base64 binary: b64"..."</summary>
    Base64Binary,
}
