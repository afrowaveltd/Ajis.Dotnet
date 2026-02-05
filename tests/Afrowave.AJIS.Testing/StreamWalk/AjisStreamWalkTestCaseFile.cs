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
      public List<AjisStreamWalkTestTraceEvent> Trace { get; init; } = [];
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

   /// <summary>
   /// Renders a UTF-8 slice into the canonical test representation: b"...".
   /// Escapes: \\, \", \n, \r, \t and control bytes as \xNN.
   /// </summary>
   public static string RenderSlice(ReadOnlySpan<byte> utf8)
   {
      // Fast-path for empty
      if(utf8.Length == 0)
         return "b\"\"";

      StringBuilder sb = new(capacity: utf8.Length + 8);
      sb.Append("b\"");

      foreach(byte b in utf8)
      {
         switch(b)
         {
            case (byte)'\\': sb.Append("\\\\"); break;
            case (byte)'\"': sb.Append("\\\""); break;
            case (byte)'\n': sb.Append("\\n"); break;
            case (byte)'\r': sb.Append("\\r"); break;
            case (byte)'\t': sb.Append("\\t"); break;

            default:
               // Render ASCII control bytes deterministically as hex.
               if(b < 0x20)
               {
                  sb.Append("\\x");
                  sb.Append(b.ToString("X2"));
               }
               else
               {
                  // For test trace we keep bytes as-is (UTF-8 bytes),
                  // which is deterministic and matches "slice bytes" semantics.
                  sb.Append((char)b);
               }
               break;
         }
      }

      sb.Append('"');
      return sb.ToString();
   }

   public static AjisStreamWalkTestCase Load(string path)
   {
      string src = File.ReadAllText(path, Encoding.UTF8);
      string caseId = Path.GetFileName(path);

      string optionsBlock = ExtractSection(src, OptionsHeader, InputHeader, required: false);
      string inputBlock = ExtractSection(src, InputHeader, ExpectedHeader, required: true);
      string expectedBlock = ExtractSection(src, ExpectedHeader, null, required: true);

      AjisStreamWalkTestOptions options = ParseOptions(optionsBlock);
      byte[] inputUtf8 = Encoding.UTF8.GetBytes(inputBlock);
      AjisStreamWalkTestExpected expected = ParseExpected(expectedBlock);

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
      int headerIndex = src.IndexOf(header, StringComparison.Ordinal);
      if(headerIndex < 0)
      {
         if(required) throw new FormatException($"Missing required section: '{header}'");
         return string.Empty;
      }

      int start = headerIndex + header.Length;
      int end = nextHeader is null
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

      AjisStreamWalkTestOptions opt = AjisStreamWalkTestOptions.DefaultForM1;

      foreach(string rawLine in SplitLines(optionsBlock))
      {
         string line = rawLine.Trim();
         if(line.Length == 0) continue;
         if(line.StartsWith("//", StringComparison.Ordinal)) continue;
         if(line.StartsWith('#')) continue;

         // Supported separators: "KEY: value" (canonical) and "KEY=value" (accepted for convenience).
         int idx = line.IndexOf(':');
         if(idx < 0) idx = line.IndexOf('=');
         if(idx < 0) throw new FormatException($"Invalid option line (expected 'KEY: value'): '{line}'");

         string key = line[..idx].Trim();
         string value = line[(idx + 1)..].Trim();

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
      if(!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
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

      List<string> lines = [.. SplitLines(expectedBlock).Select(s => s.Trim()).Where(s => s.Length > 0)];
      if(lines.Count == 0) throw new FormatException("EXPECTED section is empty.");

      // Legacy: OK + TRACE:
      if(string.Equals(lines[0], "OK", StringComparison.OrdinalIgnoreCase))
      {
         List<AjisStreamWalkTestTraceEvent> trace = [];

         int traceIndex = lines.FindIndex(s => s.Equals("TRACE:", StringComparison.OrdinalIgnoreCase));
         int start = traceIndex >= 0 ? traceIndex + 1 : 1;

         for(int i = start; i < lines.Count; i++)
         {
            string ln = lines[i];
            if(ln.StartsWith("//", StringComparison.Ordinal)) continue;

            // kind [slice]
            int space = ln.IndexOf(' ');
            if(space < 0)
            {
               trace.Add(new AjisStreamWalkTestTraceEvent(ln, null));
               continue;
            }

            string kind = ln[..space].Trim();
            string rest = ln[(space + 1)..].Trim();
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
            string payload = lines[0]["ERROR:".Length..].Trim();

            // <code> @ <offset>
            int at = payload.IndexOf('@');
            if(at < 0) throw new FormatException($"Invalid ERROR line (expected 'ERROR: <code> @ <offset>'): '{lines[0]}'");

            string code = payload[..at].Trim();
            string offsetText = payload[(at + 1)..].Trim();
            if(!long.TryParse(offsetText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long offset))
               throw new FormatException($"Invalid offset in ERROR line: '{lines[0]}'");

            int? line = null;
            int? col = null;

            foreach(string? ln in lines.Skip(1))
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
         string payload2 = lines[0][5..].Trim(); // after "ERROR"
         if(payload2.Length == 0) throw new FormatException($"Invalid ERROR line: '{lines[0]}'");

         // Tokenize by spaces, keep key=value pairs.
         string[] parts = payload2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
         string code2 = parts[0].Trim();

         long? offset2 = null;
         int? line2 = null;
         int? col2 = null;

         foreach(string? part in parts.Skip(1))
         {
            int eq = part.IndexOf('=');
            if(eq < 0) continue;

            string k = part[..eq].Trim();
            string v = part[(eq + 1)..].Trim();

            if(k.Equals("offset", StringComparison.OrdinalIgnoreCase))
            {
               if(!long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long o))
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
         List<AjisStreamWalkTestTraceEvent> trace = [];
         foreach(string? ln0 in lines)
         {
            if(ln0.StartsWith("//", StringComparison.Ordinal)) continue;

            int space = ln0.IndexOf(' ');
            if(space < 0)
            {
               trace.Add(new AjisStreamWalkTestTraceEvent(ln0, null));
               continue;
            }

            string kind = ln0[..space].Trim();
            string rest = ln0[(space + 1)..].Trim();
            trace.Add(new AjisStreamWalkTestTraceEvent(kind, rest.Length == 0 ? null : rest));
         }

         return new AjisStreamWalkTestExpected.Success { Trace = trace };
      }
   }

   private static IEnumerable<string> SplitLines(string src)
   {
      using StringReader sr = new(src);
      while(true)
      {
         string? line = sr.ReadLine();
         if(line is null) yield break;
         yield return line;
      }
   }
}
