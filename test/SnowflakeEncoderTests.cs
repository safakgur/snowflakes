namespace Snowflakes.Tests;

public sealed class SnowflakeEncoderTests
{
    public static TheoryData<SnowflakeEncoder> AllEncoders { get; } = new()
    {
        SnowflakeEncoder.Base36Upper,
        SnowflakeEncoder.Base36Lower,
        SnowflakeEncoder.Base62,
        SnowflakeEncoder.Base64Snow
    };

    [Theory]
    [MemberData(nameof(AllEncoders))]
    public void Encode_throws_when_snowflake_is_negative(SnowflakeEncoder encoder)
    {
        Assert.Throws<ArgumentOutOfRangeException>("snowflake", () => encoder.Encode(-1));
    }

    [Theory]
    [MemberData(nameof(AllEncoders))]
    public void Decode_throws_when_encodedSnowflake_is_null(SnowflakeEncoder encoder)
    {
        Assert.Throws<ArgumentNullException>("encodedSnowflake", () => encoder.Decode(null!));
    }

    [Theory]
    [MemberData(nameof(AllEncoders))]
    public void Decode_throws_when_encodedSnowflake_is_empty_or_invalid(SnowflakeEncoder encoder)
    {
        Assert.Throws<FormatException>(() => encoder.Decode(string.Empty));
        Assert.Throws<FormatException>(() => encoder.Decode(" "));
        Assert.Throws<FormatException>(() => encoder.Decode("*"));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "A")]
    [InlineData(35, "Z")]
    [InlineData(36, "10")]
    [InlineData(46, "1A")]
    public void Base36Upper_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base36Upper;

        var encoded = encoder.Encode(value);
        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode(encoded);
        Assert.Equal(value, decoded);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(9, "9")]
    [InlineData(10, "a")]
    [InlineData(35, "z")]
    [InlineData(36, "10")]
    [InlineData(46, "1a")]
    public void Base36Lower_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base36Lower;

        var encoded = encoder.Encode(value);
        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode(encoded);
        Assert.Equal(value, decoded);
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
    public void Base62_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base62;

        var encoded = encoder.Encode(value);
        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode(encoded);
        Assert.Equal(value, decoded);
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
    public void Base64Snow_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Snow;

        var encoded = encoder.Encode(value);
        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode(encoded);
        Assert.Equal(value, decoded);
    }
}
