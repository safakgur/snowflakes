using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public sealed class SequenceSnowflakeComponentTests
{
    [Theory]
    [MemberData(nameof(SnowflakeComponentTests.LengthInBits_IsValid_Data), MemberType = typeof(SnowflakeComponentTests))]
    public void Ctor_validates_lengthInBits(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new SequenceSnowflakeComponent<long>(lengthInBits, refComponentIndex: 0);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new SequenceSnowflakeComponent<long>(lengthInBits, refComponentIndex: 0));
        }
    }

    [Theory]
    [InlineData(1, -1, false)]
    [InlineData(1, 1, true)]
    [InlineData(1, 62, true)]
    [InlineData(1, 63, false)] // A snowflate has 63 bits, so it can no more than 63 1-bit components
    [InlineData(2, 61, true)]
    [InlineData(2, 62, false)] // If this 2-bit ad all others are 1-bit, there can be no more than 62 components 
    public void Ctor_validates_refComponentIndex(int lengthInBits, int refComponentIndex, bool isValid)
    {
        if (isValid)
        {
            var component = new SequenceSnowflakeComponent<long>(lengthInBits, refComponentIndex);
            Assert.Equal(refComponentIndex, component.ReferenceComponentIndex);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(refComponentIndex), () =>
                new SequenceSnowflakeComponent<long>(lengthInBits, refComponentIndex));
        }
    }

    [Fact]
    public void GetValue_returns_correcly_incremented_and_reset_sequence()
    {
        var component = new SequenceSnowflakeComponent<long>(lengthInBits: 4, refComponentIndex: 0);

        // First
        var value = GetValueForRefComponentValue(0);
        Assert.Equal(0, value);

        // Ref unchanged
        value = GetValueForRefComponentValue(0);
        Assert.Equal(1, value);

        // Ref changed
        value = GetValueForRefComponentValue(1);
        Assert.Equal(0, value);

        // Ref unchanged
        value = GetValueForRefComponentValue(1);
        Assert.Equal(1, value);

        long GetValueForRefComponentValue(int refComponentValue)
        {
            var refComponent = new ConstantSnowflakeComponent<long>(lengthInBits: 10, value: refComponentValue);
            var ctx = new SnowflakeGenerationContext<long>(refComponent, component);

            _ = refComponent.GetValue(ctx);

            return component.GetValue(ctx);
        }
    }

    [Fact]
    public void GetValue_throws_when_calculated_value_is_out_of_range()
    {
        var component = new SequenceSnowflakeComponent<long>(lengthInBits: 1, refComponentIndex: 0);

        var refComponent = new ConstantSnowflakeComponent<long>(lengthInBits: 1, value: 1);
        var ctx = new SnowflakeGenerationContext<long>(refComponent, component);

        Assert.Equal(0, component.GetValue(ctx));
        Assert.Equal(1, component.GetValue(ctx));
        Assert.Throws<OverflowException>(() => component.GetValue(ctx));
    }
}
