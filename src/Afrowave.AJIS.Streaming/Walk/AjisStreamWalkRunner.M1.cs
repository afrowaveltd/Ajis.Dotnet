#nullable enable

using System.Buffers;

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Milestone M1 implementation for StreamWalk.
/// Implements the canonical trace kinds defined in Docs/tests/streamwalk.md.
/// </summary>
public static partial class AjisStreamWalkRunner
{
   // ----------------------------
   // Public API delegates here (via AjisStreamWalkRunner.cs)
   // ----------------------------

   /// <summary>
   /// M1 entrypoint used by the API shell.
   ///
   /// NOTE: Because <see cref="AjisStreamWalkEvent"/> carries slices as <see cref="ReadOnlyMemory{T}"/>
   /// (to support stable referencing in tests), this overload materializes the input into a byte[] once.
   /// The primary performance path should call the ReadOnlyMemory overload (added later) or evolve the
   /// event contract to span-based slices.
   /// </summary>
   internal static void RunM1(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions)
   {
      // One-time materialization so events can reference stable ReadOnlyMemory slices.
      var materialized = inputUtf8.ToArray();
      RunM1(materialized, options, visitor, runnerOptions);
   }

   /// <summary>
   /// M1 core over a materialized buffer.
   /// </summary>
   internal static void RunM1(
      ReadOnlyMemory<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions)
   {
      ArgumentNullException.ThrowIfNull(visitor);

      // M1 only: we ignore AJIS extensions controlled by options flags.
      // The JSON-compatible core is always available.

      var src = inputUtf8;
      var s = src.Span;
      var i = 0;
      var depth = 0;

      try
      {
         SkipWs(s, ref i);

         if(i >= s.Length)
         {
            // Empty input is invalid for StreamWalk tests (EXPECTED must exist).
            Fail(visitor, "UnexpectedEndOfInput", offset: i);
            return;
         }

         if(!ParseValue(src, ref i, ref depth, options, visitor))
            return; // error already reported

         SkipWs(s, ref i);
         if(i != s.Length)
         {
            Fail(visitor, "TrailingGarbage", offset: i);
            return;
         }

         // Success: END_DOCUMENT must appear exactly once.
         visitor.OnEvent(new AjisStreamWalkEvent("END_DOCUMENT", default, i));
         visitor.OnCompleted();
      }
      catch(Exception ex)
      {
         // StreamWalk should never throw in normal operation.
         // Convert to a structured error for the harness.
         Fail(visitor, "UnexpectedToken", offset: i);

         // If you want diagnostics later, you can add a debug hook here.
         _ = ex;
      }
   }

   // ----------------------------
   // Parsing (M1)
   // ----------------------------

   private static bool ParseValue(
      ReadOnlyMemory<byte> src,
      ref int i,
      ref int depth,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor)
   {
      var s = src.Span;
      SkipWs(s, ref i);
      if(i >= s.Length)
      {
         Fail(visitor, "UnexpectedEndOfInput", offset: i);
         return false;
      }

      var b = s[i];
      switch(b)
      {
         case (byte)'{':
            return ParseObject(src, ref i, ref depth, options, visitor);

         case (byte)'[':
            return ParseArray(src, ref i, ref depth, options, visitor);

         case (byte)'"':
            {
               var offset = i;
               if(!TryParseString(src, ref i, options, out var decoded, out var errCode, out var errOffset))
               {
                  Fail(visitor, errCode, errOffset);
                  return false;
               }
               visitor.OnEvent(new AjisStreamWalkEvent("STRING", decoded, offset));
               return true;
            }
         case (byte)'-':
         case >= (byte)'0' and <= (byte)'9':
            {
               var start = i;
               if(!TryParseNumberToken(s, ref i, options, out var errCode, out var errOffset))
               {
                  Fail(visitor, errCode, errOffset);
                  return false;
               }
               var slice = src[start..i];
               visitor.OnEvent(new AjisStreamWalkEvent("NUMBER", slice, start));
               return true;
            }
         case (byte)'t':
            return ParseLiteral(s, ref i, "true", "TRUE", visitor);

         case (byte)'f':
            return ParseLiteral(s, ref i, "false", "FALSE", visitor);

         case (byte)'n':
            return ParseLiteral(s, ref i, "null", "NULL", visitor);

         default:
            Fail(visitor, "UnexpectedCharacter", offset: i);
            return false;
      }
   }

   private static bool ParseObject(
      ReadOnlyMemory<byte> src,
      ref int i,
      ref int depth,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor)
   {
      var s = src.Span;
      var start = i;
      i++; // '{'

      depth++;
      if(depth > options.MaxDepth)
      {
         Fail(visitor, "UnexpectedToken", start);
         return false;
      }

      visitor.OnEvent(new AjisStreamWalkEvent("BEGIN_OBJECT", default, start));

      SkipWs(s, ref i);
      if(i >= s.Length)
      {
         Fail(visitor, "UnexpectedEndOfInput", i);
         return false;
      }

      if(s[i] == (byte)'}')
      {
         var end = i;
         i++;
         depth--;
         visitor.OnEvent(new AjisStreamWalkEvent("END_OBJECT", default, end));
         return true;
      }

      while(true)
      {
         SkipWs(s, ref i);
         if(i >= s.Length)
         {
            Fail(visitor, "UnexpectedEndOfInput", i);
            return false;
         }

         if(s[i] != (byte)'"')
         {
            Fail(visitor, "UnexpectedToken", i);
            return false;
         }

         var nameOffset = i;
         if(!TryParseString(src, ref i, options, out var nameDecoded, out var errCode, out var errOffset))
         {
            Fail(visitor, errCode, errOffset);
            return false;
         }

         visitor.OnEvent(new AjisStreamWalkEvent("NAME", nameDecoded, nameOffset));

         SkipWs(s, ref i);
         if(i >= s.Length)
         {
            Fail(visitor, "UnexpectedEndOfInput", i);
            return false;
         }

         if(s[i] != (byte)':')
         {
            Fail(visitor, "UnexpectedToken", i);
            return false;
         }

         i++; // ':'

         if(!ParseValue(src, ref i, ref depth, options, visitor))
            return false;

         SkipWs(s, ref i);
         if(i >= s.Length)
         {
            Fail(visitor, "UnexpectedEndOfInput", i);
            return false;
         }

         if(s[i] == (byte)',')
         {
            i++;
            continue;
         }

         if(s[i] == (byte)'}')
         {
            var end = i;
            i++;
            depth--;
            visitor.OnEvent(new AjisStreamWalkEvent("END_OBJECT", default, end));
            return true;
         }

         Fail(visitor, "UnexpectedToken", i);
         return false;
      }
   }

   private static bool ParseArray(
      ReadOnlyMemory<byte> src,
      ref int i,
      ref int depth,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor)
   {
      var s = src.Span;
      var start = i;
      i++; // '['

      depth++;
      if(depth > options.MaxDepth)
      {
         Fail(visitor, "UnexpectedToken", start);
         return false;
      }

      visitor.OnEvent(new AjisStreamWalkEvent("BEGIN_ARRAY", default, start));

      SkipWs(s, ref i);
      if(i >= s.Length)
      {
         Fail(visitor, "UnexpectedEndOfInput", i);
         return false;
      }

      if(s[i] == (byte)']')
      {
         var end = i;
         i++;
         depth--;
         visitor.OnEvent(new AjisStreamWalkEvent("END_ARRAY", default, end));
         return true;
      }

      while(true)
      {
         if(!ParseValue(src, ref i, ref depth, options, visitor))
            return false;

         SkipWs(s, ref i);
         if(i >= s.Length)
         {
            Fail(visitor, "UnexpectedEndOfInput", i);
            return false;
         }

         if(s[i] == (byte)',')
         {
            i++;
            SkipWs(s, ref i);
            continue;
         }

         if(s[i] == (byte)']')
         {
            var end = i;
            i++;
            depth--;
            visitor.OnEvent(new AjisStreamWalkEvent("END_ARRAY", default, end));
            return true;
         }

         Fail(visitor, "UnexpectedToken", i);
         return false;
      }
   }

   private static bool ParseLiteral(ReadOnlySpan<byte> s, ref int i, string literal, string kind, IAjisStreamWalkVisitor visitor)
   {
      var start = i;
      if(!MatchAscii(s, ref i, literal))
      {
         Fail(visitor, "UnexpectedToken", start);
         return false;
      }

      visitor.OnEvent(new AjisStreamWalkEvent(kind, default, start));
      return true;
   }

   // ----------------------------
   // Token helpers
   // ----------------------------

   private static bool TryParseNumberToken(ReadOnlySpan<byte> s, ref int i, AjisStreamWalkOptions options, out string errCode, out int errOffset)
   {
      // Minimal JSON number lexer (M1): -? int frac? exp?
      // We keep slice as lexical token.
      errCode = "";
      errOffset = i;

      var start = i;
      if(s[i] == (byte)'-')
      {
         i++;
         if(i >= s.Length) { errCode = "UnexpectedEndOfInput"; errOffset = i; return false; }
      }

      if(s[i] == (byte)'0')
      {
         i++;
      }
      else if(s[i] >= (byte)'1' && s[i] <= (byte)'9')
      {
         i++;
         while(i < s.Length && s[i] >= (byte)'0' && s[i] <= (byte)'9') i++;
      }
      else
      {
         errCode = "UnexpectedCharacter";
         errOffset = i;
         return false;
      }

      if(i < s.Length && s[i] == (byte)'.')
      {
         i++;
         if(i >= s.Length) { errCode = "UnexpectedEndOfInput"; errOffset = i; return false; }
         if(s[i] < (byte)'0' || s[i] > (byte)'9') { errCode = "UnexpectedToken"; errOffset = i; return false; }
         while(i < s.Length && s[i] >= (byte)'0' && s[i] <= (byte)'9') i++;
      }

      if(i < s.Length && (s[i] == (byte)'e' || s[i] == (byte)'E'))
      {
         i++;
         if(i >= s.Length) { errCode = "UnexpectedEndOfInput"; errOffset = i; return false; }
         if(s[i] == (byte)'+' || s[i] == (byte)'-')
         {
            i++;
            if(i >= s.Length) { errCode = "UnexpectedEndOfInput"; errOffset = i; return false; }
         }
         if(s[i] < (byte)'0' || s[i] > (byte)'9') { errCode = "UnexpectedToken"; errOffset = i; return false; }
         while(i < s.Length && s[i] >= (byte)'0' && s[i] <= (byte)'9') i++;
      }

      if(i - start > options.MaxTokenBytes)
      {
         // M1 doesn't define a dedicated limit code yet; treat as UnexpectedToken.
         errCode = "UnexpectedToken";
         errOffset = start;
         return false;
      }

      return true;
   }

   private static bool TryParseString(
      ReadOnlyMemory<byte> src,
      ref int i,
      AjisStreamWalkOptions options,
      out ReadOnlyMemory<byte> decoded,
      out string errCode,
      out int errOffset)
   {
      var s = src.Span;
      decoded = default;
      errCode = "";
      errOffset = i;

      if(i >= s.Length || s[i] != (byte)'"')
      {
         errCode = "UnexpectedToken";
         errOffset = i;
         return false;
      }

      i++; // opening quote
      var contentStart = i;
      var needsUnescape = false;

      while(i < s.Length)
      {
         var b = s[i];
         if(b == (byte)'"')
         {
            var contentEnd = i;
            i++; // closing quote

            if(!needsUnescape)
            {
               decoded = src[contentStart..contentEnd];
               return decoded.Length <= options.MaxTokenBytes;
            }

            // Unescape into pooled buffer
            return UnescapeString(src[contentStart..contentEnd], options, out decoded, out errCode, out errOffset);
         }

         if(b == (byte)'\\')
         {
            needsUnescape = true;
            i++;
            if(i >= s.Length)
            {
               errCode = "UnexpectedEndOfInput";
               errOffset = i;
               return false;
            }
            i++;
            continue;
         }

         // Control chars not allowed in JSON strings
         if(b < 0x20)
         {
            errCode = "UnexpectedCharacter";
            errOffset = i;
            return false;
         }

         i++;
      }

      errCode = "UnexpectedEndOfInput";
      errOffset = i;
      return false;
   }

   private static bool UnescapeString(
      ReadOnlyMemory<byte> raw,
      AjisStreamWalkOptions options,
      out ReadOnlyMemory<byte> decoded,
      out string errCode,
      out int errOffset)
   {
      decoded = default;
      errCode = "";
      errOffset = 0;

      var s = raw.Span;
      // Worst case: decoded length <= raw length
      var rented = ArrayPool<byte>.Shared.Rent(s.Length);
      var w = 0;

      try
      {
         for(var r = 0; r < s.Length; r++)
         {
            var b = s[r];
            if(b != (byte)'\\')
            {
               rented[w++] = b;
               continue;
            }

            // escape
            r++;
            if(r >= s.Length)
            {
               errCode = "UnexpectedEndOfInput";
               errOffset = raw.Length; // relative
               return false;
            }

            var e = s[r];
            switch(e)
            {
               case (byte)'"': rented[w++] = (byte)'"'; break;
               case (byte)'\\': rented[w++] = (byte)'\\'; break;
               case (byte)'/': rented[w++] = (byte)'/'; break;
               case (byte)'b': rented[w++] = 0x08; break;
               case (byte)'f': rented[w++] = 0x0C; break;
               case (byte)'n': rented[w++] = (byte)'\n'; break;
               case (byte)'r': rented[w++] = (byte)'\r'; break;
               case (byte)'t': rented[w++] = (byte)'\t'; break;
               case (byte)'u':
                  {
                     // \uXXXX
                     if(r + 4 >= s.Length)
                     {
                        errCode = "UnexpectedEndOfInput";
                        errOffset = r;
                        return false;
                     }

                     var hex = s.Slice(r + 1, 4);
                     if(!TryParseHex4(hex, out var codeUnit))
                     {
                        errCode = "InvalidEscapeSequence";
                        errOffset = r - 1;
                        return false;
                     }

                     r += 4;

                     // Encode as UTF-8
                     var ch = (char)codeUnit;
                     w += EncodeUtf8(ch, rented.AsSpan(w));
                     break;
                  }
               default:
                  errCode = "InvalidEscapeSequence";
                  errOffset = r - 1;
                  return false;
            }

            if(w > options.MaxTokenBytes)
            {
               errCode = "UnexpectedToken";
               errOffset = 0;
               return false;
            }
         }

         var arr = new byte[w];
         Buffer.BlockCopy(rented, 0, arr, 0, w);
         decoded = arr;
         return true;
      }
      finally
      {
         ArrayPool<byte>.Shared.Return(rented);
      }
   }

   private static int EncodeUtf8(char ch, Span<byte> dest)
   {
      // M1: BMP only (sufficient for test corpus right now).
      // If you later want full surrogate handling, we extend here.
      if(ch <= 0x7F)
      {
         dest[0] = (byte)ch;
         return 1;
      }
      if(ch <= 0x7FF)
      {
         dest[0] = (byte)(0xC0 | (ch >> 6));
         dest[1] = (byte)(0x80 | (ch & 0x3F));
         return 2;
      }

      dest[0] = (byte)(0xE0 | (ch >> 12));
      dest[1] = (byte)(0x80 | ((ch >> 6) & 0x3F));
      dest[2] = (byte)(0x80 | (ch & 0x3F));
      return 3;
   }

   private static bool TryParseHex4(ReadOnlySpan<byte> hex, out int value)
   {
      value = 0;
      for(var i = 0; i < 4; i++)
      {
         var c = hex[i];
         int v;
         if(c >= (byte)'0' && c <= (byte)'9') v = c - (byte)'0';
         else if(c >= (byte)'A' && c <= (byte)'F') v = 10 + (c - (byte)'A');
         else if(c >= (byte)'a' && c <= (byte)'f') v = 10 + (c - (byte)'a');
         else return false;
         value = (value << 4) | v;
      }
      return true;
   }

   private static void SkipWs(ReadOnlySpan<byte> s, ref int i)
   {
      while(i < s.Length)
      {
         var b = s[i];
         if(b == (byte)' ' || b == (byte)'\t' || b == (byte)'\r' || b == (byte)'\n')
            i++;
         else
            break;
      }
   }

   private static bool MatchAscii(ReadOnlySpan<byte> s, ref int i, string literal)
   {
      var start = i;
      for(var j = 0; j < literal.Length; j++)
      {
         if(start + j >= s.Length) return false;
         if(s[start + j] != (byte)literal[j]) return false;
      }
      i += literal.Length;
      return true;
   }

   private static void Fail(IAjisStreamWalkVisitor visitor, string code, int offset)
   {
      visitor.OnError(new AjisStreamWalkError(code, offset, Line: null, Column: null));
   }
}