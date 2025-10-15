using System.ComponentModel;
using System.Diagnostics;
using Snowflakes.Resources;

namespace Snowflakes;

/// <summary>Converts snowflakes to custom-base-encoded strings.</summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public sealed class SnowflakeEncoder
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _digits;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _zeroDigit;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Dictionary<char, long> _digitValues;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly int _maxEncodedLength;

    private SnowflakeEncoder(string digits)
    {
        const int BitsInLong = 64;

        _digits = digits;
        _zeroDigit = digits[0].ToString();
        _digitValues = new Dictionary<char, long>(digits.Length);
        for (var i = 0; i < digits.Length; i++)
            _digitValues[digits[i]] = i;

        _maxEncodedLength = (int)Math.Ceiling(BitsInLong / Math.Log2(digits.Length));
    }

    /// <summary>
    ///     Gets an encoder that converts a snowflake to its uppercase base 36 representation, using
    ///     the digit set: US-ASCII digits (0-9), and uppercase letters (A-Z).
    /// </summary>
    /// <remarks>
    ///     This encoder expresses numeric values using a set of digits ordered by ASCII code point.
    ///     It should not be confused with binary-to-text encoding of arbitrary bytes.
    /// </remarks>
    public static SnowflakeEncoder Base36UpperOrdinal { get; } =
        new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");

    /// <inheritdoc cref="Base36UpperOrdinal" />
    [Obsolete(DeprecationMessages.BuiltInBase36UpperEncoding)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SnowflakeEncoder Base36Upper => Base36UpperOrdinal;

    /// <summary>
    ///     Gets an encoder that converts a snowflake to its lowercase base 36 representation, using
    ///     the digit set: US-ASCII digits (0-9), and lowercase letters (a-z).
    /// </summary>
    /// <remarks>
    ///     This encoder expresses numeric values using a set of digits ordered by ASCII code point.
    ///     It should not be confused with binary-to-text encoding of arbitrary bytes.
    /// </remarks>
    public static SnowflakeEncoder Base36LowerOrdinal { get; } =
        new("0123456789abcdefghijklmnopqrstuvwxyz");

    /// <inheritdoc cref="Base36LowerOrdinal" />
    [Obsolete(DeprecationMessages.BuiltInBase36LowerEncoding)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SnowflakeEncoder Base36Lower => Base36LowerOrdinal;

    /// <summary>
    ///     Gets an encoder that converts a snowflake to its base 62 representation, using the digit
    ///     set: US-ASCII digits (0-9), uppercase letters (A-Z), and lowercase letters (a-z).
    /// </summary>
    /// <remarks>
    ///     This encoder expresses numeric values using a set of digits ordered by ASCII code point.
    ///     It should not be confused with binary-to-text encoding of arbitrary bytes, such as base62.
    /// </remarks>
    public static SnowflakeEncoder Base62Ordinal { get; } =
        new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    /// <inheritdoc cref="Base62Ordinal" />
    [Obsolete(DeprecationMessages.BuiltInBase62Encoding)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SnowflakeEncoder Base62 => Base62Ordinal;

    /// <summary>
    ///     Gets an encoder that converts a snowflake to its URI-safe base 64 representation,
    ///     using the digit set: US-ASCII hyphen (-), digits (0-9), uppercase letters (A-Z),
    ///     underscore (_), and lowercase letters (a-z).
    /// </summary>
    /// <remarks>
    ///     This encoder expresses numeric values using a set of digits ordered by ASCII code point.
    ///     It should not be confused with binary-to-text encoding of arbitrary bytes, such as base64.
    /// </remarks>
    public static SnowflakeEncoder Base64Ordinal { get; } =
        new("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

    /// <inheritdoc cref="Base64Ordinal" />
    [Obsolete(DeprecationMessages.BuiltInBase64Encoding)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static SnowflakeEncoder Base64Snow => Base64Ordinal;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Base ({_digits.Length}): {_digits}";

    /// <summary>Converts the specified snowflake to a custom-base-encoded string.</summary>
    /// <param name="snowflake">The snowflake to convert.</param>
    /// <returns>The snowflake's representation in the encoder-specific format.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="snowflake" /> is negative.
    /// </exception>
    public string Encode(long snowflake)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(snowflake);

        if (snowflake == 0L)
            return _zeroDigit;

        Span<char> chars = stackalloc char[_maxEncodedLength];

        var index = _maxEncodedLength - 1;
        do
        {
            var remainder = (int)(snowflake % _digits.Length);
            chars[index--] = _digits[remainder];
            snowflake /= _digits.Length;
        } while (snowflake != 0);

        return chars[(index + 1).._maxEncodedLength].ToString();
    }

    /// <summary>Converts a custom-base-encoded snowflake back to its original value.</summary>
    /// <param name="encodedSnowflake">The encoded snowflake to convert back.</param>
    /// <returns>The original snowflake that was encoded.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="encodedSnowflake" /> is null.
    /// </exception>
    /// <exception cref="FormatException">
    ///     <paramref name="encodedSnowflake" /> is in an invalid format.
    /// </exception>
    /// <exception cref="OverflowException">
    ///     The number <paramref name="encodedSnowflake" /> represents is bigger than
    ///     <see cref="long.MaxValue" />.
    /// </exception>
    public long Decode(string encodedSnowflake)
    {
        ArgumentNullException.ThrowIfNull(encodedSnowflake);

        if (encodedSnowflake.Length == 0)
            throw new FormatException("Empty encoded snowflake.");

        var result = 0L;
        var multiplier = 1L;

        for (var i = encodedSnowflake.Length - 1; i >= 0; i--)
        {
            if (!_digitValues.TryGetValue(encodedSnowflake[i], out var digit))
                throw new FormatException(
                    $"Invalid character, '{encodedSnowflake[i]}' in encoded snowflake.");

            result = checked(result + (digit * multiplier));
            multiplier *= _digits.Length;
        }

        return result;
    }
}
