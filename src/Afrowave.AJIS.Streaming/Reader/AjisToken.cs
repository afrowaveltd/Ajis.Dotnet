#nullable enable

namespace Afrowave.AJIS.Streaming.Reader;

public enum AjisTokenKind
{
   End = 0,
   LeftBrace = 1,
   RightBrace = 2,
   LeftBracket = 3,
   RightBracket = 4,
   Colon = 5,
   Comma = 6,
   String = 7,
   Number = 8,
   Identifier = 9,
   Directive = 10,
   True = 11,
   False = 12,
   Null = 13,
   Comment = 14
}

public readonly record struct AjisToken(AjisTokenKind Kind, long Offset, int Line, int Column, string? Text);
