#nullable enable

namespace Afrowave.AJIS.Streaming.Walk;

/// <summary>
/// Milestone M1: JSON-core StreamWalk (objects, arrays, strings, numbers, true/false/null).
/// </summary>
/// <remarks>
/// AJIS extensions (comments, directives, identifiers, typed literals) are handled in later milestones.
/// </remarks>
internal static class AjisStreamWalkRunnerM1
{
   public static void Run(
      ReadOnlySpan<byte> inputUtf8,
      AjisStreamWalkOptions options,
      IAjisStreamWalkVisitor visitor,
      AjisStreamWalkRunnerOptions runnerOptions)
   {
      State st = new(inputUtf8, options, visitor, runnerOptions);

      st.SkipWs();
      if(st.AtEnd)
      {
         st.Fail("unexpected_eof", st.Offset);
         return;
      }

      st.ParseValue(depth: 0);
      if(st.Failed) return;

      st.SkipWs();
      if(!st.AtEnd)
      {
         st.Fail("trailing_garbage", st.Offset);
         return;
      }

      st.Emit("END_DOCUMENT", AjisSliceUtf8.Empty, st.Offset);
      visitor.OnCompleted();
   }

   private ref struct State
   {
      private readonly ReadOnlySpan<byte> _src;
      private readonly AjisStreamWalkOptions _opt;
      private readonly IAjisStreamWalkVisitor _v;
      private readonly AjisStreamWalkRunnerOptions _runnerOpt;

      private int _i;
      private bool _failed;
      private bool _lineStart;

      public State(
         ReadOnlySpan<byte> src,
         AjisStreamWalkOptions opt,
         IAjisStreamWalkVisitor v,
         AjisStreamWalkRunnerOptions runnerOpt)
      {
         _src = src;
         _opt = opt;
         _v = v;
         _runnerOpt = runnerOpt;
         _i = 0;
         _failed = false;
         _lineStart = true;
      }

      public readonly bool Failed => _failed;
      public readonly long Offset => _i;
      public readonly bool AtEnd => _i >= _src.Length;

      public void SkipWs()
      {
         while(_i < _src.Length)
         {
            byte b = _src[_i];
            if(b is (byte)' ' or (byte)'\t')
            {
               _i++;
               continue;
            }

            if(b is (byte)'\r' or (byte)'\n')
            {
               _i++;
               _lineStart = true;
               continue;
            }

            if(_opt.Comments && b == (byte)'/' && TryParseComment())
               continue;

            if(_opt.Directives && _lineStart && b == (byte)'#' && TryParseDirective())
               continue;

            break;
         }
      }

      private bool TryParseComment()
      {
         if(_i + 1 >= _src.Length)
            return false;

         byte next = _src[_i + 1];
         if(next != (byte)'/' && next != (byte)'*')
            return false;

         int start = _i;
         _i += 2;
         int contentStart = _i;

         if(next == (byte)'/')
         {
            while(_i < _src.Length)
            {
               byte b = _src[_i];
               if(b == (byte)'\n')
                  break;
               _i++;
            }

            ReadOnlySpan<byte> content = TrimWhitespace(_src[contentStart.._i], trimStart: false, trimEnd: true);
            Emit("COMMENT", CreateSlice(content), start);
            return true;
         }

         while(_i < _src.Length - 1)
         {
            if(_src[_i] == (byte)'*' && _src[_i + 1] == (byte)'/')
            {
               ReadOnlySpan<byte> content = _src[contentStart.._i];
               _i += 2;
               Emit("COMMENT", CreateSlice(content), start);
               return true;
            }

            if(_src[_i] == (byte)'\n')
               _lineStart = true;
            _i++;
         }

         if(_opt.Mode == AjisStreamWalkMode.Lax)
         {
            ReadOnlySpan<byte> content = _src[contentStart..];
            _i = _src.Length;
            Emit("COMMENT", CreateSlice(content), start);
            return true;
         }

         Fail("unterminated_comment", start);
         return false;
      }

      private bool TryParseDirective()
      {
         int start = _i;
         _i++;
         int contentStart = _i;

         while(_i < _src.Length)
         {
            byte b = _src[_i];
            if(b == (byte)'\n' || b == (byte)'\r')
               break;
            _i++;
         }

         ReadOnlySpan<byte> content = TrimWhitespace(_src[contentStart.._i], trimStart: true, trimEnd: true);
         Emit("DIRECTIVE", CreateSlice(content), start);
         return true;
      }

      public void ParseValue(int depth)
      {
         if(_failed) return;

         if(depth > _opt.MaxDepth)
         {
            Fail("max_depth_exceeded", Offset);
            return;
         }
         SkipWs();
         if(AtEnd)
         {
            if(_opt.Mode == AjisStreamWalkMode.Lax)
            {
               Emit("END_OBJECT", AjisSliceUtf8.Empty, Offset);
               return;
            }

            Fail("unexpected_eof", Offset);
            return;
         }

         byte b = _src[_i];

         if(b == (byte)'{') { ParseObject(depth); return; }
         if(b == (byte)'[') { ParseArray(depth); return; }
         if(b == (byte)'\"') { ParseStringValue(); return; }

         if(b == (byte)'t') { ParseLiteral("true", "TRUE"); return; }
         if(b == (byte)'f') { ParseLiteral("false", "FALSE"); return; }
         if(b == (byte)'n') { ParseLiteral("null", "NULL"); return; }

         if(_opt.Mode == AjisStreamWalkMode.Lax && b is (byte)'N' or (byte)'I')
         {
            if(ParseSpecialNumber())
               return;
         }

         if((_opt.Mode == AjisStreamWalkMode.Lax || _opt.Mode == AjisStreamWalkMode.Ajis) && IsTypedLiteralStart())
         {
            ParseTypedLiteral();
            return;
         }

         if((_opt.Mode == AjisStreamWalkMode.Lax || _opt.Mode == AjisStreamWalkMode.Ajis) && _opt.Identifiers && IsIdentifierStart(b))
         {
            ParseIdentifier();
            return;
         }

         if(b == (byte)'-' || (b >= (byte)'0' && b <= (byte)'9') || (_opt.Mode == AjisStreamWalkMode.Lax && b == (byte)'+'))
         {
            ParseNumber();
            return;
         }

         Fail("invalid_value", Offset);
      }

      private static AjisSliceUtf8 CreateSlice(ReadOnlySpan<byte> bytes)
      {
         if(bytes.IsEmpty)
            return AjisSliceUtf8.Empty;

         AjisSliceFlags flags = AjisSliceFlags.None;
         foreach(byte b in bytes)
         {
            if(b == (byte)'\\')
               flags |= AjisSliceFlags.HasEscapes;
            if(b > 0x7F)
               flags |= AjisSliceFlags.HasNonAscii;
         }

         return new AjisSliceUtf8(bytes.ToArray(), flags);
      }

      private static ReadOnlySpan<byte> TrimWhitespace(ReadOnlySpan<byte> span, bool trimStart, bool trimEnd)
      {
         int start = 0;
         int end = span.Length;

         if(trimStart)
         {
            while(start < end && IsWhitespace(span[start]))
               start++;
         }

         if(trimEnd)
         {
            while(end > start && IsWhitespace(span[end - 1]))
               end--;
         }

         return span[start..end];
      }

      private static bool IsWhitespace(byte value)
         => value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

      private void ParseObject(int depth)
      {
         int start = _i;
         _i++; // '{'
         Emit("BEGIN_OBJECT", AjisSliceUtf8.Empty, start);

         SkipWs();
         if(AtEnd)
         {
            if(_opt.Mode == AjisStreamWalkMode.Lax)
            {
               Emit("END_OBJECT", AjisSliceUtf8.Empty, Offset);
               return;
            }

            Fail("unexpected_eof", Offset);
            return;
         }

         if(_src[_i] == (byte)'}')
         {
            int endPos = _i;
            _i++;
            Emit("END_OBJECT", AjisSliceUtf8.Empty, endPos);
            return;
         }

         while(true)
         {
            SkipWs();
            if(AtEnd)
            {
               if(_opt.Mode == AjisStreamWalkMode.Lax)
               {
                  Emit("END_OBJECT", AjisSliceUtf8.Empty, Offset);
                  return;
               }

               Fail("unexpected_eof", Offset);
               return;
            }

            AjisSliceUtf8 name;
            int nameOffset = _i;
            if(_src[_i] == (byte)'\"')
            {
               name = ParseQuotedSliceToMemory(nameOffset);
            }
            else if((_opt.Mode == AjisStreamWalkMode.Lax || _opt.Mode == AjisStreamWalkMode.Ajis) && _opt.Identifiers && IsIdentifierStart(_src[_i]))
            {
               name = ParseIdentifierSlice();
            }
            else
            {
               Fail("expected_name", Offset);
               return;
            }
            if(_failed) return;
            Emit("NAME", name, nameOffset);

            SkipWs();
            if(AtEnd)
            {
               if(_opt.Mode == AjisStreamWalkMode.Lax)
               {
                  Emit("END_OBJECT", AjisSliceUtf8.Empty, Offset);
                  return;
               }

               Fail("unexpected_eof", Offset);
               return;
            }

            if(_src[_i] != (byte)':') { Fail("expected_colon", Offset); return; }
            _i++; // ':'

            ParseValue(depth + 1);
            if(_failed) return;

            SkipWs();
            if(AtEnd)
            {
               if(_opt.Mode == AjisStreamWalkMode.Lax)
               {
                  Emit("END_OBJECT", AjisSliceUtf8.Empty, Offset);
                  return;
               }

               Fail("unexpected_eof", Offset);
               return;
            }

            byte b = _src[_i];
            if(b == (byte)',')
            {
               _i++;
               SkipWs();
               if(AtEnd)
               {
                  if(_opt.Mode == AjisStreamWalkMode.Lax)
                  {
                     Emit("END_OBJECT", AjisSliceUtf8.Empty, _i);
                     return;
                  }

                  Fail("unexpected_eof", Offset);
                  return;
               }

               if(_src[_i] == (byte)'}')
               {
                  if(_opt.Mode == AjisStreamWalkMode.Lax)
                  {
                     int endPos = _i;
                     _i++;
                     Emit("END_OBJECT", AjisSliceUtf8.Empty, endPos);
                     return;
                  }

                  Fail("trailing_comma", Offset);
                  return;
               }

               continue;
            }
            if(b == (byte)'}')
            {
               int endPos = _i;
               _i++;
               Emit("END_OBJECT", AjisSliceUtf8.Empty, endPos);
               return;
            }

            Fail("expected_comma_or_end_object", Offset);
            return;
         }
      }

      private void ParseArray(int depth)
      {
         int start = _i;
         _i++; // '['
         Emit("BEGIN_ARRAY", AjisSliceUtf8.Empty, start);

         SkipWs();
         if(AtEnd)
         {
            if(_opt.Mode == AjisStreamWalkMode.Lax)
            {
               Emit("END_ARRAY", AjisSliceUtf8.Empty, Offset);
               return;
            }

            Fail("unexpected_eof", Offset);
            return;
         }

         if(_src[_i] == (byte)']')
         {
            int endPos = _i;
            _i++;
            Emit("END_ARRAY", AjisSliceUtf8.Empty, endPos);
            return;
         }

         while(true)
         {
            ParseValue(depth + 1);
            if(_failed) return;

            SkipWs();
            if(AtEnd)
            {
               if(_opt.Mode == AjisStreamWalkMode.Lax)
               {
                  Emit("END_ARRAY", AjisSliceUtf8.Empty, Offset);
                  return;
               }

               Fail("unexpected_eof", Offset);
               return;
            }

            byte b = _src[_i];
            if(b == (byte)',')
            {
               _i++;
               SkipWs();
               if(AtEnd)
               {
                  if(_opt.Mode == AjisStreamWalkMode.Lax)
                  {
                     Emit("END_ARRAY", AjisSliceUtf8.Empty, _i);
                     return;
                  }

                  Fail("unexpected_eof", Offset);
                  return;
               }

               if(_src[_i] == (byte)']')
               {
                  if(_opt.Mode == AjisStreamWalkMode.Lax)
                  {
                     int endPos = _i;
                     _i++;
                     Emit("END_ARRAY", AjisSliceUtf8.Empty, endPos);
                     return;
                  }

                  Fail("trailing_comma", Offset);
                  return;
               }

               continue;
            }
            if(b == (byte)']')
            {
               int endPos = _i;
               _i++;
               Emit("END_ARRAY", AjisSliceUtf8.Empty, endPos);
               return;
            }

            Fail("expected_comma_or_end_array", Offset);
            return;
         }
      }

      private void ParseStringValue()
      {
         int off = _i;
         AjisSliceUtf8 mem = ParseQuotedSliceToMemory(off);
         if(_failed) return;
         Emit("STRING", mem, off);
      }

      private static bool IsHex(byte b) =>
         (b >= (byte)'0' && b <= (byte)'9') ||
         (b >= (byte)'a' && b <= (byte)'f') ||
         (b >= (byte)'A' && b <= (byte)'F');

      /// <summary>
      /// Parses a JSON string token starting at current index (must be '"').
      /// Returns bytes inside quotes as a new byte[].
      /// </summary>
      private AjisSliceUtf8 ParseQuotedSliceToMemory(int tokenOffset)
      {
         // opening quote
         _i++;
         int contentStart = _i;
         AjisSliceFlags flags = AjisSliceFlags.None;

         while(_i < _src.Length)
         {
            byte b = _src[_i];
            if(b == (byte)'\"')
            {
               int contentEnd = _i;
               _i++; // closing quote

               int len = contentEnd - contentStart;
               if(len > _opt.MaxTokenBytes)
               {
                  Fail("max_token_bytes_exceeded", tokenOffset);
                  return AjisSliceUtf8.Empty;
               }

               return new AjisSliceUtf8(_src.Slice(contentStart, len).ToArray(), flags);
            }

            if(b == (byte)'\\')
            {
               flags |= AjisSliceFlags.HasEscapes;
               // Validate escape sequences (M1: JSON-safe, strict).
               int escOffset = _i; // point at '\\'

               _i++; // move to escape code
               if(_i >= _src.Length)
               {
                  Fail("unterminated_string", tokenOffset);
                  return AjisSliceUtf8.Empty;
               }

               byte e = _src[_i];
               switch(e)
               {
                  case (byte)'\"':
                  case (byte)'\\':
                  case (byte)'/':
                  case (byte)'b':
                  case (byte)'f':
                  case (byte)'n':
                  case (byte)'r':
                  case (byte)'t':
                     _i++; // consume escape code
                     break;

                  case (byte)'u':
                     // Need 4 hex digits after \u
                     if(_i + 4 >= _src.Length)
                     {
                        if(_opt.Mode == AjisStreamWalkMode.Lax)
                        {
                           _i = _src.Length;
                           break;
                        }

                        Fail("unterminated_string", tokenOffset);
                        return AjisSliceUtf8.Empty;
                     }

                     if(!IsHex(_src[_i + 1]) ||
                        !IsHex(_src[_i + 2]) ||
                        !IsHex(_src[_i + 3]) ||
                        !IsHex(_src[_i + 4]))
                     {
                        if(_opt.Mode == AjisStreamWalkMode.Lax)
                        {
                           _i++;
                           break;
                        }

                        Fail("invalid_escape", escOffset + 1);
                        return AjisSliceUtf8.Empty;
                     }

                     _i += 5; // 'u' + 4 hex digits
                     break;

                  default:
                     if(_opt.Mode == AjisStreamWalkMode.Lax)
                     {
                        _i++;
                        break;
                     }

                     Fail("invalid_escape", escOffset + 1);
                     return AjisSliceUtf8.Empty;
               }

               continue;
            }

            if(b >= 0x80)
               flags |= AjisSliceFlags.HasNonAscii;

            // Disallow raw control chars in JSON strings
            if(b < 0x20)
            {
               if(_opt.Mode == AjisStreamWalkMode.Lax)
               {
                  _i++;
                  continue;
               }

               Fail("invalid_string_control_char", _i);
               return AjisSliceUtf8.Empty;
            }

            _i++;
         }

         if(_opt.Mode == AjisStreamWalkMode.Lax)
         {
            int len = _src.Length - contentStart;
            if(len > _opt.MaxTokenBytes)
            {
               Fail("max_token_bytes_exceeded", tokenOffset);
               return AjisSliceUtf8.Empty;
            }

            return new AjisSliceUtf8(_src.Slice(contentStart, len).ToArray(), flags);
         }

         Fail("unterminated_string", tokenOffset);
         return AjisSliceUtf8.Empty;
      }

      private void ParseNumber()
      {
         int start = _i;

         if(_src[_i] == (byte)'-' || (_opt.Mode == AjisStreamWalkMode.Lax && _src[_i] == (byte)'+')) _i++;
         if(_i >= _src.Length) { Fail("invalid_number", start); return; }

         if(_src[_i] == (byte)'0')
         {
            _i++;
         }
         else
         {
            if(_src[_i] < (byte)'1' || _src[_i] > (byte)'9') { Fail("invalid_number", start); return; }
            while(_i < _src.Length && _src[_i] >= (byte)'0' && _src[_i] <= (byte)'9') _i++;
         }

         if(_i < _src.Length && _src[_i] == (byte)'.')
         {
            _i++;
            if(_i >= _src.Length || _src[_i] < (byte)'0' || _src[_i] > (byte)'9') { Fail("invalid_number", start); return; }
            while(_i < _src.Length && _src[_i] >= (byte)'0' && _src[_i] <= (byte)'9') _i++;
         }

         if(_i < _src.Length && (_src[_i] == (byte)'e' || _src[_i] == (byte)'E'))
         {
            _i++;
            if(_i < _src.Length && (_src[_i] == (byte)'+' || _src[_i] == (byte)'-')) _i++;
            if(_i >= _src.Length || _src[_i] < (byte)'0' || _src[_i] > (byte)'9') { Fail("invalid_number", start); return; }
            while(_i < _src.Length && _src[_i] >= (byte)'0' && _src[_i] <= (byte)'9') _i++;
         }

         int len = _i - start;
         if(len > _opt.MaxTokenBytes) { Fail("max_token_bytes_exceeded", start); return; }

         AjisSliceUtf8 mem = new(_src.Slice(start, len).ToArray(), AjisSliceFlags.None);
         Emit("NUMBER", mem, start);
      }

      private bool ParseSpecialNumber()
      {
         int start = _i;
         if("Infinity".Length > _opt.MaxTokenBytes)
         {
            Fail("max_token_bytes_exceeded", start);
            return true;
         }

         if(TryConsumeAscii("NaN"))
         {
            Emit("NUMBER", new AjisSliceUtf8("NaN"u8.ToArray(), AjisSliceFlags.None), start);
            return true;
         }

         if(TryConsumeAscii("Infinity"))
         {
            Emit("NUMBER", new AjisSliceUtf8("Infinity"u8.ToArray(), AjisSliceFlags.None), start);
            return true;
         }

         _i = start;
         return false;
      }

      private void ParseTypedLiteral()
      {
         int start = _i;
         while(_i < _src.Length && IsIdentifierPart(_src[_i]))
            _i++;

         int len = _i - start;
         if(len <= 0)
         {
            Fail("invalid_value", start);
            return;
         }

         if(len > _opt.MaxTokenBytes)
         {
            Fail("max_token_bytes_exceeded", start);
            return;
         }

         var slice = new AjisSliceUtf8(_src.Slice(start, len).ToArray(), AjisSliceFlags.None);
         Emit("NUMBER", slice, start);
      }

      private void ParseLiteral(string ascii, string kind)
      {
         int start = _i;
         if(ascii.Length > _opt.MaxTokenBytes)
         {
            Fail("max_token_bytes_exceeded", start);
            return;
         }
         if(!TryConsumeAscii(ascii))
         {
            Fail("invalid_literal", start);
            return;
         }

         Emit(kind, AjisSliceUtf8.Empty, start);
      }

      private void ParseIdentifier()
      {
         int start = _i;
         while(_i < _src.Length && IsIdentifierPart(_src[_i]))
            _i++;

         int len = _i - start;
         if(len <= 0)
         {
            Fail("invalid_value", start);
            return;
         }

         if(len > _opt.MaxTokenBytes)
         {
            Fail("max_token_bytes_exceeded", start);
            return;
         }

         var slice = new AjisSliceUtf8(_src.Slice(start, len).ToArray(), AjisSliceFlags.None);
         Emit("IDENTIFIER", slice, start);
      }

      private AjisSliceUtf8 ParseIdentifierSlice()
      {
         int start = _i;
         while(_i < _src.Length && IsIdentifierPart(_src[_i]))
            _i++;

         int len = _i - start;
         if(len <= 0)
         {
            Fail("invalid_value", start);
            return AjisSliceUtf8.Empty;
         }

         if(len > _opt.MaxTokenBytes)
         {
            Fail("max_token_bytes_exceeded", start);
            return AjisSliceUtf8.Empty;
         }

         return new AjisSliceUtf8(_src.Slice(start, len).ToArray(), AjisSliceFlags.None);
      }

      private static bool IsIdentifierStart(byte b)
         => (b >= (byte)'A' && b <= (byte)'Z')
            || (b >= (byte)'a' && b <= (byte)'z')
            || b == (byte)'$';

      private static bool IsIdentifierPart(byte b)
         => IsIdentifierStart(b) || (b >= (byte)'0' && b <= (byte)'9');

      private readonly bool IsTypedLiteralStart()
      {
         if(_i + 1 >= _src.Length)
            return false;

         byte current = _src[_i];
         byte next = _src[_i + 1];
         return current is >= (byte)'A' and <= (byte)'Z'
            && next is >= (byte)'0' and <= (byte)'9';
      }

      private bool TryConsumeAscii(string ascii)
      {
         if(_i + ascii.Length > _src.Length) return false;
         for(int j = 0; j < ascii.Length; j++)
            if(_src[_i + j] != (byte)ascii[j]) return false;
         _i += ascii.Length;
         return true;
      }

      public void Emit(string kind, AjisSliceUtf8 slice, long offset)
      {
         if(_failed) return;

         bool cont = _v.OnEvent(new AjisStreamWalkEvent(kind, slice, offset));
         if(!cont && _runnerOpt.AllowVisitorAbort)
         {
            Fail("visitor_abort", offset);
         }
      }

      public void Fail(string code, long offset)
      {
         if(_failed) return;
         _failed = true;

         // M1 does not compute line/column yet.
         _v.OnError(new AjisStreamWalkError(code, offset, Line: null, Column: null));
      }
   }
}
