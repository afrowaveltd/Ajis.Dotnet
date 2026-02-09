#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Afrowave.AJIS.Core.Localization;

/// <summary>
/// Built-in locale helpers.
/// </summary>
public static class AjisBuiltInLocales
{
   /// <summary>
   /// Default language code for built-in locales.
   /// </summary>
   public const string DefaultLanguageCode = "en";

   /// <summary>
   /// Loads the built-in English locale dictionary.
   /// </summary>
   /// <param name="ct">Cancellation token.</param>
   /// <returns>The loaded localization dictionary.</returns>
   public static async ValueTask<AjisLocDictionary> LoadEnglishAsync(CancellationToken ct = default)
   {
      var asm = typeof(AjisBuiltInLocales).Assembly;

      // NOTE: Resource name depends on default namespace + folder path.
      // If your root namespace differs, adjust this string once and keep stable.
      const string resourceName = "Afrowave.AJIS.Core.Resources.Locales.en.loc";

      await using var s = asm.GetManifestResourceStream(resourceName)
          ?? throw new InvalidOperationException($"Embedded locale not found: {resourceName}");

      return await AjisLocLoader.LoadAsync(s, ct).ConfigureAwait(false);
   }
}