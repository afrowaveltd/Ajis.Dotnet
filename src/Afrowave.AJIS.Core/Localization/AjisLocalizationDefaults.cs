#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using System.Globalization;

namespace Afrowave.AJIS.Core.Localization;

/// <summary>
/// Creates a default localization provider chain:
/// user overrides -> UI culture -> English fallback.
/// </summary>
public static class AjisLocalizationDefaults
{
   /// <summary>
   /// Builds default provider using current UI culture and built-in English fallback.
   /// </summary>
   public static async ValueTask<IAjisTextProvider> BuildDefaultAsync(
       AjisLocDictionary? userOverrides = null,
       MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.Bracketed,
       CancellationToken ct = default)
   {
      var builder = new AjisTextProviderBuilder();

      // Highest priority: user overrides
      if(userOverrides is not null)
         builder.AddHighPriority(userOverrides);

      // Middle: UI language (optional; loaded via callback/provider later)
      // For now: Core only guarantees English fallback. We keep the slot for future packages.
      // We can add UI dictionary here later once we have a resolver.

      // Lowest priority: built-in English
      var en = await AjisBuiltInLocales.LoadEnglishAsync(ct).ConfigureAwait(false);
      builder.AddLowPriority(en);

      return builder.Build(missingKeyBehavior);
   }

   /// <summary>
   /// Picks the most relevant culture for messages. Defaults to CurrentUICulture.
   /// </summary>
   public static CultureInfo GetDefaultCulture()
       => CultureInfo.CurrentUICulture;
}