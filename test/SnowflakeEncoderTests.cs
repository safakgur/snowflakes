using System.Reflection;

namespace Snowflakes.Tests;

public sealed class SnowflakeEncoderTests
{
    public static TheoryData<SnowflakeEncoder> AllEncoders { get; } = new(
        SnowflakeEncoder.Base36Upper,
        SnowflakeEncoder.Base36Lower,
        SnowflakeEncoder.Base62,
        SnowflakeEncoder.Base64Snow);

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
    [InlineData(nameof(SnowflakeEncoder.Base36Upper), "1Y2P0IJ32E8E7", "1Y2P0IJ32E8E8")]
    [InlineData(nameof(SnowflakeEncoder.Base36Lower), "1y2p0ij32e8e7", "1y2p0ij32e8e8")]
    [InlineData(nameof(SnowflakeEncoder.Base62), "AzL8n0Y58m7", "AzL8n0Y58m8")]
    [InlineData(nameof(SnowflakeEncoder.Base64Snow), "6zzzzzzzzzz", "7zzzzzzzzz-")]
    public void Decode_throws_when_encodedSnowflake_is_too_big(
        string encoderName, string maxEncoded, string maxPlusOneEncoded)
    {
        var encoder = (SnowflakeEncoder)typeof(SnowflakeEncoder)
            .GetProperty(encoderName, BindingFlags.Public | BindingFlags.Static)!
            .GetValue(obj: null)!;

        var maxDecoded = encoder.Decode(maxEncoded);
        Assert.Equal(long.MaxValue, maxDecoded);
        Assert.Throws<OverflowException>(() => encoder.Decode(maxPlusOneEncoded));
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
    [InlineData(5013014052624339181, "123456789abcd")]
    [InlineData(1899220601727276649, "efghijklmnop")]
    [InlineData(2718988031752955, "qrstuvwxyz")]
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
    [InlineData(867042935339397963, "123456789AB")]
    [InlineData(165333629449474169, "CDEFGHIJKL")]
    [InlineData(302923689427890599, "MNOPQRSTUV")]
    [InlineData(440513749406307029, "WXYZabcdef")]
    [InlineData(578103809384723459, "ghijklmnop")]
    [InlineData(715693869363139889, "qrstuvwxyz")]
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
    [InlineData(1189812668901397131, "0123456789A")]
    [InlineData(219894577724208405, "BCDEFGHIJK")]
    [InlineData(402897991153866655, "LMNOPQRSTU")]
    [InlineData(585901404583524905, "VWXYZ_abcd")]
    [InlineData(768904818013183155, "efghijklmn")]
    [InlineData(951908231442841405, "opqrstuvwx")]
    [InlineData(4031, "yz")]
    public void Base64Snow_Encode_and_Decode_work_correctly(long value, string expectedEncoded)
    {
        var encoder = SnowflakeEncoder.Base64Snow;

        var encoded = encoder.Encode(value);
        Assert.Equal(expectedEncoded, encoded);

        var decoded = encoder.Decode(encoded);
        Assert.Equal(value, decoded);
    }
}
