using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Afrowave.AJIS;

/// <summary>
/// Represents a value in an AJIS/JSON document.
/// This is the core DOM type that can be null, boolean, number, string, object, or array.
/// OPTIMIZED: Eliminates boxing for primitive types (bool, number) to reduce allocations.
/// </summary>
public sealed class AjisValue
{
    // Inline storage - no boxing for primitive types!
    private readonly double _numberValue;
    private readonly bool _boolValue;
    private string? _stringValue;
    private readonly Dictionary<string, AjisValue>? _objectValue;
    private readonly List<AjisValue>? _arrayValue;
    private readonly byte[]? _utf8StringBuffer;
    private readonly int _utf8StringOffset;
    private readonly int _utf8StringLength;

    /// <summary>
    /// Gets the type of this value.
    /// </summary>
    public AjisValueType Type { get; }

    // ===== Constructors =====

    private AjisValue(
        AjisValueType type,
        double numberValue = 0,
        bool boolValue = false,
        string? stringValue = null,
        Dictionary<string, AjisValue>? objectValue = null,
        List<AjisValue>? arrayValue = null,
        byte[]? utf8StringBuffer = null,
        int utf8StringOffset = 0,
        int utf8StringLength = 0)
    {
        Type = type;
        _numberValue = numberValue;
        _boolValue = boolValue;
        _stringValue = stringValue;
        _objectValue = objectValue;
        _arrayValue = arrayValue;
        _utf8StringBuffer = utf8StringBuffer;
        _utf8StringOffset = utf8StringOffset;
        _utf8StringLength = utf8StringLength;
    }

    /// <summary>
    /// Creates a null value.
    /// </summary>
    public static AjisValue Null() => new(AjisValueType.Null);

    /// <summary>
    /// Creates a boolean value.
    /// </summary>
    public static AjisValue Boolean(bool value) => new(AjisValueType.Boolean, boolValue: value);

    /// <summary>
    /// Creates a number value from a double.
    /// </summary>
    public static AjisValue Number(double value) => new(AjisValueType.Number, numberValue: value);

    /// <summary>
    /// Creates a number value from a long.
    /// </summary>
    public static AjisValue Number(long value) => new(AjisValueType.Number, numberValue: value);

    /// <summary>
    /// Creates a string value.
    /// </summary>
    public static AjisValue String(string value) => new(AjisValueType.String, stringValue: value ?? throw new ArgumentNullException(nameof(value)));

    internal static AjisValue Utf8String(byte[] buffer, int offset, int length)
    {
        ArgumentNullException.ThrowIfNull(buffer);


        if (offset < 0 || length < 0 || offset + length > buffer.Length)
        {

            throw new ArgumentOutOfRangeException(nameof(offset));
        }


        return new AjisValue(
            AjisValueType.String,
            stringValue: null,
            utf8StringBuffer: buffer,
            utf8StringOffset: offset,
            utf8StringLength: length);
    }

    /// <summary>
    /// Creates an object value (empty).
    /// </summary>
    public static AjisValue Object() => new(AjisValueType.Object, objectValue: new Dictionary<string, AjisValue>());

    /// <summary>
    /// Creates an object value from a dictionary.
    /// </summary>
    public static AjisValue Object(Dictionary<string, AjisValue> properties) => new(AjisValueType.Object, objectValue: properties ?? throw new ArgumentNullException(nameof(properties)));

    /// <summary>
    /// Creates an array value (empty).
    /// </summary>
    public static AjisValue Array() => new(AjisValueType.Array, arrayValue: new List<AjisValue>());

    /// <summary>
    /// Creates an array value from a list.
    /// </summary>
    public static AjisValue Array(List<AjisValue> items) => new(AjisValueType.Array, arrayValue: items ?? throw new ArgumentNullException(nameof(items)));

    // ===== Type checks =====

    /// <summary>
    /// Returns true if this is a null value.
    /// </summary>
    public bool IsNull => Type == AjisValueType.Null;

    /// <summary>
    /// Returns true if this is a boolean value.
    /// </summary>
    public bool IsBoolean => Type == AjisValueType.Boolean;

    /// <summary>
    /// Returns true if this is a number value.
    /// </summary>
    public bool IsNumber => Type == AjisValueType.Number;

    /// <summary>
    /// Returns true if this is a string value.
    /// </summary>
    public bool IsString => Type == AjisValueType.String;

    /// <summary>
    /// Returns true if this is an object value.
    /// </summary>
    public bool IsObject => Type == AjisValueType.Object;

    /// <summary>
    /// Returns true if this is an array value.
    /// </summary>
    public bool IsArray => Type == AjisValueType.Array;

    // ===== Value accessors =====

    /// <summary>
    /// Gets this value as a boolean.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not a boolean value.</exception>
    public bool AsBoolean()
    {
        if (Type != AjisValueType.Boolean)
        {

            throw new InvalidOperationException($"Cannot convert {Type} to Boolean");
        }


        return _boolValue;
    }

    /// <summary>
    /// Gets this value as a double.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not a number value.</exception>
    public double AsNumber()
    {
        if (Type != AjisValueType.Number)
        {

            throw new InvalidOperationException($"Cannot convert {Type} to Number");
        }


        return _numberValue;
    }

    /// <summary>
    /// Gets this value as an integer.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not a number value.</exception>
    public int AsInt32() => (int)AsNumber();

    /// <summary>
    /// Gets this value as a long.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not a number value.</exception>
    public long AsInt64() => (long)AsNumber();

    /// <summary>
    /// Gets this value as a string.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not a string value.</exception>
    public string AsString()
    {
        if (Type != AjisValueType.String)
        {

            throw new InvalidOperationException($"Cannot convert {Type} to String");
        }


        if (_stringValue != null)
        {
            return _stringValue;
        }

        if (_utf8StringBuffer != null)
        {
            _stringValue = System.Text.Encoding.UTF8.GetString(_utf8StringBuffer, _utf8StringOffset, _utf8StringLength);
            return _stringValue;
        }

        return _stringValue!;
    }

    internal bool TryGetUtf8String(out ReadOnlySpan<byte> utf8)
    {
        if (_utf8StringBuffer != null)
        {
            utf8 = new ReadOnlySpan<byte>(_utf8StringBuffer, _utf8StringOffset, _utf8StringLength);
            return true;
        }

        utf8 = default;
        return false;
    }

    /// <summary>
    /// Gets this value as an object (dictionary).
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not an object value.</exception>
    public Dictionary<string, AjisValue> AsObject()
    {
        if (Type != AjisValueType.Object)
        {

            throw new InvalidOperationException($"Cannot convert {Type} to Object");
        }


        return _objectValue!;
    }

    /// <summary>
    /// Gets this value as an array (list).
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not an array value.</exception>
    public List<AjisValue> AsArray()
    {
        if (Type != AjisValueType.Array)
        {

            throw new InvalidOperationException($"Cannot convert {Type} to Array");
        }


        return _arrayValue!;
    }

    // ===== Indexers for easy access =====

    /// <summary>
    /// Gets a property value from this object by key.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not an object.</exception>
    /// <exception cref="KeyNotFoundException">If the key does not exist.</exception>
    public AjisValue this[string key]
    {
        get
        {
            var obj = AsObject();
            if (!obj.TryGetValue(key, out var value))
            {

                throw new KeyNotFoundException($"Key '{key}' not found in object");
            }


            return value;
        }
    }

    /// <summary>
    /// Gets an element from this array by index.
    /// </summary>
    /// <exception cref="InvalidOperationException">If this is not an array.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the index is out of range.</exception>
    public AjisValue this[int index]
    {
        get
        {
            var arr = AsArray();
            return arr[index];
        }
    }

    /// <summary>
    /// Tries to get a property value from this object.
    /// </summary>
    public bool TryGetProperty(string key, [NotNullWhen(true)] out AjisValue? value)
    {
        if (Type != AjisValueType.Object)
        {
            value = null;
            return false;
        }
        return AsObject().TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets the number of properties (for objects) or elements (for arrays).
    /// </summary>
    public int Count => Type switch
    {
        AjisValueType.Object => AsObject().Count,
        AjisValueType.Array => AsArray().Count,
        _ => 0
    };

    public override string ToString() => Type switch
    {
        AjisValueType.Null => "null",
        AjisValueType.Boolean => AsBoolean().ToString().ToLowerInvariant(),
        AjisValueType.Number => AsNumber().ToString(CultureInfo.InvariantCulture),
        AjisValueType.String => $"\"{AsString()}\"",
        AjisValueType.Object => $"{{ {AsObject().Count} properties }}",
        AjisValueType.Array => $"[ {AsArray().Count} items ]",
        _ => "unknown"
    };
}
