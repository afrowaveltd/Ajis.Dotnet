using System;
using System.Text;

namespace Afrowave.AJIS.Legacy;

/// <summary>
/// Serializes AjisValue objects to AJIS/JSON strings.
/// OPTIMIZED: Now uses Utf8JsonWriter for better performance.
/// </summary>
public sealed class AjisSerializer
{
    private readonly AjisSerializerOptions _options;

    public AjisSerializer(AjisSerializerOptions? options = null)
    {
        _options = options ?? AjisSerializerOptions.Ajis;
    }

    /// <summary>
    /// Serializes an AjisValue to a string.
    /// For maximum performance, consider using AjisUtf8Serializer.SerializeToUtf8Bytes() instead.
    /// </summary>
    public static string Serialize(AjisValue value, AjisSerializerOptions? options = null)
    {
        // Delegate to the optimized Utf8 serializer
        return AjisUtf8Serializer.Serialize(value, options);
    }

    /// <summary>
    /// Serializes an AjisValue to a string.
    /// </summary>
    public string SerializeValue(AjisValue value)
    {
        return AjisUtf8Serializer.Serialize(value, _options);
    }

    // Legacy StringBuilder-based implementation kept for reference/fallback
    // Can be removed in future versions
    #region Legacy StringBuilder Implementation

    private readonly StringBuilder _builder = new StringBuilder(256);
    private int _indentLevel;

    private void WriteValue(AjisValue value)
    {
        switch (value.Type)
        {
            case AjisValueType.Null:
                _builder.Append("null");
                break;

            case AjisValueType.Boolean:
                _builder.Append(value.AsBoolean() ? "true" : "false");
                break;

            case AjisValueType.Number:
                WriteNumber(value.AsNumber());
                break;

            case AjisValueType.String:
                WriteString(value.AsString());
                break;

            case AjisValueType.Array:
                WriteArray(value);
                break;

            case AjisValueType.Object:
                WriteObject(value);
                break;

            default:
                throw new InvalidOperationException($"Unknown value type: {value.Type}");
        }
    }

    private void WriteNumber(double number)
    {
        // Check if it's an integer
        if (number % 1 == 0 && number >= long.MinValue && number <= long.MaxValue)
        {
            long intValue = (long)number;

            // Use AJIS extensions if enabled and number is suitable
            if (_options.UseAjisExtensions)
            {
                // Use hex for certain values (multiples of 16, or commonly used hex values)
                if (intValue >= 0 && intValue % 16 == 0 && intValue >= 256)
                {
                    _builder.Append("0x").Append(intValue.ToString("X"));
                    return;
                }
            }

            _builder.Append(intValue);
        }
        else
        {
            // Get the numeric value (works for both int and double)
            double value = number;
            if (value % 1 == 0 && value >= long.MinValue && value <= long.MaxValue)
            {
                _builder.Append((long)value);
            }
            else
            {
                _builder.Append(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    private void WriteString(string value)
    {
        _builder.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    _builder.Append("\\\"");
                    break;
                case '\\':
                    _builder.Append("\\\\");
                    break;
                case '\n':
                    _builder.Append("\\n");
                    break;
                case '\r':
                    _builder.Append("\\r");
                    break;
                case '\t':
                    _builder.Append("\\t");
                    break;
                case '\b':
                    _builder.Append("\\b");
                    break;
                case '\f':
                    _builder.Append("\\f");
                    break;
                default:
                    if (_options.EscapeNonAscii && c > 127)
                    {
                        _builder.Append("\\u").Append(((int)c).ToString("X4"));
                    }
                    else if (char.IsControl(c))
                    {
                        _builder.Append("\\u").Append(((int)c).ToString("X4"));
                    }
                    else
                    {
                        _builder.Append(c);
                    }
                    break;
            }
        }

        _builder.Append('"');
    }

    private void WriteArray(AjisValue array)
    {
        _builder.Append('[');

        var items = array.AsArray();
        if (items.Count > 0)
        {
            if (_options.WriteIndented)
            {
                _indentLevel++;
                for (int i = 0; i < items.Count; i++)
                {
                    _builder.AppendLine();
                    WriteIndent();
                    WriteValue(items[i]);

                    if (i < items.Count - 1)
                        _builder.Append(',');
                }
                _indentLevel--;
                _builder.AppendLine();
                WriteIndent();
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    WriteValue(items[i]);
                    if (i < items.Count - 1)
                        _builder.Append(',');
                }
            }
        }

        _builder.Append(']');
    }

    private void WriteObject(AjisValue obj)
    {
        _builder.Append('{');

        var properties = obj.AsObject();
        if (properties.Count > 0)
        {
            if (_options.WriteIndented)
            {
                _indentLevel++;
                bool first = true;
                foreach (var kvp in properties)
                {
                    // Skip null values if configured
                    if (!_options.WriteNullValues && kvp.Value.Type == AjisValueType.Null)
                        continue;

                    if (!first)
                        _builder.Append(',');
                    first = false;

                    _builder.AppendLine();
                    WriteIndent();

                    // Apply naming policy to key
                    string key = kvp.Key;
                    if (_options.PropertyNamingPolicy != null)
                    {
                        key = _options.PropertyNamingPolicy.ConvertName(key);
                    }

                    WriteString(key);
                    _builder.Append(": ");
                    WriteValue(kvp.Value);
                }
                _indentLevel--;
                _builder.AppendLine();
                WriteIndent();
            }
            else
            {
                bool first = true;
                foreach (var kvp in properties)
                {
                    // Skip null values if configured
                    if (!_options.WriteNullValues && kvp.Value.Type == AjisValueType.Null)
                        continue;

                    if (!first)
                        _builder.Append(',');
                    first = false;

                    // Apply naming policy to key
                    string key = kvp.Key;
                    if (_options.PropertyNamingPolicy != null)
                    {
                        key = _options.PropertyNamingPolicy.ConvertName(key);
                    }

                    WriteString(key);
                    _builder.Append(':');
                    WriteValue(kvp.Value);
                }
            }
        }

        _builder.Append('}');
    }

    private void WriteIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _builder.Append(_options.IndentString);
        }
    }

    #endregion
}
