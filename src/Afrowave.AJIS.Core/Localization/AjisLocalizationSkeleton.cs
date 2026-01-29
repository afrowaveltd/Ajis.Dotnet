#nullable enable

using Afrowave.AJIS.Core.Abstractions;
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
   public async ValueTask<AjisLocDictionary> LoadAsync(Stream input, CancellationToken ct = default)
   {
      if(input is null) throw new ArgumentNullException(nameof(input));

      // NOTE: We intentionally do not use StreamReader.ReadLineAsync() for maximum control.
      // This implementation is already low-memory (line-by-line) and fast enough for large dictionaries.

      var dict = new Dictionary<string, string>(StringComparer.Ordinal);

      using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 16 * 1024, leaveOpen: true);

      string? line;
      while((line = await reader.ReadLineAsync().WaitAsync(ct).ConfigureAwait(false)) is not null)
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

      // Must start with opening quote.
      if(line.Length < 4 || line[0] != '"') return false;

      if(!TryReadQuotedString(line, startIndex: 0, out var keyText, out var afterKey))
         return false;

      // Skip whitespace
      while(afterKey < line.Length && char.IsWhiteSpace(line[afterKey])) afterKey++;

      // Expect ':'
      if(afterKey >= line.Length || line[afterKey] != ':') return false;
      afterKey++;

      // Skip whitespace
      while(afterKey < line.Length && char.IsWhiteSpace(line[afterKey])) afterKey++;

      // Expect opening quote for value
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
            // End of string.
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
               _ => e, // Unknown escapes are preserved as-is for now.
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
public sealed class AjisLocDictionary
{
   private readonly IReadOnlyDictionary<string, string> _dict;

   /// <summary>
   /// Creates a dictionary wrapper.
   /// </summary>
   public AjisLocDictionary(IReadOnlyDictionary<string, string> dict)
   {
      _dict = dict ?? throw new ArgumentNullException(nameof(dict));
   }

   /// <summary>
   /// Gets the underlying read-only dictionary.
   /// </summary>
   public IReadOnlyDictionary<string, string> Entries => _dict;

   /// <summary>
   /// Attempts to get a localized string.
   /// </summary>
   public bool TryGet(string key, out string value)
   {
      if(key is null) throw new ArgumentNullException(nameof(key));
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
   private readonly List<AjisLocDictionary> _chain = new();

   /// <summary>
   /// Adds a dictionary at the highest priority (first lookup).
   /// </summary>
   public AjisTextProviderBuilder AddHighPriority(AjisLocDictionary dictionary)
   {
      if(dictionary is null) throw new ArgumentNullException(nameof(dictionary));
      _chain.Insert(0, dictionary);
      return this;
   }

   /// <summary>
   /// Adds a dictionary at the lowest priority (last lookup).
   /// </summary>
   public AjisTextProviderBuilder AddLowPriority(AjisLocDictionary dictionary)
   {
      if(dictionary is null) throw new ArgumentNullException(nameof(dictionary));
      _chain.Add(dictionary);
      return this;
   }

   /// <summary>
   /// Builds a provider that resolves localization keys.
   /// </summary>
   /// <remarks>
   /// This is a minimal in-memory provider. Later we will connect it to diagnostics, events and logging.
   /// </remarks>
   public IAjisTextProvider Build(MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.ReturnKey)
       => new ChainedAjisTextProvider(_chain, missingKeyBehavior);

   private sealed class ChainedAjisTextProvider : IAjisTextProvider
   {
      private readonly IReadOnlyList<AjisLocDictionary> _chain;
      private readonly MissingKeyBehavior _missingKeyBehavior;

      public ChainedAjisTextProvider(IReadOnlyList<AjisLocDictionary> chain, MissingKeyBehavior missingKeyBehavior)
      {
         _chain = chain;
         _missingKeyBehavior = missingKeyBehavior;
      }

      public string Get(string key)
      {
         if(key is null) throw new ArgumentNullException(nameof(key));

         foreach(var d in _chain)
         {
            if(d.TryGet(key, out var value))
               return value;
         }

         return _missingKeyBehavior switch
         {
            MissingKeyBehavior.ReturnKey => key,
            MissingKeyBehavior.Bracketed => $"[missing:{key}]",
            _ => key,
         };
      }

      public string Format(string key, params object?[] args)
      {
         var fmt = Get(key);
         return string.Format(CultureInfo.CurrentCulture, fmt, args);
      }
   }
}

