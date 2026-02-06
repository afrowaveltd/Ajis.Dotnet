#nullable enable

using System.Text;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Escapes AJIS strings for JSON-compatible output.
/// </summary>
internal static class AjisTextEscaper
{
   public static string Escape(string value)
   {
      ArgumentNullException.ThrowIfNull(value);

      var builder = new StringBuilder(value.Length + 8);
      foreach(char c in value)
      {
         switch(c)
         {
            case '\\':
               builder.Append("\\\\");
               break;
            case '"':
               builder.Append("\\\"");
               break;
            case '\n':
               builder.Append("\\n");
               break;
            case '\r':
               builder.Append("\\r");
               break;
            case '\t':
               builder.Append("\\t");
               break;
            default:
               if(c < 0x20)
               {
                  builder.Append("\\u");
                  builder.Append(((int)c).ToString("X4"));
               }
               else
               {
                  builder.Append(c);
               }
               break;
         }
      }

      return builder.ToString();
   }

   public static string EscapeUtf8(ReadOnlySpan<byte> utf8)
   {
      string decoded = Encoding.UTF8.GetString(utf8);
      return Escape(decoded);
   }
}
