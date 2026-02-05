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
   public string CaseId { get; init; } = string.Empty;
   public AjisStreamWalkTestOptions Options { get; init; } = new();
   public ReadOnlyMemory<byte> InputUtf8 { get; init; } = ReadOnlyMemory<byte>.Empty;
   public AjisStreamWalkTestExpected Expected { get; init; } = null!;

   public override string ToString() => CaseId;
}

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
   LAX = 2,
}

public abstract record AjisStreamWalkTestExpected
{
   public sealed record Success : AjisStreamWalkTestExpected
   {
      public List<AjisStreamWalkTestTraceEvent> Trace { get; init; } = new();
   }

   public sealed record Failure : AjisStreamWalkTestExpected
   {
      public string Code { get; init; } = string.Empty;
      public long Offset { get; init; }
      public int? Line { get; init; }
      public int? Col { get; init; }
   }
}

public sealed record AjisStreamWalkTestTraceEvent(string Kind, string? Slice);

public static class AjisStreamWalkTestCaseFile
{
   private const string OptionsHeader = "# OPTIONS";
   private const string InputHeader = "# INPUT";
   private const string ExpectedHeader = "# EXPECTED";

   public static AjisStreamWalkTestCase Load(string path)
   {
      var src = File.ReadAllText(path, Encoding.UTF8);
      var caseId = Path.GetFileName(path);

      var optionsBlock = ExtractSection(src, OptionsHeader, InputHeader, required: false);
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

   private static string ExtractSection(string src, string header, string? nextHeader, bool required)
   {
      var headerIndex = src.IndexOf(header, StringComparison.Ordinal);
      if(headerIndex < 0)
      {
         if(required) throw new FormatException($"Missing required section: '{header}'");
         return string.Empty;
      }

      var start = headerIndex + header.Length;
      var end = nextHeader is null
         ? src.Length
         : src.IndexOf(nextHeader, start, StringComparison.Ordinal);

      if(end < 0)
      {
         if(nextHeader is not null)
            throw new FormatException($"Missing section terminator '{nextHeader}' for '{header}'");

         end = src.Length;
      }

      return src[start..end].Trim();
   }

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
         if(line.StartsWith('#')) continue;

         // Supported separators: "KEY: value" (canonical) and "KEY=value" (accepted for convenience).
         var idx = line.IndexOf(':');
         if(idx < 0) idx = line.IndexOf('=');
         if(idx < 0) throw new FormatException($"Invalid option line (expected 'KEY: value'): '{line}'");

         var key = line[..idx].Trim();
         var value = line[(idx + 1)..].Trim();

         // Canonical docs show uppercase keys, but we accept case-insensitive for author convenience.
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
               // Forward-compatible: unknown keys are ignored (future additions won't break older runners).
               break;
         }
      }

      return opt;
   }

   private static AjisStreamWalkTestMode ParseMode(string value)
   {
      if(string.Equals(value, "AJIS", StringComparison.OrdinalIgnoreCase)) return AjisStreamWalkTestMode.AJIS;
      if(string.Equals(value, "JSON", StringComparison.OrdinalIgnoreCase)) return AjisStreamWalkTestMode.JSON;
      if(string.Equals(value, "LAX", StringComparison.OrdinalIgnoreCase)) return AjisStreamWalkTestMode.LAX;
      throw new FormatException($"Invalid MODE value: '{value}' (expected AJIS|JSON|LAX)");
   }

   private static bool ParseOnOff(string value)
   {
      if(string.Equals(value, "ON", StringComparison.OrdinalIgnoreCase)) return true;
      if(string.Equals(value, "OFF", StringComparison.OrdinalIgnoreCase)) return false;
      if(string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)) return true;
      if(string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase)) return false;

      throw new FormatException($"Invalid bool value: '{value}' (expected on|off|true|false)");
   }

   private static int ParseInt(string value, string key)
   {
      if(!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
         throw new FormatException($"Invalid integer value for {key}: '{value}'");

      return n;
   }

   private static AjisStreamWalkTestExpected ParseExpected(string expectedBlock)
   {
      // Canonical StreamWalk M1 expected formats:
      //
      // SUCCESS (most common):
      //   <TRACE_LINE_1>
      //   <TRACE_LINE_2>
      //   ...
      //
      // FAILURE:
      //   ERROR <Code> offset=<n> [line=<n> col=<n>]
      //
      // Accepted legacy formats (for compatibility with older drafts):
      //   OK
      //   TRACE:
      //     <kind> [<slice>]
      //
      //   ERROR: <code> @ <offset> (optional: LINE=<n> COL=<n>)

      var lines = SplitLines(expectedBlock).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
      if(lines.Count == 0) throw new FormatException("EXPECTED section is empty.");

      // Legacy: OK + TRACE:
      if(string.Equals(lines[0], "OK", StringComparison.OrdinalIgnoreCase))
      {
         var trace = new List<AjisStreamWalkTestTraceEvent>();

         var traceIndex = lines.FindIndex(s => s.Equals("TRACE:", StringComparison.OrdinalIgnoreCase));
         var start = traceIndex >= 0 ? traceIndex + 1 : 1;

         for(var i = start; i < lines.Count; i++)
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

         return new AjisStreamWalkTestExpected.Success { Trace = trace };
      }

      // Failure (canonical): "ERROR <Code> offset=<n> ..."
      if(lines[0].StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
      {
         // Legacy: "ERROR: <code> @ <offset> ..."
         if(lines[0].StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase))
         {
            var payload = lines[0]["ERROR:".Length..].Trim();

            // <code> @ <offset>
            var at = payload.IndexOf('@');
            if(at < 0) throw new FormatException($"Invalid ERROR line (expected 'ERROR: <code> @ <offset>'): '{lines[0]}'");

            var code = payload[..at].Trim();
            var offsetText = payload[(at + 1)..].Trim();
            if(!long.TryParse(offsetText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset))
               throw new FormatException($"Invalid offset in ERROR line: '{lines[0]}'");

            int? line = null;
            int? col = null;

            foreach(var ln in lines.Skip(1))
            {
               if(ln.StartsWith("//", StringComparison.Ordinal)) continue;

               if(ln.StartsWith("LINE=", StringComparison.OrdinalIgnoreCase))
               {
                  line = ParseInt(ln["LINE=".Length..].Trim(), "LINE");
                  continue;
               }

               if(ln.StartsWith("COL=", StringComparison.OrdinalIgnoreCase))
               {
                  col = ParseInt(ln["COL=".Length..].Trim(), "COL");
                  continue;
               }
            }

            return new AjisStreamWalkTestExpected.Failure
            {
               Code = code,
               Offset = offset,
               Line = line,
               Col = col,
            };
         }

         // Canonical: "ERROR <Code> offset=<n> [line=<n> col=<n>]"
         var payload2 = lines[0].Substring(5).Trim(); // after "ERROR"
         if(payload2.Length == 0) throw new FormatException($"Invalid ERROR line: '{lines[0]}'");

         // Tokenize by spaces, keep key=value pairs.
         var parts = payload2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
         var code2 = parts[0].Trim();

         long? offset2 = null;
         int? line2 = null;
         int? col2 = null;

         foreach(var part in parts.Skip(1))
         {
            var eq = part.IndexOf('=');
            if(eq < 0) continue;

            var k = part[..eq].Trim();
            var v = part[(eq + 1)..].Trim();

            if(k.Equals("offset", StringComparison.OrdinalIgnoreCase))
            {
               if(!long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var o))
                  throw new FormatException($"Invalid offset in ERROR line: '{lines[0]}'");
               offset2 = o;
               continue;
            }

            if(k.Equals("line", StringComparison.OrdinalIgnoreCase))
            {
               line2 = ParseInt(v, "line");
               continue;
            }

            if(k.Equals("col", StringComparison.OrdinalIgnoreCase))
            {
               col2 = ParseInt(v, "col");
               continue;
            }
         }

         if(offset2 is null)
            throw new FormatException($"Invalid ERROR line (missing offset=<n>): '{lines[0]}'");

         return new AjisStreamWalkTestExpected.Failure
         {
            Code = code2,
            Offset = offset2.Value,
            Line = line2,
            Col = col2,
         };
      }

      // Canonical success: trace lines directly.
      {
         var trace = new List<AjisStreamWalkTestTraceEvent>();
         foreach(var ln0 in lines)
         {
            if(ln0.StartsWith("//", StringComparison.Ordinal)) continue;

            var space = ln0.IndexOf(' ');
            if(space < 0)
            {
               trace.Add(new AjisStreamWalkTestTraceEvent(ln0, null));
               continue;
            }

            var kind = ln0[..space].Trim();
            var rest = ln0[(space + 1)..].Trim();
            trace.Add(new AjisStreamWalkTestTraceEvent(kind, rest.Length == 0 ? null : rest));
         }

         return new AjisStreamWalkTestExpected.Success { Trace = trace };
      }
   }

   private static IEnumerable<string> SplitLines(string src)
   {
      using var sr = new StringReader(src);
      while(true)
      {
         var line = sr.ReadLine();
         if(line is null) yield break;
         yield return line;
      }
   }
}
