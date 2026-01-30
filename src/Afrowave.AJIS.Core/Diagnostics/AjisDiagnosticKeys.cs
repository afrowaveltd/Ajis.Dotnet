#nullable enable

namespace Afrowave.AJIS.Core.Diagnostics;

/// <summary>
/// Localization keys for diagnostics. Keep stable.
/// </summary>
public static class AjisDiagnosticKeys
{
   public static string For(AjisDiagnosticCode code) => code switch
   {
      AjisDiagnosticCode.UnexpectedEof => "ajis.error.unexpected_eof",
      AjisDiagnosticCode.UnexpectedChar => "ajis.error.unexpected_char",
      AjisDiagnosticCode.ExpectedChar => "ajis.error.expected_char",
      AjisDiagnosticCode.DepthLimit => "ajis.error.depth_limit",

      AjisDiagnosticCode.StringUnterminated => "ajis.error.string_unterminated",
      AjisDiagnosticCode.StringInvalidEscape => "ajis.error.string_invalid_escape",
      AjisDiagnosticCode.StringInvalidUnicodeEscape => "ajis.error.string_invalid_unicode_escape",
      AjisDiagnosticCode.StringControlChar => "ajis.error.string_control_char",

      AjisDiagnosticCode.NumberInvalid => "ajis.error.number_invalid",
      AjisDiagnosticCode.NumberBaseInvalid => "ajis.error.number_base_invalid",
      AjisDiagnosticCode.NumberGroupingInvalid => "ajis.error.number_grouping_invalid",
      AjisDiagnosticCode.NumberFractionGrouping => "ajis.error.number_fraction_grouping",

      AjisDiagnosticCode.ArrayTrailingComma => "ajis.error.array_trailing_comma",
      AjisDiagnosticCode.ObjectTrailingComma => "ajis.error.object_trailing_comma",
      AjisDiagnosticCode.ObjectExpectedKey => "ajis.error.object_expected_key",
      AjisDiagnosticCode.ObjectDuplicateKey => "ajis.error.object_duplicate_key",

      _ => "ajis.error.unknown",
   };
}