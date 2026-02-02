#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using System.Globalization;
using System.Text;

namespace Afrowave.AJIS.Core.Localization;

/// <summary>
/// Loads AJIS LOC v1 dictionaries from UTF-8 text.
/// </summary>
/// <remarks>
/// <para>
/// AJIS LOC v1 is line-based. Only lines starting with a double quote (<c>"</c>) are records.
/// All other lines are ignored (comments/metadata/empty lines).
/// </para>
/// <para>
/// Record syntax:
/// <c>"KEY":"VALUE"</c> and anything after the closing value quote is ignored.
/// </para>
/// </remarks>
public sealed class AjisLocLoader
{
   /// <summary>
   /// Loads a localization dictionary from a stream.
   /// </summary>
   /// <param name="input">Input stream containing a .loc file in UTF-8.</param>
   /// <param name="ct">Cancellation token.</param>
   /// <returns>Loaded dictionary.</returns>
   public static async ValueTask<AjisLocDictionary> LoadAsync(Stream input, CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(input);

      var dict = new Dictionary<string, string>(StringComparer.Ordinal);

      using var reader = new StreamReader(
         input,
         Encoding.UTF8,
         detectEncodingFromByteOrderMarks: true,
         bufferSize: 16 * 1024,
         leaveOpen: true);

      string? line;
      while((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
      {
         ct.ThrowIfCancellationRequested();

         if(line.Length == 0) continue;
         if(line[0] != '"') continue; // Ignore comments/metadata.

         if(TryParseLocLine(line.AsSpan(), out var key, out var value))
         {
            // Last wins (allows overrides within a file).
            dict[key] = value;
         }
         // else: silently ignore invalid record lines for now.
         // Later: plug in diagnostics + localized error reporting.
      }

      return new AjisLocDictionary(dict);
   }

   /// <summary>
   /// Parses one AJIS LOC v1 record line.
   /// </summary>
   internal static bool TryParseLocLine(ReadOnlySpan<char> line, out string key, out string value)
   {
      key = string.Empty;
      value = string.Empty;

      if(line.Length < 4 || line[0] != '"') return false;

      if(!TryReadQuotedString(line, startIndex: 0, out var keyText, out var afterKey))
         return false;

      while(afterKey < line.Length && char.IsWhiteSpace(line[afterKey])) afterKey++;

      if(afterKey >= line.Length || line[afterKey] != ':') return false;
      afterKey++;

      while(afterKey < line.Length && char.IsWhiteSpace(line[afterKey])) afterKey++;

      if(afterKey >= line.Length || line[afterKey] != '"') return false;

      if(!TryReadQuotedString(line, startIndex: afterKey, out var valueText, out _))
         return false;

      key = keyText;
      value = valueText;
      return true;
   }

   /// <summary>
   /// Reads a quoted string supporting basic escapes.
   /// </summary>
   private static bool TryReadQuotedString(ReadOnlySpan<char> line, int startIndex, out string result, out int nextIndex)
   {
      result = string.Empty;
      nextIndex = startIndex;

      if(startIndex < 0 || startIndex >= line.Length) return false;
      if(line[startIndex] != '"') return false;

      var sb = new StringBuilder(Math.Min(256, Math.Max(0, line.Length - startIndex)));

      var i = startIndex + 1;
      while(i < line.Length)
      {
         var c = line[i++];

         if(c == '"')
         {
            result = sb.ToString();
            nextIndex = i;
            return true;
         }

         if(c == '\\')
         {
            if(i >= line.Length) return false;
            var e = line[i++];

            sb.Append(e switch
            {
               '"' => '"',
               '\\' => '\\',
               'n' => '\n',
               'r' => '\r',
               't' => '\t',
               _ => e,
            });

            continue;
         }

         sb.Append(c);
      }

      return false;
   }
}

/// <summary>
/// Represents a localization dictionary loaded from AJIS LOC v1.
/// </summary>
public sealed class AjisLocDictionary(IReadOnlyDictionary<string, string> dict)
{
   private readonly IReadOnlyDictionary<string, string> _dict =
      dict ?? throw new ArgumentNullException(nameof(dict));

   /// <summary>
   /// Gets the underlying read-only dictionary.
   /// </summary>
   public IReadOnlyDictionary<string, string> Entries => _dict;

   /// <summary>
   /// Attempts to get a localized string.
   /// </summary>
   public bool TryGet(string key, out string value)
   {
      ArgumentNullException.ThrowIfNull(key);
      return _dict.TryGetValue(key, out value!);
   }
}

/// <summary>
/// Builds a text provider from multiple localization sources.
/// </summary>
/// <remarks>
/// This class focuses on composing the fallback chain:
/// user overrides -> UI language -> English.
/// </remarks>
public sealed class AjisTextProviderBuilder
{
   private readonly List<AjisLocDictionary> _chain = [];

   /// <summary>
   /// Adds a dictionary at the highest priority (first lookup).
   /// </summary>
   public AjisTextProviderBuilder AddHighPriority(AjisLocDictionary dictionary)
   {
      ArgumentNullException.ThrowIfNull(dictionary);
      _chain.Insert(0, dictionary);
      return this;
   }

   /// <summary>
   /// Adds a dictionary at the lowest priority (last lookup).
   /// </summary>
   public AjisTextProviderBuilder AddLowPriority(AjisLocDictionary dictionary)
   {
      ArgumentNullException.ThrowIfNull(dictionary);
      _chain.Add(dictionary);
      return this;
   }

   /// <summary>
   /// Builds a provider that resolves localization keys.
   /// </summary>
   public IAjisTextProvider Build(MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.ReturnKey)
       => new ChainedAjisTextProvider(_chain, missingKeyBehavior);

   private sealed class ChainedAjisTextProvider(IReadOnlyList<AjisLocDictionary> chain, MissingKeyBehavior missingKeyBehavior) : IAjisTextProvider
   {
      private readonly IReadOnlyList<AjisLocDictionary> _chain = chain;
      private readonly MissingKeyBehavior _missingKeyBehavior = missingKeyBehavior;

      public string GetText(string key, CultureInfo? culture = null, IReadOnlyDictionary<string, object?>? data = null)
      {
         ArgumentNullException.ThrowIfNull(key);

         string? raw = null;

         foreach(var d in _chain)
         {
            if(d.TryGet(key, out var value))
            {
               raw = value;
               break;
            }
         }

         raw ??= _missingKeyBehavior switch
         {
            MissingKeyBehavior.ReturnKey => key,
            MissingKeyBehavior.Bracketed => $"[missing:{key}]",
            MissingKeyBehavior.Empty => string.Empty,
            _ => key,
         };

         if(data is null || data.Count == 0)
            return raw;

         // Minimal Core convention: data["args"] = object?[] for string.Format.
         foreach(var kv in data)
         {
            if(!string.Equals(kv.Key, "args", StringComparison.OrdinalIgnoreCase))
               continue;

            if(kv.Value is object?[] args)
            {
               var fmtCulture = culture ?? CultureInfo.CurrentCulture;
               return string.Format(fmtCulture, raw, args);
            }
         }

         return raw;
      }

      public string Format(CultureInfo? culture, string key, params object?[] args)
      {
         var fmt = GetText(key, culture ?? CultureInfo.CurrentUICulture, data: null);
         return string.Format(culture ?? CultureInfo.CurrentCulture, fmt, args);
      }
   }
}