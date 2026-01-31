// File: tests/Afrowave.AJIS.Testing/StreamWalk/StreamWalkCaseFile.cs
#nullable enable

using System.Globalization;
using System.Text;

namespace Afrowave.AJIS.Testing.StreamWalk;

/// <summary>
/// Represents one StreamWalk test case loaded from a <c>.case</c> file.
/// Canonical format is defined in <c>Docs/tests/streamwalk.md</c> (M1).
/// </summary>
public sealed class StreamWalkCase
{
   public required string CaseId { get; init; }
   public required StreamWalkOptions Options { get; init; }
   public required byte[] InputUtf8 { get; init; }
   public required StreamWalkExpected Expected { get; init; }

   public override string ToString() => CaseId;
}

/// <summary>
/// Options section (M1). Unspecified values mean "use implementation defaults".
/// </summary>
public sealed record StreamWalkOptions
{
   public StreamWalkMode? Mode { get; init; }
   public bool? Comments { get; init; }
   public bool? Directives { get; init; }
   public bool? Identifiers { get; init; }
   public int? MaxDepth { get; init; }
   public int? MaxTokenBytes { get; init; }

   public static StreamWalkOptions DefaultForM1 => new()
   {
      Mode = StreamWalkMode.AJIS,
      Comments = false,
      Directives = false,
      Identifiers = false,
      // limits: left null on purpose (implementation defaults)
   };
}

public enum StreamWalkMode
{
   AJIS = 0,
   JSON = 1,
}

/// <summary>
/// Expected outcome: either Success (trace) or Failure (error).
/// </summary>
public abstract class StreamWalkExpected
{
   private StreamWalkExpected() { }

   public sealed class Success : StreamWalkExpected
   {
      public required IReadOnlyList<TraceEvent> Trace { get; init; }
   }

   public sealed class Failure : StreamWalkExpected
   {
      public required string ErrorCode { get; init; }
      public required int ErrorOffset { get; init; }
      public int? ErrorLine { get; init; }
      public int? ErrorColumn { get; init; }
   }
}

/// <summary>
/// One expected event line.
/// </summary>
public readonly record struct TraceEvent(string Kind, string? Slice);

/// <summary>
/// Loader for StreamWalk <c>.case</c> files.
/// </summary>
public static class StreamWalkCaseFile
{
   private const string OptionsHeader = "# OPTIONS";
   private const string InputHeader = "# INPUT";
   private const string ExpectedHeader = "# EXPECTED";

   /// <summary>
   /// Loads a <c>.case</c> file from disk.
   /// </summary>
   public static StreamWalkCase Load(string path)
   {
      if(string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
      var text = File.ReadAllText(path, Encoding.UTF8);
      return Parse(text, caseId: NormalizeCaseId(path));
   }

   /// <summary>
   /// Parses a <c>.case</c> file text (UTF-8 expected) into a structured representation.
   /// </summary>
   public static StreamWalkCase Parse(string caseFileText, string caseId)
   {
      if(caseFileText is null) throw new ArgumentNullException(nameof(caseFileText));
      if(string.IsNullOrWhiteSpace(caseId)) throw new ArgumentException("CaseId is required.", nameof(caseId));

      // Normalize newlines (we treat CRLF and LF the same)
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

   /// <summary>
   /// Renders a UTF-8 byte slice into the canonical <c>b"..."</c> trace form.
   /// </summary>
   public static string RenderSlice(ReadOnlySpan<byte> utf8)
   {
      // Render bytes as UTF-8 string and escape for test traces.
      // The trace representation preserves decoded bytes, not lexical quoting.
      var s = Encoding.UTF8.GetString(utf8);
      var sb = new StringBuilder(s.Length + 8);
      sb.Append("b\"");
      foreach(var ch in s)
      {
         switch(ch)
         {
            case '\\': sb.Append("\\\\"); break;
            case '"': sb.Append("\\\""); break;
            case '\n': sb.Append("\\n"); break;
            case '\r': sb.Append("\\r"); break;
            case '\t': sb.Append("\\t"); break;
            default:
               // Keep printable chars; for others, use \uXXXX.
               if(char.IsControl(ch)) sb.Append($"\\u{(int)ch:X4}");
               else sb.Append(ch);
               break;
         }
      }
      sb.Append('"');
      return sb.ToString();
   }

   /// <summary>
   /// Parses a trace slice token in <c>b"..."</c> form back into decoded UTF-8 bytes.
   /// </summary>
   public static byte[] ParseRenderedSlice(string token)
   {
      if(token is null) throw new ArgumentNullException(nameof(token));
      token = token.Trim();

      if(!token.StartsWith("b\"", StringComparison.Ordinal) || !token.EndsWith("\"", StringComparison.Ordinal))
         throw new FormatException($"Invalid slice token: {token}");

      var inner = token.Substring(2, token.Length - 3);
      var sb = new StringBuilder(inner.Length);

      for(int i = 0; i < inner.Length; i++)
      {
         var c = inner[i];
         if(c != '\\') { sb.Append(c); continue; }

         if(i + 1 >= inner.Length) throw new FormatException("Invalid escape at end of slice.");
         var n = inner[++i];
         switch(n)
         {
            case '\\': sb.Append('\\'); break;
            case '"': sb.Append('"'); break;
            case 'n': sb.Append('\n'); break;
            case 'r': sb.Append('\r'); break;
            case 't': sb.Append('\t'); break;
            case 'u':
               if(i + 4 >= inner.Length) throw new FormatException("Invalid \\u escape in slice.");
               var hex = inner.Substring(i + 1, 4);
               if(!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                  throw new FormatException($"Invalid \\u escape in slice: {hex}");
               sb.Append((char)code);
               i += 4;
               break;
            default:
               // Unknown escape; keep strict so tests catch drift.
               throw new FormatException($"Unknown escape \\{n} in slice.");
         }
      }

      return Encoding.UTF8.GetBytes(sb.ToString());
   }

   private static StreamWalkOptions ParseOptions(string optionsBlock)
   {
      StreamWalkOptions opt = new();

      foreach(var rawLine in EnumerateNonEmptyNonCommentLines(optionsBlock))
      {
         var line = rawLine.Trim();
         var idx = line.IndexOf(':');
         if(idx <= 0) throw new FormatException($"Invalid option line: {rawLine}");

         var key = line.Substring(0, idx).Trim();
         var value = line[(idx + 1)..].Trim();

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

      // Apply M1 defaults only for missing booleans/mode.
      var d = StreamWalkOptions.DefaultForM1;
      return new StreamWalkOptions
      {
         Mode = opt.Mode ?? d.Mode,
         Comments = opt.Comments ?? d.Comments,
         Directives = opt.Directives ?? d.Directives,
         Identifiers = opt.Identifiers ?? d.Identifiers,
         MaxDepth = opt.MaxDepth,
         MaxTokenBytes = opt.MaxTokenBytes,
      };
   }

   private static StreamWalkExpected ParseExpected(string expectedBlock)
   {
      var lines = expectedBlock.Replace("\r\n", "\n").Replace("\r", "\n")
         .Split('\n');

      // Detect failure format by presence of ERROR_CODE.
      var map = new Dictionary<string, string>(StringComparer.Ordinal);
      var trace = new List<TraceEvent>();

      foreach(var raw in lines)
      {
         var line = raw.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith('#')) continue;

         // Header-style key: value
         var idx = line.IndexOf(':');
         if(idx > 0 && line[..idx].All(c => char.IsLetter(c) || c == '_'))
         {
            var k = line.Substring(0, idx).Trim();
            var v = line[(idx + 1)..].Trim();
            map[k] = v;
            continue;
         }

         // Trace line
         var (kind, slice) = ParseTraceLine(line);
         trace.Add(new TraceEvent(kind, slice));
      }

      if(map.Count > 0)
      {
         if(!map.TryGetValue("ERROR_CODE", out var code) || string.IsNullOrWhiteSpace(code))
            throw new FormatException("Expected ERROR_CODE in failure block.");

         if(!map.TryGetValue("ERROR_OFFSET", out var offStr))
            throw new FormatException("Expected ERROR_OFFSET in failure block.");

         var off = ParseInt(offStr, "ERROR_OFFSET") ?? throw new FormatException("ERROR_OFFSET is required.");

         int? lineNo = null;
         int? colNo = null;
         if(map.TryGetValue("ERROR_LINE", out var ln)) lineNo = ParseInt(ln, "ERROR_LINE");
         if(map.TryGetValue("ERROR_COLUMN", out var cn)) colNo = ParseInt(cn, "ERROR_COLUMN");

         return new StreamWalkExpected.Failure
         {
            ErrorCode = code,
            ErrorOffset = off,
            ErrorLine = lineNo,
            ErrorColumn = colNo,
         };
      }

      // Success: must include END_DOCUMENT exactly once.
      var endCount = trace.Count(e => e.Kind == "END_DOCUMENT");
      if(endCount != 1)
         throw new FormatException($"Success trace must contain END_DOCUMENT exactly once (found {endCount}).");

      return new StreamWalkExpected.Success { Trace = trace };
   }

   private static (string kind, string? slice) ParseTraceLine(string line)
   {
      // Forms: "KIND" or "KIND <slice>".
      var firstSpace = line.IndexOf(' ');
      if(firstSpace < 0) return (line, null);

      var kind = line.Substring(0, firstSpace).Trim();
      var rest = line[(firstSpace + 1)..].Trim();
      if(rest.Length == 0) return (kind, null);

      // Slice is a single token, e.g. b"...".
      return (kind, rest);
   }

   private static IEnumerable<string> EnumerateNonEmptyNonCommentLines(string text)
   {
      foreach(var raw in text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
      {
         var line = raw.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith('#')) continue;
         yield return raw;
      }
   }

   private static StreamWalkMode ParseMode(string value)
   {
      return value switch
      {
         "AJIS" => StreamWalkMode.AJIS,
         "JSON" => StreamWalkMode.JSON,
         _ => throw new FormatException($"Invalid MODE: {value}")
      };
   }

   private static bool ParseOnOff(string value)
   {
      return value switch
      {
         "on" => true,
         "off" => false,
         _ => throw new FormatException($"Invalid on/off value: {value}")
      };
   }

   private static int? ParseInt(string value, string key)
   {
      if(!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
         throw new FormatException($"Invalid integer for {key}: {value}");
      return n;
   }

   private static string ExtractSection(string src, string startHeader, string? endHeader, bool required)
   {
      int start = FindHeaderLine(src, startHeader);
      if(start < 0)
      {
         if(!required) return string.Empty;
         throw new FormatException($"Missing section header: {startHeader}");
      }

      int afterStart = SkipHeaderLine(src, start);
      int end = endHeader is null ? src.Length : FindHeaderLine(src, endHeader);
      if(endHeader is not null && end < 0)
         throw new FormatException($"Missing section header: {endHeader}");

      var block = src.Substring(afterStart, end - afterStart);
      // Trim a single leading newline if present.
      if(block.StartsWith("\n", StringComparison.Ordinal)) block = block[1..];
      return block.TrimEnd();
   }

   private static int FindHeaderLine(string src, string header)
   {
      // Find exact line match "# ..." at line start.
      var norm = src;
      int idx = 0;
      while(idx < norm.Length)
      {
         int lineEnd = norm.IndexOf('\n', idx);
         if(lineEnd < 0) lineEnd = norm.Length;

         var line = norm.Substring(idx, lineEnd - idx).TrimEnd();
         if(line == header) return idx;

         idx = lineEnd + 1;
      }
      return -1;
   }

   private static int SkipHeaderLine(string src, int headerStartIdx)
   {
      var end = src.IndexOf('\n', headerStartIdx);
      return end < 0 ? src.Length : end + 1;
   }

   private static string NormalizeCaseId(string path)
   {
      // CaseId should be stable and human readable.
      // Convert path separators to '/', remove extension.
      var p = path.Replace('\\', '/');
      var file = Path.GetFileNameWithoutExtension(p);
      var dir = Path.GetDirectoryName(p)?.Replace('\\', '/') ?? string.Empty;
      // Keep last two folders if present (family/name).
      var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
      if(parts.Length >= 2)
      {
         var fam = parts[^2] + "/" + parts[^1];
         return fam + "/" + file;
      }
      return file;
   }
}
