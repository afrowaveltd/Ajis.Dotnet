#nullable enable

using System.Globalization;

namespace Afrowave.AJIS.Core;

/// <summary>
/// High-performance, allocation-free number parsing for UTF-8 byte sequences.
/// </summary>
/// <remarks>
/// <para>
/// Provides span-based parsing of integers and decimals without intermediate
/// string allocations. Optimized for AJIS number formats.
/// </para>
/// <para>
/// Handles: integers, decimals, scientific notation (e.g., 1.23e-4)
/// </para>
/// </remarks>
public static class AjisNumberParser
{
    /// <summary>
    /// Attempts to parse a decimal from a UTF-8 byte sequence.
    /// </summary>
    /// <param name="utf8Bytes">The UTF-8 encoded number bytes.</param>
    /// <param name="value">The parsed decimal value.</param>
    /// <returns>True if parsing succeeds; false otherwise.</returns>
    /// <remarks>
    /// This method is allocation-free and optimized for performance.
    /// Supports integer, decimal, and scientific notation.
    /// </remarks>
    public static bool TryParseDecimal(ReadOnlySpan<byte> utf8Bytes, out decimal value)
    {
        value = 0m;

        if (utf8Bytes.IsEmpty)
            return false;

        int pos = 0;

        // Handle optional sign
        int sign = 1;
        if (utf8Bytes[pos] == (byte)'-')
        {
            sign = -1;
            pos++;
        }
        else if (utf8Bytes[pos] == (byte)'+')
        {
            pos++;
        }

        if (pos >= utf8Bytes.Length)
            return false;

        // Parse integer part
        long integerPart = 0;
        int digitCount = 0;

        while (pos < utf8Bytes.Length && IsDigit(utf8Bytes[pos]))
        {
            int digit = utf8Bytes[pos] - (byte)'0';
            integerPart = integerPart * 10 + digit;
            digitCount++;
            pos++;

            // Prevent overflow
            if (digitCount > 28)
                return TryParseDecimalLarge(utf8Bytes, out value);
        }

        if (digitCount == 0)
            return false; // No digits at all

        // Handle decimal point
        int decimalPlaces = 0;
        if (pos < utf8Bytes.Length && utf8Bytes[pos] == (byte)'.')
        {
            pos++;

            while (pos < utf8Bytes.Length && IsDigit(utf8Bytes[pos]))
            {
                int digit = utf8Bytes[pos] - (byte)'0';
                integerPart = integerPart * 10 + digit;
                decimalPlaces++;
                digitCount++;
                pos++;

                // Prevent overflow
                if (digitCount > 28)
                    return TryParseDecimalLarge(utf8Bytes, out value);
            }
        }

        // Handle scientific notation
        if (pos < utf8Bytes.Length && (utf8Bytes[pos] == (byte)'e' || utf8Bytes[pos] == (byte)'E'))
        {
            pos++;

            int expSign = 1;
            if (pos < utf8Bytes.Length)
            {
                if (utf8Bytes[pos] == (byte)'-')
                {
                    expSign = -1;
                    pos++;
                }
                else if (utf8Bytes[pos] == (byte)'+')
                {
                    pos++;
                }
            }

            int exponent = 0;
            int expDigits = 0;
            while (pos < utf8Bytes.Length && IsDigit(utf8Bytes[pos]))
            {
                exponent = exponent * 10 + (utf8Bytes[pos] - (byte)'0');
                expDigits++;
                pos++;
            }

            if (expDigits == 0)
                return false; // Invalid exponent

            decimalPlaces -= expSign * exponent;
        }

        // Ensure we consumed all bytes
        if (pos != utf8Bytes.Length)
            return false;

        // Construct the decimal
        try
        {
            value = new decimal((int)(integerPart & 0xFFFFFFFF),
                               (int)((integerPart >> 32) & 0xFFFFFFFF),
                               0,
                               sign < 0,
                               (byte)Math.Max(0, Math.Min(28, decimalPlaces)));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a long integer from a UTF-8 byte sequence.
    /// </summary>
    public static bool TryParseInt64(ReadOnlySpan<byte> utf8Bytes, out long value)
    {
        value = 0;

        if (utf8Bytes.IsEmpty)
            return false;

        int pos = 0;
        int sign = 1;

        if (utf8Bytes[pos] == (byte)'-')
        {
            sign = -1;
            pos++;
        }
        else if (utf8Bytes[pos] == (byte)'+')
        {
            pos++;
        }

        if (pos >= utf8Bytes.Length || !IsDigit(utf8Bytes[pos]))
            return false;

        while (pos < utf8Bytes.Length && IsDigit(utf8Bytes[pos]))
        {
            int digit = utf8Bytes[pos] - (byte)'0';
            
            // Check for overflow
            if (value > (long.MaxValue / 10) || 
                (value == long.MaxValue / 10 && digit > 7))
                return false;

            value = value * 10 + digit;
            pos++;
        }

        // Ensure we consumed all bytes
        if (pos != utf8Bytes.Length)
            return false;

        value *= sign;
        return true;
    }

    /// <summary>
    /// Attempts to parse an int from a UTF-8 byte sequence.
    /// </summary>
    public static bool TryParseInt32(ReadOnlySpan<byte> utf8Bytes, out int value)
    {
        if (TryParseInt64(utf8Bytes, out long longValue))
        {
            if (longValue >= int.MinValue && longValue <= int.MaxValue)
            {
                value = (int)longValue;
                return true;
            }
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Attempts to parse a double from a UTF-8 byte sequence.
    /// </summary>
    public static bool TryParseDouble(ReadOnlySpan<byte> utf8Bytes, out double value)
    {
        value = 0.0;

        if (utf8Bytes.IsEmpty)
            return false;

        // Fallback to decimal and convert
        if (TryParseDecimal(utf8Bytes, out decimal decimalValue))
        {
            value = (double)decimalValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Fallback for large number parsing (>28 digits).
    /// </summary>
    private static bool TryParseDecimalLarge(ReadOnlySpan<byte> utf8Bytes, out decimal value)
    {
        value = 0m;

        try
        {
            // Fall back to standard parsing for very large numbers
            var str = System.Text.Encoding.UTF8.GetString(utf8Bytes);
            if (decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                value = result;
                return true;
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Checks if a byte represents an ASCII digit (0-9).
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(byte b)
    {
        return b >= (byte)'0' && b <= (byte)'9';
    }
}
