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

      st.Emit("END_DOCUMENT", ReadOnlyMemory<byte>.Empty, st.Offset);
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
      }

      public readonly bool Failed => _failed;
      public readonly long Offset => _i;
      public readonly bool AtEnd => _i >= _src.Length;

      public void SkipWs()
      {
         while(_i < _src.Length)
         {
            byte b = _src[_i];
            if(b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n') { _i++; continue; }
            break;
         }
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
         if(AtEnd) { Fail("unexpected_eof", Offset); return; }

         byte b = _src[_i];

         if(b == (byte)'{') { ParseObject(depth); return; }
         if(b == (byte)'[') { ParseArray(depth); return; }
         if(b == (byte)'\"') { ParseStringValue(); return; }

         if(b == (byte)'t') { ParseLiteral("true", "TRUE"); return; }
         if(b == (byte)'f') { ParseLiteral("false", "FALSE"); return; }
         if(b == (byte)'n') { ParseLiteral("null", "NULL"); return; }

         if(b == (byte)'-' || (b >= (byte)'0' && b <= (byte)'9'))
         {
            ParseNumber();
            return;
         }

         Fail("invalid_value", Offset);
      }

      private void ParseObject(int depth)
      {
         int start = _i;
         _i++; // '{'
         Emit("BEGIN_OBJECT", ReadOnlyMemory<byte>.Empty, start);

         SkipWs();
         if(AtEnd) { Fail("unexpected_eof", Offset); return; }

         if(_src[_i] == (byte)'}')
         {
            int endPos = _i;
            _i++;
            Emit("END_OBJECT", ReadOnlyMemory<byte>.Empty, endPos);
            return;
         }

         while(true)
         {
            SkipWs();
            if(AtEnd) { Fail("unexpected_eof", Offset); return; }

            if(_src[_i] != (byte)'\"')
            {
               Fail("expected_name", Offset);
               return;
            }

            int nameOffset = _i;
            ReadOnlyMemory<byte> name = ParseQuotedSliceToMemory(nameOffset);
            if(_failed) return;
            Emit("NAME", name, nameOffset);

            SkipWs();
            if(AtEnd) { Fail("unexpected_eof", Offset); return; }

            if(_src[_i] != (byte)':') { Fail("expected_colon", Offset); return; }
            _i++; // ':'

            ParseValue(depth + 1);
            if(_failed) return;

            SkipWs();
            if(AtEnd) { Fail("unexpected_eof", Offset); return; }

            byte b = _src[_i];
            if(b == (byte)',') { _i++; continue; }
            if(b == (byte)'}')
            {
               int endPos = _i;
               _i++;
               Emit("END_OBJECT", ReadOnlyMemory<byte>.Empty, endPos);
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
         Emit("BEGIN_ARRAY", ReadOnlyMemory<byte>.Empty, start);

         SkipWs();
         if(AtEnd) { Fail("unexpected_eof", Offset); return; }

         if(_src[_i] == (byte)']')
         {
            int endPos = _i;
            _i++;
            Emit("END_ARRAY", ReadOnlyMemory<byte>.Empty, endPos);
            return;
         }

         while(true)
         {
            ParseValue(depth + 1);
            if(_failed) return;

            SkipWs();
            if(AtEnd) { Fail("unexpected_eof", Offset); return; }

            byte b = _src[_i];
            if(b == (byte)',') { _i++; continue; }
            if(b == (byte)']')
            {
               int endPos = _i;
               _i++;
               Emit("END_ARRAY", ReadOnlyMemory<byte>.Empty, endPos);
               return;
            }

            Fail("expected_comma_or_end_array", Offset);
            return;
         }
      }

      private void ParseStringValue()
      {
         int off = _i;
         ReadOnlyMemory<byte> mem = ParseQuotedSliceToMemory(off);
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
      private ReadOnlyMemory<byte> ParseQuotedSliceToMemory(int tokenOffset)
      {
         // opening quote
         _i++;
         int contentStart = _i;

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
                  return ReadOnlyMemory<byte>.Empty;
               }

               return new ReadOnlyMemory<byte>(_src.Slice(contentStart, len).ToArray());
            }

            if(b == (byte)'\\')
            {
               // Validate escape sequences (M1: JSON-safe, strict).
               int escOffset = _i; // point at '\\'

               _i++; // move to escape code
               if(_i >= _src.Length)
               {
                  Fail("unterminated_string", tokenOffset);
                  return ReadOnlyMemory<byte>.Empty;
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
                        Fail("unterminated_string", tokenOffset);
                        return ReadOnlyMemory<byte>.Empty;
                     }

                     if(!IsHex(_src[_i + 1]) ||
                        !IsHex(_src[_i + 2]) ||
                        !IsHex(_src[_i + 3]) ||
                        !IsHex(_src[_i + 4]))
                     {
                        Fail("invalid_escape", escOffset + 1);
                        return ReadOnlyMemory<byte>.Empty;
                     }

                     _i += 5; // 'u' + 4 hex digits
                     break;

                  default:
                     Fail("invalid_escape", escOffset + 1);
                     return ReadOnlyMemory<byte>.Empty;
               }

               continue;
            }

            // Disallow raw control chars in JSON strings
            if(b < 0x20)
            {
               Fail("invalid_string_control_char", _i);
               return ReadOnlyMemory<byte>.Empty;
            }

            _i++;
         }

         Fail("unterminated_string", tokenOffset);
         return ReadOnlyMemory<byte>.Empty;
      }

      private void ParseNumber()
      {
         int start = _i;

         if(_src[_i] == (byte)'-') _i++;
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

         ReadOnlyMemory<byte> mem = new(_src.Slice(start, len).ToArray());
         Emit("NUMBER", mem, start);
      }

      private void ParseLiteral(string ascii, string kind)
      {
         int start = _i;
         if(!TryConsumeAscii(ascii))
         {
            Fail("invalid_literal", start);
            return;
         }

         Emit(kind, ReadOnlyMemory<byte>.Empty, start);
      }

      private bool TryConsumeAscii(string ascii)
      {
         if(_i + ascii.Length > _src.Length) return false;
         for(int j = 0; j < ascii.Length; j++)
            if(_src[_i + j] != (byte)ascii[j]) return false;
         _i += ascii.Length;
         return true;
      }

      public void Emit(string kind, ReadOnlyMemory<byte> slice, long offset)
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
