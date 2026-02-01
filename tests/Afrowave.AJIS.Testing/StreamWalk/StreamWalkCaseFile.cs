using Afrowave.AJIS.Streaming;
using System.Text;

namespace Afrowave.AJIS.Testing.StreamWalk;

public sealed record StreamWalkCase
{
   public required string CaseId { get; init; }
   public required StreamWalkOptions Options { get; init; }
   public required byte[] InputUtf8 { get; init; }
   public required StreamWalkExpected Expected { get; init; }
}

public sealed record StreamWalkExpected
{
   public required List<TraceEvent> Events { get; init; }
}

public sealed record TraceEvent
{
   public required string Kind { get; init; }
   public string? Value { get; init; }
}

public static class StreamWalkCaseFile
{
   private const string OptionsHeader = "# OPTIONS";
   private const string InputHeader = "# INPUT";
   private const string ExpectedHeader = "# EXPECTED";

   /// <summary>
   /// Loads a .case/.swcase file from disk.
   /// </summary>
   public static StreamWalkCase Load(string path)
   {
      if(string.IsNullOrWhiteSpace(path))
         throw new ArgumentException("Path is required.", nameof(path));

      var text = File.ReadAllText(path, Encoding.UTF8);
      return Parse(text, caseId: NormalizeCaseId(path));
   }

   /// <summary>
   /// Parses a StreamWalk case file text (UTF-8 expected) into a structured representation.
   /// </summary>
   public static StreamWalkCase Parse(string caseFileText, string caseId)
   {
      if(caseFileText is null)
         throw new ArgumentNullException(nameof(caseFileText));
      if(string.IsNullOrWhiteSpace(caseId))
         throw new ArgumentException("CaseId is required.", nameof(caseId));

      // Normalize newlines (treat CRLF and LF the same)
      var src = caseFileText.Replace("\r\n", "\n").Replace("\r", "\n");

      var optionsBlock = ExtractSection(src, OptionsHeader, InputHeader, required: true);
      var inputBlock = ExtractSection(src, InputHeader, ExpectedHeader, required: true);
      var expectedBlock = ExtractSection(src, ExpectedHeader, null, required: true);

      var options = ParseOptions(optionsBlock);
      var inputUtf8 = Encoding.UTF8.GetBytes(inputBlock);
      var expected = ParseExpected(expectedBlock);

      return new StreamWalkCase
      {
         CaseId = caseId,
         Options = options,
         InputUtf8 = inputUtf8,
         Expected = expected,
      };
   }

   private static string NormalizeCaseId(string path)
   {
      var file = Path.GetFileNameWithoutExtension(path);
      return string.IsNullOrWhiteSpace(file) ? "case" : file;
   }

   private static string ExtractSection(string src, string header, string? nextHeader, bool required)
   {
      var start = src.IndexOf(header, StringComparison.Ordinal);
      if(start < 0)
      {
         if(required)
            throw new FormatException($"Missing section: {header}");
         return string.Empty;
      }

      start += header.Length;

      // Accept an optional colon after header ("# OPTIONS:"), and skip one newline.
      if(start < src.Length && src[start] == ':') start++;
      if(start < src.Length && src[start] == '\n') start++;

      var end = nextHeader is null
          ? src.Length
          : src.IndexOf(nextHeader, start, StringComparison.Ordinal);

      if(end < 0)
      {
         if(required)
            throw new FormatException($"Missing section terminator: {nextHeader}");
         end = src.Length;
      }

      return src.Substring(start, end - start).TrimEnd();
   }

   private static StreamWalkOptions ParseOptions(string optionsBlock)
   {
      // Start from canonical defaults. Then apply overrides.
      var opt = StreamWalkOptions.DefaultForM1;

      foreach(var rawLine in SplitLines(optionsBlock))
      {
         var line = rawLine.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith("#", StringComparison.Ordinal)) continue;

         var idx = line.IndexOf(':');
         if(idx <= 0)
            throw new FormatException($"Invalid option line: '{line}' (expected KEY: VALUE)");

         var key = line.Substring(0, idx).Trim().ToUpperInvariant();
         var value = line.Substring(idx + 1).Trim();

         switch(key)
         {
            case "MODE":
               opt = opt with { Mode = ParseMode(value) };
               break;
            case "COMMENTS":
               opt = opt with { Comments = ParseOnOff(value) };
               break;
            case "DIRECTIVES":
               opt = opt with { Directives = ParseOnOff(value) };
               break;
            case "IDENTIFIERS":
               opt = opt with { Identifiers = ParseOnOff(value) };
               break;
            case "MAX_DEPTH":
               opt = opt with { MaxDepth = ParseInt(value, key) };
               break;
            case "MAX_TOKEN_BYTES":
               opt = opt with { MaxTokenBytes = ParseInt(value, key) };
               break;
            default:
               throw new FormatException($"Unknown option key: {key}");
         }
      }

      return opt;
   }

   private static StreamWalkMode ParseMode(string value)
   {
      var v = value.Trim().ToUpperInvariant();
      return v switch
      {
         "AJIS" => StreamWalkMode.Ajis,
         "JSON" => StreamWalkMode.Json,
         "AUTO" => StreamWalkMode.Auto,
         _ => throw new FormatException($"Invalid MODE: '{value}'")
      };
   }

   private static bool ParseOnOff(string value)
   {
      var v = value.Trim().ToUpperInvariant();
      return v switch
      {
         "ON" => true,
         "OFF" => false,
         "TRUE" => true,
         "FALSE" => false,
         "1" => true,
         "0" => false,
         _ => throw new FormatException($"Invalid ON/OFF value: '{value}'")
      };
   }

   private static int ParseInt(string value, string key)
   {
      if(!int.TryParse(value.Trim(), out var n) || n < 0)
         throw new FormatException($"Invalid integer for {key}: '{value}'");
      return n;
   }

   private static IEnumerable<string> SplitLines(string s)
   {
      if(string.IsNullOrEmpty(s))
         yield break;

      using var sr = new StringReader(s);
      string? line;
      while((line = sr.ReadLine()) is not null)
         yield return line;
   }

   private static StreamWalkExpected ParseExpected(string expectedBlock)
   {
      // Minimal v1: lines like:
      //   EVENT: Kind[: Value]
      // Examples:
      //   TOKEN: LBRACE
      //   VALUE: "hello"
      //   ERROR: unexpected EOF

      var events = new List<TraceEvent>();

      foreach(var rawLine in SplitLines(expectedBlock))
      {
         var line = rawLine.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith("#", StringComparison.Ordinal)) continue;

         var idx = line.IndexOf(':');
         if(idx <= 0)
            throw new FormatException($"Invalid expected line: '{line}' (expected KIND: VALUE?)");

         var kind = line.Substring(0, idx).Trim();
         var rest = line.Substring(idx + 1).Trim();

         events.Add(new TraceEvent
         {
            Kind = kind,
            Value = rest.Length == 0 ? null : rest,
         });
      }

      return new StreamWalkExpected { Events = events };
   }
}
