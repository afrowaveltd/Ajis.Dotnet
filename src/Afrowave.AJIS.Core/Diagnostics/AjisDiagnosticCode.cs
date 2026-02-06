#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes. Keep numeric values stable once released.
/// </summary>
public enum AjisDiagnosticCode
{
   Unknown = 0,

   // Reader / structure (1000–1099)
   UnexpectedEof = 1001,
   UnexpectedChar = 1002,
   ExpectedChar = 1003,
   DepthLimit = 1004,
   TrailingGarbage = 1005,
   TokenTooLarge = 1006,
   VisitorAbort = 1007,
   ModeNotSupported = 1008,
   InvalidUtf8 = 1009,
   EngineSelected = 1010,
   InputNotSupported = 1011,

   // Strings (1100–1199)
   StringUnterminated = 1101,
   StringInvalidEscape = 1102,
   StringInvalidUnicodeEscape = 1103,
   StringControlChar = 1104,
   StringUnicodeEscapeNotSupported = 1105,

   // Numbers (1200–1299)
   NumberInvalid = 1201,
   NumberBaseInvalid = 1202,
   NumberGroupingInvalid = 1203,
   NumberFractionGrouping = 1204,
   NumberOverflow = 1205,

   // Arrays/Objects (1300–1399)
   ArrayTrailingComma = 1301,
   ObjectTrailingComma = 1302,
   ObjectExpectedKey = 1303,
   ObjectDuplicateKey = 1304,
   ObjectExpectedColon = 1305,

   // AJIS text extensions (1500–1599) – planned / optional
   CommentUnterminated = 1501,
   DirectiveInvalid = 1502,
   DirectiveUnknown = 1503,
   IdentifierInvalid = 1504,
   IdentifierDuplicate = 1505,

   // Typed literals (1600–1699)
   TimestampInvalid = 1601,
   TimestampUnitInvalid = 1602,
   TimestampOverflow = 1603,

   // LAX recovery & tolerance (1700–1799)
   LaxRecoveredUnquotedKey = 1701,
   LaxRecoveredSingleQuotedString = 1702,
   LaxRecoveredTrailingComma = 1703,
   LaxRecoveredMissingComma = 1704,
   LaxRecoveredMissingColon = 1705,
   LaxSkippedUnknownDirective = 1706,

   // ATP / attachments (1800–1899) – reserved for M3
   AttachmentTableInvalid = 1801,
   AttachmentOutOfRange = 1802,
   AttachmentCrcMismatch = 1803,
   AttachmentNotFound = 1804,
}
