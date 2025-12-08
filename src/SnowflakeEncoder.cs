using System.Collections.Frozen;
using System.Diagnostics;
using System.Numerics;
using Lock =
#if NET9_0_OR_GREATER
    System.Threading.Lock;
#else
    System.Object;
#endif

namespace Snowflakes;

/// <summary>Converts snowflakes to custom-base-encoded strings.</summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public sealed class SnowflakeEncoder
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal FrozenDictionary<Type, object> _typedEncoders = FrozenDictionary<Type, object>.Empty;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Lock _typedEncodersLock = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _digits;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly string _zeroDigit;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnowflakeEncoder" />
    ///     class using the specified digit characters.
    /// </summary>
    /// <param name="digits">
    ///     <para>A string containing the set of unique characters to use as encoding digits.</para>
    ///     <para>
    ///         The number of digits (characters in the string) determines the encoding base,
    ///         and the order of digits defines their encoded values.
    ///         The first digit is zero.
    ///     </para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="digits" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="digits" /> contains fewer than two, or duplicate characters.
    /// </exception>
    internal SnowflakeEncoder(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);

        if (digits.Length < 2)
            throw new ArgumentException("Digits must contain at least two characters.", nameof(digits));

        if (digits.ToHashSet().Count != digits.Length)
            throw new ArgumentException("Digits contains duplicate characters.", nameof(digits));

        _digits = digits;
        _zeroDigit = digits[0].ToString();
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Base ({_digits.Length}): {_digits}";

    /// <typeparam name="T">The snowflake type.</typeparam>
    /// <inheritdoc cref="Encoder{T}.Encode" />
    public string Encode<T>(T snowflake)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T> =>
        GetOrCreateTypedEncoder<T>().Encode(snowflake);

    /// <typeparam name="T">The snowflake type.</typeparam>
    /// <inheritdoc cref="Encoder{T}.Decode" />
    public T Decode<T>(string encodedSnowflake)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T> =>
        GetOrCreateTypedEncoder<T>().Decode(encodedSnowflake);

    /// <inheritdoc cref="Encoder{T}.Decode" />
    public long Decode(string encodedSnowflake) =>
        Decode<long>(encodedSnowflake);

    private Encoder<T> GetOrCreateTypedEncoder<T>()
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        var key = typeof(T);
        if (!_typedEncoders.TryGetValue(key, out var typedEncoder))
            lock (_typedEncodersLock)
                if (!_typedEncoders.TryGetValue(key, out typedEncoder))
                {
                    var bitCount = int.CreateChecked(T.PopCount(T.AllBitsSet));
                    typedEncoder = bitCount switch
                    {
                        < 32 => new Encoder32<T>(this),
                        32 when T.MinValue < T.Zero => new Encoder32<T>(this), // Signed
                        _ => new BigEncoder<T>(this)
                    };

                    // Frozen dictionaries are expensive to create but performant to use.
                    // We can only have as many encoders as IBinaryInteger implementations
                    // in a codebase, and most applications won't use more than one,
                    // hence we are fine with this trade off.
                    _typedEncoders = _typedEncoders
                        .Append(new(key, typedEncoder))
                        .ToFrozenDictionary();
                }

        return (typedEncoder as Encoder<T>)!;
    }

    private abstract class Encoder<T>
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        protected readonly SnowflakeEncoder Parent;
        protected readonly int MaxEncodedLength;

        public Encoder(SnowflakeEncoder parent)
        {
            Parent = parent;

            var bitCount = int.CreateChecked(T.PopCount(T.MaxValue));
            MaxEncodedLength = (int)Math.Ceiling(bitCount / Math.Log2(Parent._digits.Length));
        }

        /// <summary>Converts the specified snowflake to a custom-base-encoded string.</summary>
        /// <param name="snowflake">The snowflake to convert.</param>
        /// <returns>The snowflake's representation in the encoder-specific format.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="snowflake" /> is negative.
        /// </exception>
        public string Encode(T snowflake)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(snowflake);

            if (T.IsZero(snowflake))
                return Parent._zeroDigit;

            return EncodeImpl(snowflake);
        }

        protected abstract string EncodeImpl(T snowflake);

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
        ///     <paramref name="encodedSnowflake" /> represents a number that
        ///     is bigger than the snowflake type's maximum value.
        /// </exception>
        public T Decode(string encodedSnowflake)
        {
            ArgumentNullException.ThrowIfNull(encodedSnowflake);

            if (encodedSnowflake.Length == 0)
                throw new FormatException("Empty encoded snowflake.");


            return DecodeImpl(encodedSnowflake);
        }

        protected abstract T DecodeImpl(string encodedSnowflake);
    }

    private sealed class BigEncoder<T> : Encoder<T>
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        private readonly T _digitCount;
        private readonly FrozenDictionary<char, T> _digitValues;

        public BigEncoder(SnowflakeEncoder parent) : base(parent)
        {
            _digitCount = T.CreateChecked(Parent._digits.Length);
            _digitValues = Parent._digits
                .Select((c, i) => (c, T.CreateChecked(i)))
                .ToFrozenDictionary(p => p.c, p => p.Item2);
        }

        protected override string EncodeImpl(T snowflake)
        {
            Span<char> chars = stackalloc char[MaxEncodedLength];
            var resultIndex = MaxEncodedLength - 1;
            var digits = Parent._digits;
            do
            {
                var sourceIndex = int.CreateChecked(snowflake % _digitCount);
                chars[resultIndex--] = digits[sourceIndex];
                snowflake /= _digitCount;
            } while (snowflake != T.Zero);

            return chars[(resultIndex + 1)..].ToString();
        }

        protected override T DecodeImpl(string encodedSnowflake)
        {
            var result = T.Zero;
            var multiplier = T.One;
            var index = encodedSnowflake.Length - 1;
            while (true)
            {
                if (!_digitValues.TryGetValue(encodedSnowflake[index], out var digit))
                    throw new FormatException(
                        $"Invalid character, '{encodedSnowflake[index]}' in encoded snowflake.");

                result = checked(result + (digit * multiplier));
                if (index == 0)
                    return result;

                index--;
                multiplier = checked(multiplier * _digitCount);
            }
        }
    }

    private sealed class Encoder32<T> : Encoder<T>
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        private readonly FrozenDictionary<char, int> _digitValues;

        public Encoder32(SnowflakeEncoder parent) : base(parent)
        {
            _digitValues = Parent._digits
                .Take(int.CreateChecked(T.MaxValue) is var max && max < int.MaxValue ? max + 1 : max)
                .Select((c, i) => (c, i))
                .ToFrozenDictionary(p => p.c, p => p.i);
        }

        protected override string EncodeImpl(T snowflake)
        {
            var snowflake32 = int.CreateChecked(snowflake);

            Span<char> chars = stackalloc char[MaxEncodedLength];
            var index = MaxEncodedLength - 1;
            var digits = Parent._digits;
            do
            {
                chars[index--] = digits[snowflake32 % digits.Length];
                snowflake32 /= digits.Length;
            } while (snowflake32 != 0);

            return chars[(index + 1)..].ToString();
        }

        protected override T DecodeImpl(string encodedSnowflake)
        {
            var result = 0;
            var multiplier = 1;
            var index = encodedSnowflake.Length - 1;
            var digitCount = Parent._digits.Length;
            while (true)
            {
                if (!_digitValues.TryGetValue(encodedSnowflake[index], out var digit))
                    throw new FormatException(
                        $"Invalid character, '{encodedSnowflake[index]}' in encoded snowflake.");

                result = checked(result + (digit * multiplier));
                if (index == 0)
                    return T.CreateChecked(result);

                index--;
                multiplier = checked(multiplier * digitCount);
            }
        }
    }
}
