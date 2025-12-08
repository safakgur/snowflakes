using System.Security.Cryptography;
using Snowflakes.Components;
using Snowflakes.Resources;

namespace Snowflakes.Tests.Components;

public sealed class ConstantSnowflakeComponentTests
{
    [Theory]
    [MemberData(nameof(SnowflakeComponentTests.LengthInBits_IsValid_Data), MemberType = typeof(SnowflakeComponentTests))]
    public void Ctor_validates_lengthInBits(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new ConstantSnowflakeComponent<long>(lengthInBits, value: 0L);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new ConstantSnowflakeComponent<long>(lengthInBits, value: 0L));
        }
    }


    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(long.MaxValue, true)]
    public void Ctor_validates_value(long value, bool isValid)
    {
        if (isValid)
        {
            var component = new ConstantSnowflakeComponent<long>(lengthInBits: 10, value);
            Assert.Equal(value, component.Value);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(value), () =>
                new ConstantSnowflakeComponent<long>(lengthInBits: 10, value: value));
        }
    }

    [Theory]
    [MemberData(nameof(SnowflakeComponentTests.LengthInBits_IsValid_Data), MemberType = typeof(SnowflakeComponentTests))]
    [Obsolete(DeprecationMessages.HashedConstantComponent)]
    public void Hashing_ctor_validates_lengthInBits(int lengthInBits, bool isValid)
    {
        var valueToHash = Guid.NewGuid().ToString("n");
        using var hashAlg = MD5.Create();

        if (isValid)
        {
            var component = new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash, hashAlg);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash, hashAlg));
        }
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(" ", null)]
    [InlineData("A", null)]
    [Obsolete(DeprecationMessages.HashedConstantComponent)]
    public void Hashing_ctor_validates_valueToHash(string? valueToHash, Type? exceptionType)
    {
        var lengthInBits = 10;
        using var hashAlg = MD5.Create();

        if (exceptionType is null)
        {
            _ = new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash!, hashAlg);
            return;
        }

        var ex = Assert.Throws(exceptionType, () =>
            new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash!, hashAlg));

        Assert.Equal(nameof(valueToHash), (ex as ArgumentException)!.ParamName);
    }

    [Fact]
    [Obsolete(DeprecationMessages.HashedConstantComponent)]
    public void Hashing_ctor_validates_hashAlg()
    {
        var lengthInBits = 10;
        var valueToHash = Guid.NewGuid().ToString("n");

        Assert.Throws<ArgumentNullException>("hashAlg", () =>
            new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash!, hashAlg: null!));
    }

    [Fact]
    [Obsolete(DeprecationMessages.HashedConstantComponent)]
    public void Hashing_ctor_produces_expected_hash()
    {
        var lengthInBits = 63;
        var valueToHash = "test value";
        var expectedMD5Hash = 2353163291832495564L;
        var expectedSHA256Hash = 8069623936395563335L;

        using var md5HashAlg = MD5.Create();
        var component = new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash, md5HashAlg);
        var value = component.GetValue(new(component));
        Assert.Equal(expectedMD5Hash, value);

        using var sha256HashAlg = SHA256.Create();
        component = new ConstantSnowflakeComponent<long>(lengthInBits, valueToHash, sha256HashAlg);
        value = component.GetValue(new(component));
        Assert.Equal(expectedSHA256Hash, value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void GetValue_returns_correct_value(long value)
    {
        var component = new ConstantSnowflakeComponent<long>(lengthInBits: 10, value: value);

        Assert.Equal(value, component.GetValue(new(component)));
    }
}
