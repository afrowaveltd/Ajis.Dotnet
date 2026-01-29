#nullable enable

using System.Globalization;

namespace Afrowave.AJIS.Core.Abstractions;

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
   /// Gets the localized text for <paramref name="key"/>.
   /// </summary>
   /// <param name="key">Localization key.</param>
   /// <returns>Localized string (or fallback/missing-key behavior output).</returns>
   string Get(string key);

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
   /// <param name="key">Localization key.</param>
   /// <param name="args">Formatting arguments.</param>
   /// <returns>Formatted localized string.</returns>
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
