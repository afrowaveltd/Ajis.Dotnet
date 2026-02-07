#nullable enable

namespace Afrowave.AJIS.Core.Directives;

/// <summary>
/// Represents a parsed AJIS directive.
/// </summary>
public sealed record AjisDirective(string Namespace, string Command, IReadOnlyDictionary<string, string> Arguments);

/// <summary>
/// Parses directive payloads into structured representations.
/// </summary>
public static class AjisDirectiveParser
{
   /// <summary>
   /// Parses a directive payload (text following '#').
   /// </summary>
   public static AjisDirective Parse(string text)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(text);

      string trimmed = text.Trim();
      string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if(parts.Length < 2)
         throw new FormatException();

      string ns = parts[0].ToUpperInvariant();
      string command = parts[1];
      var args = new Dictionary<string, string>(StringComparer.Ordinal);

      for(int i = 2; i < parts.Length; i++)
      {
         string part = parts[i];
         int equalsIndex = part.IndexOf('=');
         if(equalsIndex <= 0 || equalsIndex == part.Length - 1)
            throw new FormatException();

         string key = part[..equalsIndex];
         string value = part[(equalsIndex + 1)..];
         args[key] = value;
      }

      return new AjisDirective(ns, command, args);
   }
}
