using Snowflakes.Components;

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
    [InlineData(0)]
    [InlineData(1)]
    public void GetValue_returns_correct_value(long value)
    {
        var component = new ConstantSnowflakeComponent<long>(lengthInBits: 10, value: value);

        Assert.Equal(value, component.GetValue(new(component)));
    }
}
