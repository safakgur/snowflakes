using System.Diagnostics;

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
    ///     Gets an encoder that will convert a snowflake to its base 36 representation
    ///     using 0-9 and A-Z (in this order) as digits.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values produced by this encoder are safe to use in URIs.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Integer</term>
    ///             <description>Encoded</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>0</description>
    ///         </item>
    ///         <item>
    ///             <term>9</term>
    ///             <description>9</description>
    ///         </item>
    ///         <item>
    ///             <term>10</term>
    ///             <description>A</description>
    ///         </item>
    ///         <item>
    ///             <term>35</term>
    ///             <description>Z</description>
    ///         </item>
    ///         <item>
    ///             <term>36</term>
    ///             <description>10</description>
    ///         </item>
    ///         <item>
    ///             <term>46</term>
    ///             <description>1A</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static SnowflakeEncoder Base36Upper { get; } =
        new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");

    /// <summary>
    ///     Gets an encoder that will convert a snowflake to its base 36 representation
    ///     using 0-9 and a-z (in this order) as digits.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values produced by this encoder are safe to use in URIs.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Integer</term>
    ///             <description>Encoded</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>0</description>
    ///         </item>
    ///         <item>
    ///             <term>9</term>
    ///             <description>9</description>
    ///         </item>
    ///         <item>
    ///             <term>10</term>
    ///             <description>a</description>
    ///         </item>
    ///         <item>
    ///             <term>35</term>
    ///             <description>z</description>
    ///         </item>
    ///         <item>
    ///             <term>36</term>
    ///             <description>10</description>
    ///         </item>
    ///         <item>
    ///             <term>46</term>
    ///             <description>1a</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static SnowflakeEncoder Base36Lower { get; } =
        new("0123456789abcdefghijklmnopqrstuvwxyz");

    /// <summary>
    ///     Gets an encoder that will convert a snowflake to its base 62 representation
    ///     using 0-9, A-Z, and a-z (in this order) as digits.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Values produced by this encoder are safe to use in URIs.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Integer</term>
    ///             <description>Encoded</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>0</description>
    ///         </item>
    ///         <item>
    ///             <term>9</term>
    ///             <description>9</description>
    ///         </item>
    ///         <item>
    ///             <term>10</term>
    ///             <description>A</description>
    ///         </item>
    ///         <item>
    ///             <term>35</term>
    ///             <description>Z</description>
    ///         </item>
    ///         <item>
    ///             <term>36</term>
    ///             <description>a</description>
    ///         </item>
    ///         <item>
    ///             <term>61</term>
    ///             <description>z</description>
    ///         </item>
    ///         <item>
    ///             <term>62</term>
    ///             <description>10</description>
    ///         </item>
    ///         <item>
    ///             <term>71</term>
    ///             <description>19</description>
    ///         </item>
    ///         <item>
    ///             <term>72</term>
    ///             <description>1A</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static SnowflakeEncoder Base62 { get; } =
        new("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    /// <summary>
    ///     Gets an encoder that will convert a snowflake to its base 62 representation
    ///     using -, 0-9, A-Z, _, and a-z  (in this order) as digits.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note that this encoder represents an integer identifier in a specific base using
    ///         extra digits, similar to base 2 (binary digits: 0-1) and base 16 (hexadecimal
    ///         digits: 0-9, A-F). It should not be confused with the binary-to-text encoding
    ///         format that is also known as Base64. which is aimed to represent arbitrary
    ///         bytes rather than being a custom-base representation of numbers.
    ///     </para>
    ///     <para>
    ///         Values produced by this encoder are safe to use in URIs.
    ///     </para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Integer</term>
    ///             <description>Encoded</description>
    ///         </listheader>
    ///         <item>
    ///             <term>0</term>
    ///             <description>-</description>
    ///         </item>
    ///         <item>
    ///             <term>1</term>
    ///             <description>0</description>
    ///         </item>
    ///         <item>
    ///             <term>10</term>
    ///             <description>9</description>
    ///         </item>
    ///         <item>
    ///             <term>11</term>
    ///             <description>A</description>
    ///         </item>
    ///         <item>
    ///             <term>36</term>
    ///             <description>Z</description>
    ///         </item>
    ///         <item>
    ///             <term>37</term>
    ///             <description>_</description>
    ///         </item>
    ///         <item>
    ///             <term>38</term>
    ///             <description>a</description>
    ///         </item>
    ///         <item>
    ///             <term>63</term>
    ///             <description>z</description>
    ///         </item>
    ///         <item>
    ///             <term>64</term>
    ///             <description>0-</description>
    ///         </item>
    ///         <item>
    ///             <term>65</term>
    ///             <description>00</description>
    ///         </item>
    ///         <item>
    ///             <term>74</term>
    ///             <description>09</description>
    ///         </item>
    ///         <item>
    ///             <term>75</term>
    ///             <description>0A</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static SnowflakeEncoder Base64Snow { get; } =
        new("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");

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
    ///     <paramref name="encodedSnowflake" />is null.
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
