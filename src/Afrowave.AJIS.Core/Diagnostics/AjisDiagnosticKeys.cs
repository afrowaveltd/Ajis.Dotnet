#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Stable string keys for diagnostics (for logs, localization, test outputs).
/// </summary>
public static class AjisDiagnosticKeys
{
   public const string Unknown = "unknown";

   // Reader / structure
   public const string UnexpectedEof = "unexpected_eof";
   public const string UnexpectedChar = "unexpected_char";
   public const string ExpectedChar = "expected_char";
   public const string DepthLimit = "depth_limit";
   public const string TrailingGarbage = "trailing_garbage";
   public const string TokenTooLarge = "token_too_large";
   public const string VisitorAbort = "visitor_abort";
   public const string ModeNotSupported = "mode_not_supported";

   // Strings
   public const string StringUnterminated = "string_unterminated";
   public const string StringInvalidEscape = "string_invalid_escape";
   public const string StringInvalidUnicodeEscape = "string_invalid_unicode_escape";
   public const string StringControlChar = "string_control_char";
   public const string StringUnicodeEscapeNotSupported = "string_unicode_escape_not_supported";

   // Numbers
   public const string NumberInvalid = "number_invalid";
   public const string NumberBaseInvalid = "number_base_invalid";
   public const string NumberGroupingInvalid = "number_grouping_invalid";
   public const string NumberGroupingInFraction = "number_grouping_in_fraction";

   // Arrays/Objects
   public const string ArrayTrailingComma = "array_trailing_comma";
   public const string ObjectTrailingComma = "object_trailing_comma";
   public const string ObjectExpectedKey = "object_expected_key";
   public const string ObjectDuplicateKey = "object_duplicate_key";

   // AJIS text extensions (reserved for M2)
   public const string CommentUnterminated = "comment_unterminated";
   public const string DirectiveInvalid = "directive_invalid";
   public const string DirectiveUnknown = "directive_unknown";
   public const string IdentifierInvalid = "identifier_invalid";

   // Typed literals (reserved for M2)
   public const string TimestampInvalid = "timestamp_invalid";
   public const string TimestampUnitInvalid = "timestamp_unit_invalid";

   /// <summary>
   /// Converts a diagnostic code to its stable string key.
   /// </summary>
   public static string For(AjisDiagnosticCode code) => code switch
   {
      AjisDiagnosticCode.UnexpectedEof => UnexpectedEof,
      AjisDiagnosticCode.UnexpectedChar => UnexpectedChar,
      AjisDiagnosticCode.ExpectedChar => ExpectedChar,
      AjisDiagnosticCode.DepthLimit => DepthLimit,
      AjisDiagnosticCode.TrailingGarbage => TrailingGarbage,
      AjisDiagnosticCode.TokenTooLarge => TokenTooLarge,
      AjisDiagnosticCode.VisitorAbort => VisitorAbort,
      AjisDiagnosticCode.ModeNotSupported => ModeNotSupported,

      AjisDiagnosticCode.StringUnterminated => StringUnterminated,
      AjisDiagnosticCode.StringInvalidEscape => StringInvalidEscape,
      AjisDiagnosticCode.StringInvalidUnicodeEscape => StringInvalidUnicodeEscape,
      AjisDiagnosticCode.StringControlChar => StringControlChar,
      AjisDiagnosticCode.StringUnicodeEscapeNotSupported => StringUnicodeEscapeNotSupported,

      AjisDiagnosticCode.NumberInvalid => NumberInvalid,
      AjisDiagnosticCode.NumberBaseInvalid => NumberBaseInvalid,
      AjisDiagnosticCode.NumberGroupingInvalid => NumberGroupingInvalid,
      AjisDiagnosticCode.NumberFractionGrouping => NumberGroupingInFraction,

      AjisDiagnosticCode.ArrayTrailingComma => ArrayTrailingComma,
      AjisDiagnosticCode.ObjectTrailingComma => ObjectTrailingComma,
      AjisDiagnosticCode.ObjectExpectedKey => ObjectExpectedKey,
      AjisDiagnosticCode.ObjectDuplicateKey => ObjectDuplicateKey,

      AjisDiagnosticCode.CommentUnterminated => CommentUnterminated,
      AjisDiagnosticCode.DirectiveInvalid => DirectiveInvalid,
      AjisDiagnosticCode.DirectiveUnknown => DirectiveUnknown,
      AjisDiagnosticCode.IdentifierInvalid => IdentifierInvalid,

      AjisDiagnosticCode.TimestampInvalid => TimestampInvalid,
      AjisDiagnosticCode.TimestampUnitInvalid => TimestampUnitInvalid,

      _ => Unknown,
   };
}
