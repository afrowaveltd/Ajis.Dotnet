#nullable enable

using System.Text;

namespace Afrowave.AJIS.Serialization;

/// <summary>
/// Serializes AjisValue instances into AJIS text.
/// </summary>
internal sealed class AjisValueTextWriter
{
   private readonly StringBuilder _builder = new();
   private readonly bool _compact;

   public AjisValueTextWriter(bool compact)
   {
      _compact = compact;
   }

   public string Write(AjisValue value)
   {
      ArgumentNullException.ThrowIfNull(value);
      AppendValue(value);
      return _builder.ToString();
   }

   public async Task WriteAsync(Stream output, AjisValue value, CancellationToken ct)
   {
      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(value);

      AppendValue(value);
      byte[] bytes = Encoding.UTF8.GetBytes(_builder.ToString());
      await output.WriteAsync(bytes, ct).ConfigureAwait(false);
   }

   private void AppendValue(AjisValue value)
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
            AppendArray(a.Items);
            break;
         case AjisValue.ObjectValue o:
            AppendObject(o.Properties);
            break;
         default:
            throw new InvalidOperationException("Unsupported AjisValue type.");
      }
   }

   private void AppendArray(IReadOnlyList<AjisValue> items)
   {
      _builder.Append('[');
      for(int i = 0; i < items.Count; i++)
      {
         if(i > 0)
         {
            _builder.Append(',');
            AppendSeparatorSpace();
         }

         AppendValue(items[i]);
      }
      _builder.Append(']');
   }

   private void AppendObject(IReadOnlyList<KeyValuePair<string, AjisValue>> properties)
   {
      _builder.Append('{');
      for(int i = 0; i < properties.Count; i++)
      {
         if(i > 0)
         {
            _builder.Append(',');
            AppendSeparatorSpace();
         }

         AppendQuoted(properties[i].Key);
         _builder.Append(':');
         AppendSeparatorSpace();
         AppendValue(properties[i].Value);
      }
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
      if(!_compact)
         _builder.Append(' ');
   }
}
