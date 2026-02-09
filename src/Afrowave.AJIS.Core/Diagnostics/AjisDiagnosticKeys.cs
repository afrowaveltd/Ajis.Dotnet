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
   public const string InvalidUtf8 = "invalid_utf8";
   public const string EngineSelected = "engine_selected";
   public const string InputNotSupported = "input_not_supported";


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
   public const string NumberOverflow = "number_overflow";

   // Arrays/Objects
   public const string ArrayTrailingComma = "array_trailing_comma";
   public const string ObjectTrailingComma = "object_trailing_comma";
   public const string ObjectExpectedKey = "object_expected_key";
   public const string ObjectDuplicateKey = "object_duplicate_key";
   public const string ObjectExpectedColon = "object_expected_colon";

   // AJIS text extensions (planned)
   public const string CommentUnterminated = "comment_unterminated";
   public const string DirectiveInvalid = "directive_invalid";
   public const string DirectiveUnknown = "directive_unknown";
   public const string IdentifierInvalid = "identifier_invalid";
   public const string IdentifierDuplicate = "identifier_duplicate";

   // Typed literals (planned)
   public const string TimestampInvalid = "timestamp_invalid";
   public const string TimestampUnitInvalid = "timestamp_unit_invalid";
   public const string TimestampOverflow = "timestamp_overflow";

   // LAX recovery & tolerance
   public const string LaxRecoveredUnquotedKey = "lax_recovered_unquoted_key";
   public const string LaxRecoveredSingleQuotedString = "lax_recovered_single_quoted_string";
   public const string LaxRecoveredTrailingComma = "lax_recovered_trailing_comma";
   public const string LaxRecoveredMissingComma = "lax_recovered_missing_comma";
   public const string LaxRecoveredMissingColon = "lax_recovered_missing_colon";
   public const string LaxSkippedUnknownDirective = "lax_skipped_unknown_directive";

   // ATP / attachments (reserved)
   public const string AttachmentTableInvalid = "attachment_table_invalid";
   public const string AttachmentOutOfRange = "attachment_out_of_range";
   public const string AttachmentCrcMismatch = "attachment_crc_mismatch";
   public const string AttachmentNotFound = "attachment_not_found";

   /// <summary>
   /// Converts a diagnostic code to its stable string key.
   /// </summary>
   /// <param name="code">Diagnostic code to convert.</param>
   /// <returns>Stable string key for the diagnostic.</returns>
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
      AjisDiagnosticCode.InvalidUtf8 => InvalidUtf8,
      AjisDiagnosticCode.EngineSelected => EngineSelected,
      AjisDiagnosticCode.InputNotSupported => InputNotSupported,


      AjisDiagnosticCode.StringUnterminated => StringUnterminated,
      AjisDiagnosticCode.StringInvalidEscape => StringInvalidEscape,
      AjisDiagnosticCode.StringInvalidUnicodeEscape => StringInvalidUnicodeEscape,
      AjisDiagnosticCode.StringControlChar => StringControlChar,
      AjisDiagnosticCode.StringUnicodeEscapeNotSupported => StringUnicodeEscapeNotSupported,

      AjisDiagnosticCode.NumberInvalid => NumberInvalid,
      AjisDiagnosticCode.NumberBaseInvalid => NumberBaseInvalid,
      AjisDiagnosticCode.NumberGroupingInvalid => NumberGroupingInvalid,
      AjisDiagnosticCode.NumberFractionGrouping => NumberGroupingInFraction,
      AjisDiagnosticCode.NumberOverflow => NumberOverflow,

      AjisDiagnosticCode.ArrayTrailingComma => ArrayTrailingComma,
      AjisDiagnosticCode.ObjectTrailingComma => ObjectTrailingComma,
      AjisDiagnosticCode.ObjectExpectedKey => ObjectExpectedKey,
      AjisDiagnosticCode.ObjectDuplicateKey => ObjectDuplicateKey,
      AjisDiagnosticCode.ObjectExpectedColon => ObjectExpectedColon,

      AjisDiagnosticCode.CommentUnterminated => CommentUnterminated,
      AjisDiagnosticCode.DirectiveInvalid => DirectiveInvalid,
      AjisDiagnosticCode.DirectiveUnknown => DirectiveUnknown,
      AjisDiagnosticCode.IdentifierInvalid => IdentifierInvalid,
      AjisDiagnosticCode.IdentifierDuplicate => IdentifierDuplicate,

      AjisDiagnosticCode.TimestampInvalid => TimestampInvalid,
      AjisDiagnosticCode.TimestampUnitInvalid => TimestampUnitInvalid,
      AjisDiagnosticCode.TimestampOverflow => TimestampOverflow,

      AjisDiagnosticCode.LaxRecoveredUnquotedKey => LaxRecoveredUnquotedKey,
      AjisDiagnosticCode.LaxRecoveredSingleQuotedString => LaxRecoveredSingleQuotedString,
      AjisDiagnosticCode.LaxRecoveredTrailingComma => LaxRecoveredTrailingComma,
      AjisDiagnosticCode.LaxRecoveredMissingComma => LaxRecoveredMissingComma,
      AjisDiagnosticCode.LaxRecoveredMissingColon => LaxRecoveredMissingColon,
      AjisDiagnosticCode.LaxSkippedUnknownDirective => LaxSkippedUnknownDirective,

      AjisDiagnosticCode.AttachmentTableInvalid => AttachmentTableInvalid,
      AjisDiagnosticCode.AttachmentOutOfRange => AttachmentOutOfRange,
      AjisDiagnosticCode.AttachmentCrcMismatch => AttachmentCrcMismatch,
      AjisDiagnosticCode.AttachmentNotFound => AttachmentNotFound,

      _ => Unknown,
   };
}
