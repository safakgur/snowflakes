namespace Snowflakes.Tests;

#pragma warning disable CS0618 // Type or member is obsolete

public sealed class SnowflakeEncoderTests
{
    private static readonly SnowflakeEncoder s_defaultEncoder = SnowflakeEncoder.Base62Ordinal;

    [Fact]
    public void Ctor_throws_when_digits_is_null()
    {
        Assert.Throws<ArgumentNullException>("digits", () => new SnowflakeEncoder(null!));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("01", true)]
    public void Ctor_throws_when_digits_has_fewer_than_two_characters(string digits, bool isValid)
    {
        if (isValid)
            _ = new SnowflakeEncoder(digits);
        else
            Assert.Throws<ArgumentException>(nameof(digits), () => new SnowflakeEncoder(digits));
    }

    [Fact]
    public void Ctor_throws_when_digits_has_duplicates()
    {
        Assert.Throws<ArgumentException>("digits", () => new SnowflakeEncoder("0120"));
    }

    [Fact]
    public void Encode_throws_when_snowflake_is_negative()
    {
        Assert.Throws<ArgumentOutOfRangeException>("snowflake", () => s_defaultEncoder.Encode(-1U));

        Assert.Throws<ArgumentOutOfRangeException>("snowflake", () => s_defaultEncoder.Encode(-1));
    }

    [Fact]
    public void Encode_returns_cached_zero()
    {
        var zero1 = s_defaultEncoder.Encode<byte>(0);
        var zero2 = s_defaultEncoder.Encode<long>(0);

        Assert.Same(zero1, zero2);
    }

    [Theory]
    [InlineData(255, new[] { 1, 0 })]
    [InlineData(256, new[] { 255 })]
    [InlineData(257, new[] { 255 })]
    public void Encode_works_as_expected_around_digit_length_boundary(int digitCount, int[] expectedDigitIndexes)
    {
        char[] digits = [.. Enumerable.Range('A', digitCount).Select(i => (char)i)];
        var encoder = new SnowflakeEncoder(new(digits));
        var expectedEncoded = new string([.. expectedDigitIndexes.Select(i => digits[i])]);

        var encoded = encoder.Encode(byte.MaxValue);

        Assert.Equal(expectedEncoded, encoded);
    }

    [Fact]
    public void Decode_throws_when_encodedSnowflake_is_null()
    {
        Assert.Throws<ArgumentNullException>("encodedSnowflake", () => s_defaultEncoder.Decode<long>(null!));

        Assert.Throws<ArgumentNullException>("encodedSnowflake", () => s_defaultEncoder.Decode(null!));

        Assert.Throws<ArgumentNullException>("encodedSnowflake", () => s_defaultEncoder.Decode<int>(null!));
    }

    [Fact]
    public void Decode_throws_when_encodedSnowflake_is_empty_or_invalid()
    {
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<long>(string.Empty));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode(string.Empty));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<int>(string.Empty));

        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<long>(" "));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode(" "));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<int>(" "));

        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<long>("*"));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode("*"));
        Assert.Throws<FormatException>(() => s_defaultEncoder.Decode<int>("*"));
    }

    [Fact]
    public void Decode_throws_when_encodedSnowflake_is_too_big()
    {
        var encoded = s_defaultEncoder.Encode(byte.MaxValue + 1);

        Assert.Throws<OverflowException>(() => s_defaultEncoder.Decode<byte>(encoded));
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(byte.MaxValue, "2z")]
    public void Can_encode_and_decode_Byte(byte snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<byte>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(sbyte.MaxValue, "0z")]
    public void Can_encode_and_decode_SByte(sbyte snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<sbyte>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(short.MaxValue, "6zz")]
    public void Can_encode_and_decode_Int16(short snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<short>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(ushort.MaxValue, "Ezz")]
    public void Can_encode_and_decode_UInt16(ushort snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<ushort>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(int.MaxValue, "0zzzzz")]
    public void Can_encode_and_decode_Int32(int snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<int>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(uint.MaxValue, "2zzzzz")]
    public void Can_encode_and_decode_UInt32(uint snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<uint>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(long.MaxValue, "6zzzzzzzzzz")]
    public void Can_encode_and_decode_Int64(long snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<long>(encoded);

        Assert.Equal(snowflake, decoded);

        decoded = encoder.Decode(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(ulong.MaxValue, "Ezzzzzzzzzz")]
    public void Can_encode_and_decode_UInt64(ulong snowflake, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Ordinal;

        var encoded = encoder.Encode(snowflake);

        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode<ulong>(encoded);

        Assert.Equal(snowflake, decoded);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(35, "Z")]
    [InlineData(36, "10")]
    [InlineData(46, "1A")]
    [InlineData(5013014052624339181, "123456789ABCD")]
    [InlineData(1899220601727276649, "EFGHIJKLMNOP")]
    [InlineData(2718988031752955, "QRSTUVWXYZ")]
    public void Base36UpperOrdinal_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        Assert.All([SnowflakeEncoder.Base36UpperOrdinal, SnowflakeEncoder.Base36Upper], encoder =>
        {
            var encoded = encoder.Encode(value);
            Assert.Equal(expectedEncoded, encoded);

            var decoded = encoder.Decode<long>(encoded);
            Assert.Equal(value, decoded);

            decoded = encoder.Decode(encoded);
            Assert.Equal(value, decoded);
        });
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "a")]
    [InlineData(35, "z")]
    [InlineData(36, "10")]
    [InlineData(46, "1a")]
    [InlineData(5013014052624339181, "123456789abcd")]
    [InlineData(1899220601727276649, "efghijklmnop")]
    [InlineData(2718988031752955, "qrstuvwxyz")]
    public void Base36LowerOrdinal_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        Assert.All([SnowflakeEncoder.Base36LowerOrdinal, SnowflakeEncoder.Base36Lower], encoder =>
        {
            var encoded = encoder.Encode(value);
            Assert.Equal(expectedEncoded, encoded);

            var decoded = encoder.Decode<long>(encoded);
            Assert.Equal(value, decoded);

            decoded = encoder.Decode(encoded);
            Assert.Equal(value, decoded);
        });
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(35, "Z")]
    [InlineData(36, "a")]
    [InlineData(61, "z")]
    [InlineData(62, "10")]
    [InlineData(71, "19")]
    [InlineData(72, "1A")]
    [InlineData(867042935339397963, "123456789AB")]
    [InlineData(165333629449474169, "CDEFGHIJKL")]
    [InlineData(302923689427890599, "MNOPQRSTUV")]
    [InlineData(440513749406307029, "WXYZabcdef")]
    [InlineData(578103809384723459, "ghijklmnop")]
    [InlineData(715693869363139889, "qrstuvwxyz")]
    public void Base62Ordinal_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        Assert.All([SnowflakeEncoder.Base62Ordinal, SnowflakeEncoder.Base62], encoder =>
        {
            var encoded = encoder.Encode(value);
            Assert.Equal(expectedEncoded, encoded);

            var decoded = encoder.Decode<long>(encoded);
            Assert.Equal(value, decoded);

            decoded = encoder.Decode(encoded);
            Assert.Equal(value, decoded);
        });
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(1, "0")]
    [InlineData(10, "9")]
    [InlineData(11, "A")]
    [InlineData(36, "Z")]
    [InlineData(37, "_")]
    [InlineData(38, "a")]
    [InlineData(63, "z")]
    [InlineData(64, "0-")]
    [InlineData(65, "00")]
    [InlineData(74, "09")]
    [InlineData(75, "0A")]
    [InlineData(1189812668901397131, "0123456789A")]
    [InlineData(219894577724208405, "BCDEFGHIJK")]
    [InlineData(402897991153866655, "LMNOPQRSTU")]
    [InlineData(585901404583524905, "VWXYZ_abcd")]
    [InlineData(768904818013183155, "efghijklmn")]
    [InlineData(951908231442841405, "opqrstuvwx")]
    [InlineData(4031, "yz")]
    public void Base64Ordinal_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        Assert.All([SnowflakeEncoder.Base64Ordinal, SnowflakeEncoder.Base64Snow], encoder =>
        {
            var encoded = encoder.Encode(value);
            Assert.Equal(expectedEncoded, encoded);

            var decoded = encoder.Decode<long>(encoded);
            Assert.Equal(value, decoded);

            decoded = encoder.Decode(encoded);
            Assert.Equal(value, decoded);
        });
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
