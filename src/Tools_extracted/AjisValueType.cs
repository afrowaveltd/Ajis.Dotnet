using System;

namespace Afrowave.AJIS;

/// <summary>
/// Represents the type of an AJIS value.
/// </summary>
public enum AjisValueType : byte
{
    /// <summary>Null value</summary>
    Null = 0,

    /// <summary>Boolean value (true/false)</summary>
    Boolean,

    /// <summary>Number value (integer or float)</summary>
    Number,

    /// <summary>String value</summary>
    String,

    /// <summary>Object (dictionary of key-value pairs)</summary>
    Object,

    /// <summary>Array (ordered list of values)</summary>
    Array,
}
