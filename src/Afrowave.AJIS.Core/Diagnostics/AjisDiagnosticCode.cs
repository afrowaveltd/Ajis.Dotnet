#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Stable diagnostic codes. Keep values stable once released.
/// </summary>
public enum AjisDiagnosticCode
{
   Unknown = 0,

   // Reader / structure
   UnexpectedEof = 1001,

   UnexpectedChar = 1002,
   ExpectedChar = 1003,
   DepthLimit = 1004,

   // Strings
   StringUnterminated = 1101,

   StringInvalidEscape = 1102,
   StringInvalidUnicodeEscape = 1103,
   StringControlChar = 1104,

   // Numbers
   NumberInvalid = 1201,

   NumberBaseInvalid = 1202,
   NumberGroupingInvalid = 1203,
   NumberFractionGrouping = 1204,

   // Arrays/Objects
   ArrayTrailingComma = 1301,

   ObjectTrailingComma = 1302,
   ObjectExpectedKey = 1303,
   ObjectDuplicateKey = 1304,
}