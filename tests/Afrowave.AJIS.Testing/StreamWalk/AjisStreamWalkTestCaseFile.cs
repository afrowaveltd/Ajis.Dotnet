// File: tests/Afrowave.AJIS.Testing/StreamWalk/AjisStreamWalkTestCaseFile.cs
#nullable enable

using System.Globalization;
using System.Text;

namespace Afrowave.AJIS.Testing.StreamWalk;

/// <summary>
/// Represents one StreamWalk test case loaded from a <c>.case</c> file.
/// Canonical format is defined in <c>Docs/tests/streamwalk.md</c> (M1).
/// 
/// NOTE: All types in the test harness use an <c>AjisStreamWalkTest*</c> prefix
/// to avoid name collisions with production types living in Afrowave.AJIS.Streaming.
/// </summary>
public sealed class AjisStreamWalkTestCase
{
   public required string CaseId { get; init; }
   public required AjisStreamWalkTestOptions Options { get; init; }
   public required byte[] InputUtf8 { get; init; }
   public required AjisStreamWalkTestExpected Expected { get; init; }

   public override string ToString() => CaseId;
}

/// <summary>
/// Options section (M1). Unspecified values mean "use implementation defaults".
/// </summary>
public sealed record AjisStreamWalkTestOptions
{
   public AjisStreamWalkTestMode? Mode { get; init; }
   public bool? Comments { get; init; }
   public bool? Directives { get; init; }
   public bool? Identifiers { get; init; }
   public int? MaxDepth { get; init; }
   public int? MaxTokenBytes { get; init; }

   public static AjisStreamWalkTestOptions DefaultForM1 => new()
   {
      Mode = AjisStreamWalkTestMode.AJIS,
      Comments = false,
      Directives = false,
      Identifiers = false,
      // limits: left null on purpose (implementation defaults)
   };
}

public enum AjisStreamWalkTestMode
{
   AJIS = 0,
   JSON = 1,
}

/// <summary>
/// Expected outcome: either Success (trace) or Failure (error).
/// </summary>
public abstract class AjisStreamWalkTestExpected
{
   private AjisStreamWalkTestExpected() { }

   public sealed class Success : AjisStreamWalkTestExpected
   {
      public required IReadOnlyList<AjisStreamWalkTestTraceEvent> Trace { get; init; }
   }

   public sealed class Failure : AjisStreamWalkTestExpected
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
public readonly record struct AjisStreamWalkTestTraceEvent(string Kind, string? Slice);

/// <summary>
/// Loader for StreamWalk <c>.case</c> files.
/// </summary>
public static class AjisStreamWalkTestCaseFile
{
   private const string OptionsHeader = "# OPTIONS";
   private const string InputHeader = "# INPUT";
   private const string ExpectedHeader = "# EXPECTED";

   /// <summary>
   /// Loads a <c>.case</c> file from disk.
   /// </summary>
   public static AjisStreamWalkTestCase Load(string path)
   {
      if(string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
      var text = File.ReadAllText(path, Encoding.UTF8);
      return Parse(text, caseId: NormalizeCaseId(path));
   }

   /// <summary>
   /// Parses a <c>.case</c> file text (UTF-8 expected) into a structured representation.
   /// </summary>
   public static AjisStreamWalkTestCase Parse(string caseFileText, string caseId)
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

      return new AjisStreamWalkTestCase
      {
         CaseId = caseId,
         Options = options,
         InputUtf8 = inputUtf8,
         Expected = expected,
      };
   }

   /// <summary>
   /// Renders a UTF-8 byte slice into the canonical <c>b\"...\"</c> trace form.
   /// </summary>
   public static string RenderSlice(ReadOnlySpan<byte> slice)
   {
      // We follow the same escape rules as JSON for the textual representation,
      // but keep the outer wrapper b"..." to emphasize it is a byte slice.
      var sb = new StringBuilder(slice.Length + 8);
      sb.Append('b').Append('"');

      for(var i = 0; i < slice.Length; i++)
      {
         var b = slice[i];
         switch(b)
         {
            case (byte)'\\': sb.Append("\\\\"); break;
            case (byte)'\"': sb.Append("\\\""); break;
            case (byte)'\n': sb.Append("\\n"); break;
            case (byte)'\r': sb.Append("\\r"); break;
            case (byte)'\t': sb.Append("\\t"); break;
            default:
               if(b < 0x20)
               {
                  sb.Append("\\u00");
                  sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
               }
               else
               {
                  sb.Append((char)b);
               }
               break;
         }
      }

      sb.Append('"');
      return sb.ToString();
   }

   // ----------------------------
   // Parsing helpers
   // ----------------------------

   private static AjisStreamWalkTestOptions ParseOptions(string optionsBlock)
   {
      if(string.IsNullOrWhiteSpace(optionsBlock))
      {
         // If OPTIONS exists but is empty, still use M1 defaults.
         return AjisStreamWalkTestOptions.DefaultForM1;
      }

      var opt = AjisStreamWalkTestOptions.DefaultForM1;

      foreach(var rawLine in SplitLines(optionsBlock))
      {
         var line = rawLine.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith("#", StringComparison.Ordinal)) continue;

         var idx = line.IndexOf('=');
         if(idx < 0) throw new FormatException($"Invalid option line (expected key=value): '{line}'");

         var key = line[..idx].Trim();
         var value = line[(idx + 1)..].Trim();

         // We keep keys case-insensitive.
         switch(key.ToUpperInvariant())
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
               throw new FormatException($"Unknown option key: '{key}'");
         }
      }

      return opt;
   }

   private static AjisStreamWalkTestMode ParseMode(string value)
   {
      if(string.Equals(value, "AJIS", StringComparison.OrdinalIgnoreCase)) return AjisStreamWalkTestMode.AJIS;
      if(string.Equals(value, "JSON", StringComparison.OrdinalIgnoreCase)) return AjisStreamWalkTestMode.JSON;
      throw new FormatException($"Invalid MODE value: '{value}' (expected AJIS|JSON)");
   }

   private static bool ParseOnOff(string value)
   {
      if(string.Equals(value, "ON", StringComparison.OrdinalIgnoreCase)) return true;
      if(string.Equals(value, "OFF", StringComparison.OrdinalIgnoreCase)) return false;
      if(string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)) return true;
      if(string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase)) return false;
      throw new FormatException($"Invalid boolean value: '{value}' (expected ON|OFF)");
   }

   private static int ParseInt(string value, string key)
   {
      if(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
      throw new FormatException($"Invalid integer value for {key}: '{value}'");
   }

   private static AjisStreamWalkTestExpected ParseExpected(string expectedBlock)
   {
      // Format (M1):
      //   OK
      //   TRACE:
      //     <kind> [<slice>]
      // or
      //   ERROR: <code> @ <offset>
      //   (optional: LINE=<n> COL=<n>)

      var lines = SplitLines(expectedBlock).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
      if(lines.Count == 0) throw new FormatException("EXPECTED section is empty.");

      if(string.Equals(lines[0], "OK", StringComparison.OrdinalIgnoreCase))
      {
         var trace = new List<AjisStreamWalkTestTraceEvent>();

         var traceIndex = lines.FindIndex(s => s.Equals("TRACE:", StringComparison.OrdinalIgnoreCase));
         if(traceIndex >= 0)
         {
            for(var i = traceIndex + 1; i < lines.Count; i++)
            {
               var ln = lines[i];
               if(ln.StartsWith("//", StringComparison.Ordinal)) continue;

               // kind [slice]
               var space = ln.IndexOf(' ');
               if(space < 0)
               {
                  trace.Add(new AjisStreamWalkTestTraceEvent(ln, null));
                  continue;
               }

               var kind = ln[..space].Trim();
               var rest = ln[(space + 1)..].Trim();
               trace.Add(new AjisStreamWalkTestTraceEvent(kind, rest.Length == 0 ? null : rest));
            }
         }

         return new AjisStreamWalkTestExpected.Success { Trace = trace };
      }

      if(lines[0].StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
      {
         var payload = lines[0]["ERROR:".Length..].Trim();
         // <code> @ <offset>
         var at = payload.IndexOf('@');
         if(at < 0) throw new FormatException($"Invalid ERROR line (expected 'ERROR: <code> @ <offset>'): '{lines[0]}'");

         var code = payload[..at].Trim();
         var offsetText = payload[(at + 1)..].Trim();
         if(!int.TryParse(offsetText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
            throw new FormatException($"Invalid ERROR offset: '{offsetText}'");

         int? lineNo = null;
         int? colNo = null;

         foreach(var ln in lines.Skip(1))
         {
            // optional: LINE=<n> COL=<n>
            var parts = ln.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach(var p in parts)
            {
               if(p.StartsWith("LINE=", StringComparison.OrdinalIgnoreCase))
               {
                  var v = p["LINE=".Length..];
                  if(int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) lineNo = n;
               }
               else if(p.StartsWith("COL=", StringComparison.OrdinalIgnoreCase) || p.StartsWith("COLUMN=", StringComparison.OrdinalIgnoreCase))
               {
                  var v = p.Contains('=') ? p[(p.IndexOf('=') + 1)..] : string.Empty;
                  if(int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)) colNo = n;
               }
            }
         }

         return new AjisStreamWalkTestExpected.Failure
         {
            ErrorCode = code,
            ErrorOffset = offset,
            ErrorLine = lineNo,
            ErrorColumn = colNo,
         };
      }

      throw new FormatException($"Unknown EXPECTED header: '{lines[0]}'");
   }

   private static IEnumerable<string> SplitLines(string src)
   {
      // src already normalized to \n
      using var sr = new StringReader(src);
      string? line;
      while((line = sr.ReadLine()) is not null)
      {
         yield return line;
      }
   }

   private static string ExtractSection(string src, string header, string? nextHeader, bool required)
   {
      var start = src.IndexOf(header, StringComparison.OrdinalIgnoreCase);
      if(start < 0)
      {
         if(required) throw new FormatException($"Missing required section: {header}");
         return string.Empty;
      }

      start += header.Length;

      var end = nextHeader is null
         ? src.Length
         : src.IndexOf(nextHeader, start, StringComparison.OrdinalIgnoreCase);

      if(end < 0)
      {
         if(required) throw new FormatException($"Missing required section: {nextHeader}");
         end = src.Length;
      }

      var block = src.Substring(start, end - start);
      // Trim a single leading newline for convenience
      if(block.StartsWith("\n", StringComparison.Ordinal)) block = block[1..];
      return block.TrimEnd('\n');
   }

   private static string NormalizeCaseId(string path)
   {
      // keep stable id independent of OS path separators
      var name = Path.GetFileNameWithoutExtension(path);
      return name.Replace(' ', '_');
   }
}
