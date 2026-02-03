#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes. Keep values stable once released.
/// </summary>
public enum AjisDiagnosticCode
{
   Unknown = 0,

   // Reader / structure (1000–1099)
   UnexpectedEof = 1001,
   UnexpectedChar = 1002,
   ExpectedChar = 1003,
   DepthLimit = 1004,

   /// <summary>
   /// Non-whitespace data after the document finished.
   /// </summary>
   TrailingGarbage = 1005,

   /// <summary>
   /// Token (string/number/name/etc.) exceeded configured byte limit.
   /// </summary>
   TokenTooLarge = 1006,

   /// <summary>
   /// Visitor requested to stop processing (only if allowed by options).
   /// </summary>
   VisitorAbort = 1007,

   /// <summary>
   /// Parsing mode is not supported by this runner/milestone.
   /// </summary>
   ModeNotSupported = 1008,

   // Strings (1100–1199)
   StringUnterminated = 1101,
   StringInvalidEscape = 1102,
   StringInvalidUnicodeEscape = 1103,
   StringControlChar = 1104,

   /// <summary>
   /// Unicode escape sequences are not supported in the current milestone.
   /// </summary>
   StringUnicodeEscapeNotSupported = 1105,

   // Numbers (1200–1299)
   NumberInvalid = 1201,
   NumberBaseInvalid = 1202,
   NumberGroupingInvalid = 1203,
   NumberFractionGrouping = 1204,

   // Arrays/Objects (1300–1399)
   ArrayTrailingComma = 1301,
   ObjectTrailingComma = 1302,
   ObjectExpectedKey = 1303,
   ObjectDuplicateKey = 1304,

   // AJIS text extensions (1500–1599) – reserved for M2
   CommentUnterminated = 1501,
   DirectiveInvalid = 1502,
   DirectiveUnknown = 1503,
   IdentifierInvalid = 1504,

   // Typed literals (1600–1699) – reserved for M2
   TimestampInvalid = 1601,
   TimestampUnitInvalid = 1602,
}
