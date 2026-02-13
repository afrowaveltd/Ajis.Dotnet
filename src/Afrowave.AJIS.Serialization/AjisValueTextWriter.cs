#nullable enable

using System.Text;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Serializes AjisValue instances into AJIS text.
/// </summary>
internal sealed class AjisValueTextWriter(AjisSerializationFormattingOptions options)
{
   private readonly StringBuilder _builder = new();
   private readonly bool _compact = options.Compact;
   private readonly bool _pretty = options.Pretty;
   private readonly int _indentSize = options.IndentSize;
   private readonly bool _canonicalize = options.Canonicalize;

   public string Write(AjisValue value)
   {
      ArgumentNullException.ThrowIfNull(value);
      // Vylepšený serializér: explicitně nastav kompaktní režim
      // Pokud settings nejsou předány, použij Compact=true
      AppendValue(value, depth: 0);
      return _builder.ToString();
   }

   public async Task WriteAsync(Stream output, AjisValue value, CancellationToken ct)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(value);

      AppendValue(value, depth: 0);
      byte[] bytes = Encoding.UTF8.GetBytes(_builder.ToString());
      await output.WriteAsync(bytes, ct).ConfigureAwait(false);
   }

   private void AppendValue(AjisValue value, int depth)
   {
      switch(value)
      {
         case AjisValue.NullValue:
            _builder.Append("null");
            break;
         case AjisValue.BoolValue b:
            _builder.Append(b.Value ? "true" : "false");
            break;
         case AjisValue.NumberValue n:
            _builder.Append(n.Text);
            break;
         case AjisValue.StringValue s:
            AppendQuoted(s.Value);
            break;
         case AjisValue.ArrayValue a:
            AppendArray(a.Items, depth);
            break;
         case AjisValue.ObjectValue o:
            AppendObject(o.Properties, depth);
            break;
         default:
            throw new InvalidOperationException("Unsupported AjisValue type.");
      }
   }

   private void AppendArray(IReadOnlyList<AjisValue> items, int depth)
   {
      _builder.Append('[');
      for(int i = 0; i < items.Count; i++)
      {
         if(_pretty)
         {
            if(i > 0)
            {
               _builder.Append(',');
               _builder.Append(' ');
            }
            AppendNewLineAndIndent(depth + 1);
         }
         else if(i > 0)
         {
            _builder.Append(',');
            // V compact režimu nikdy nepřidávej mezery
            if(!_compact) _builder.Append(' ');
         }
         AppendValue(items[i], depth + 1);
      }
      if(_pretty && items.Count > 0)
         AppendNewLineAndIndent(depth);
      _builder.Append(']');
   }

   private void AppendObject(IReadOnlyList<KeyValuePair<string, AjisValue>> properties, int depth)
   {
      IEnumerable<KeyValuePair<string, AjisValue>> ordered = properties;
      if(_canonicalize)
         ordered = properties.OrderBy(p => p.Key, StringComparer.Ordinal);
      _builder.Append('{');
      int i = 0;
      foreach(var property in ordered)
      {
         if(_pretty)
         {
            if(i > 0)
               _builder.Append(',');
            AppendNewLineAndIndent(depth + 1);
         }
         else if(i > 0)
         {
            _builder.Append(',');
            // V compact režimu nikdy nepřidávej mezery
            if(!_compact) _builder.Append(' ');
         }
         AppendQuoted(property.Key);
         _builder.Append(':');
         // V compact režimu nikdy nepřidávej mezery
         if(!_compact) _builder.Append(' ');
         AppendValue(property.Value, depth + 1);
         i++;
      }
      if(_pretty && i > 0)
         AppendNewLineAndIndent(depth);
      _builder.Append('}');
   }

   private void AppendQuoted(string value)
   {
      _builder.Append('"');
      _builder.Append(AjisTextEscaper.Escape(value));
      _builder.Append('"');
   }

   private void AppendSeparatorSpace()
   {
      // Only insert space if neither compact nor pretty
      if(!_compact && !_pretty)
         _builder.Append(' ');
   }

   private void AppendNameSeparatorSpace()
   {
      // Only insert space if neither compact nor pretty
      if(!_compact && !_pretty)
         _builder.Append(' ');
   }

   private void AppendNewLineAndIndent(int depth)
   {
      _builder.AppendLine();
      _builder.Append(' ', depth * _indentSize);
   }
}
