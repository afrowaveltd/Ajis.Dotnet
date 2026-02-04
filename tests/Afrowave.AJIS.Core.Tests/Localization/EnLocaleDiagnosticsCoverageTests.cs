#nullable enable

using Afrowave.AJIS.Core.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class EnLocaleDiagnosticsCoverageTests
{
   [Fact]
   public void EnLocale_ShouldContain_AllDiagnosticKeys()
   {
      string repoRoot = FindRepoRoot();
      string locPath = Path.Combine(repoRoot, "src", "Afrowave.AJIS.Core", "Resources", "Locales", "en.loc");

      Assert.True(File.Exists(locPath), $"Missing locale file: {locPath}");

      string locText = ReadTextUtf8OrUtf8Bom(locPath);
      HashSet<string> locKeys = ParseLocKeys(locText);

      // Expected: for every AjisDiagnosticKeys constant value V, there must be: "ajis.error.V".
      string[] diagValues = [.. typeof(AjisDiagnosticKeys)
         .GetFields(BindingFlags.Public | BindingFlags.Static)
         .Where(f => f.FieldType == typeof(string))
         .Select(f => (string)f.GetRawConstantValue()!)
         .Distinct(StringComparer.Ordinal)];

      string[] expected = [.. diagValues.Select(v => $"ajis.error.{v}")];

      string[] missing = [.. expected
         .Where(k => !locKeys.Contains(k))
         .OrderBy(k => k, StringComparer.Ordinal)];

      Assert.True(missing.Length == 0,
         "en.loc is missing diagnostic keys:\n" + string.Join("\n", missing));
   }

   private static string FindRepoRoot()
   {
      var dir = new DirectoryInfo(AppContext.BaseDirectory);
      while(dir is not null)
      {
         if(File.Exists(Path.Combine(dir.FullName, "Directory.Build.props"))
             || File.Exists(Path.Combine(dir.FullName, "Ajis.Dotnet.slnx"))
             || File.Exists(Path.Combine(dir.FullName, "Ajis.Dotnet.sln")))
         {
            return dir.FullName;
         }

         dir = dir.Parent;
      }

      throw new DirectoryNotFoundException(
         "Could not locate repository root (Directory.Build.props / Ajis.Dotnet.slnx not found above test output folder).");
   }

   private static string ReadTextUtf8OrUtf8Bom(string path)
   {
      byte[] bytes = File.ReadAllBytes(path);
      // Strip UTF-8 BOM if present
      if(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
      {
         return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
      }
      return Encoding.UTF8.GetString(bytes);
   }

   private static HashSet<string> ParseLocKeys(string locText)
   {
      // LOC format: "key":"value" (value supports escapes), other lines ignored.
      var set = new HashSet<string>(StringComparer.Ordinal);
      var rx = new Regex("^\\s*\"(?<k>[^\"]+)\"\\s*:\\s*\"(?:[^\"\\\\]|\\\\.)*\"\\s*$",
         RegexOptions.Multiline | RegexOptions.CultureInvariant);

      foreach(Match m in rx.Matches(locText))
      {
         string key = m.Groups["k"].Value;
         if(!string.IsNullOrWhiteSpace(key))
            set.Add(key);
      }

      return set;
   }
}
