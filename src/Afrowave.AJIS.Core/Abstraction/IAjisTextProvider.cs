#nullable enable

using System.Globalization;

namespace Afrowave.AJIS.Core.Abstraction;

/// <summary>
/// Resolves localized strings for AJIS diagnostics and user-facing messages.
/// </summary>
/// <remarks>
/// <para>
/// AJIS localization is lightweight and optional. Core always provides English fallback.
/// Additional languages can be supplied by satellite packages or user overrides.
/// </para>
/// <para>
/// Formatting uses .NET-style placeholders (e.g., <c>{0}</c>, <c>{1:format}</c>)
/// and respects the specified <see cref="CultureInfo"/>.
/// </para>
/// </remarks>
public interface IAjisTextProvider
{
   /// <summary>
   /// Gets the localized text for <paramref name="key"/> (optionally formatted).
   /// </summary>
   /// <param name="key">Localization key.</param>
   /// <param name="culture">Culture to use. If null, <see cref="CultureInfo.CurrentUICulture"/> is used.</param>
   /// <param name="data">
   /// Optional structured data for formatting.
   /// Minimal convention supported by Core:
   /// - if data contains "args" (case-insensitive) with value object?[] then string.Format is applied.
   /// Other keys are ignored by Core provider (may be used by richer providers).
   /// </param>
   /// <returns>Localized string (or fallback/missing-key behavior output).</returns>
   string GetText(string key, CultureInfo? culture = null, IReadOnlyDictionary<string, object?>? data = null);

   /// <summary>
   /// Gets the localized text for <paramref name="key"/> using current UI culture.
   /// </summary>
   string Get(string key) => GetText(key, CultureInfo.CurrentUICulture, data: null);

   /// <summary>
   /// Formats a localized text for <paramref name="key"/> using the provided culture.
   /// </summary>
   /// <param name="culture">Culture to use for formatting. If null, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
   /// <param name="key">Localization key.</param>
   /// <param name="args">Formatting arguments.</param>
   /// <returns>Formatted localized string.</returns>
   string Format(CultureInfo? culture, string key, params object?[] args);

   /// <summary>
   /// Formats a localized text for <paramref name="key"/> using <see cref="CultureInfo.CurrentCulture"/>.
   /// </summary>
   string Format(string key, params object?[] args)
       => Format(CultureInfo.CurrentCulture, key, args);
}

/// <summary>
/// Defines how missing localization keys are handled.
/// </summary>
public enum MissingKeyBehavior
{
   /// <summary>
   /// Return the key as-is.
   /// </summary>
   ReturnKey = 0,

   /// <summary>
   /// Return <c>[missing:KEY]</c>.
   /// </summary>
   Bracketed = 1,

   /// <summary>
   /// Return an empty string.
   /// </summary>
   Empty = 2,
}