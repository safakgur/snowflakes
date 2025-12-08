using System.Numerics;
using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public sealed class SnowflakeComponentTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(7, true)] // sbyte - 1 bit is sign, 7 is usable
    [InlineData(8, false)]
    public void Ctor_validates_and_sets_lengthInBits_for_signed(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new IncrementingTestSnowflakeComponent<sbyte>(lengthInBits);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new IncrementingTestSnowflakeComponent<sbyte>(lengthInBits));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(8, true)] // byte - all 8 bits are usable
    [InlineData(9, false)]
    public void Ctor_validates_and_sets_lengthInBits_for_unsigned(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new IncrementingTestSnowflakeComponent<byte>(lengthInBits);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new IncrementingTestSnowflakeComponent<byte>(lengthInBits));
        }
    }

    [Theory]
    [InlineData(7, 0b_0_111_1111)] // sbyte - 1 bit is sign, 7 is usable
    [InlineData(6, 0b_0_011_1111)]
    [InlineData(1, 0b_0_000_0001)]
    public void Masking_is_correct_for_signed(int lengthInBits, sbyte expectedMask)
    {
        var component = new ConstantSnowflakeComponent<sbyte>(lengthInBits, sbyte.MaxValue);
        var ctx = new SnowflakeGenerationContext<sbyte>(component);

        var masked = component.GetValue(ctx);

        Assert.Equal(expectedMask, masked);
    }

    [Theory]
    [InlineData(8, 0b_1111_1111)] // byte - all 8 bits are usable
    [InlineData(7, 0b_0111_1111)]
    [InlineData(1, 0b_0000_0001)]
    public void Masking_is_correct_for_unsigned(int lengthInBits, byte expectedMask)
    {
        var component = new ConstantSnowflakeComponent<byte>(lengthInBits, byte.MaxValue);
        var ctx = new SnowflakeGenerationContext<byte>(component);

        var masked = component.GetValue(ctx);

        Assert.Equal(expectedMask, masked);
    }

    [Fact]
    public void GetValue_validates_ctx()
    {
        var component = new IncrementingTestSnowflakeComponent<long>(lengthInBits: 10);

        Assert.Throws<ArgumentNullException>("ctx", () => component.GetValue(null!));
    }

    [Theory]
    [InlineData(4, 0b_1101, 0b_1101)]
    [InlineData(3, 0b_1101, 0b_0101)]
    [InlineData(2, 0b_1011, 0b_0011)]
    [InlineData(1, 0b_1011, 0b_0001)]
    public void GetValue_masks_value(int lengthInBits, long originalValue, long maskedValue)
    {
        var component = new IncrementingTestSnowflakeComponent<long>(
            lengthInBits, originalValue, allowTruncation: true);

        var value = component.GetValue(new(component));

        Assert.Equal(maskedValue, value);
    }

    [Fact]
    public void GetValue_throws_when_calculated_value_is_out_of_range()
    {
        var component = new IncrementingTestSnowflakeComponent<long>(
            4, 0b_1110, allowTruncation: false);

        var ctx = new SnowflakeGenerationContext<long>(component);

        Assert.Equal(0b_1110, component.GetValue(ctx));
        Assert.Equal(0b_1111, component.GetValue(ctx));
        Assert.Throws<OverflowException>(() => component.GetValue(ctx));
    }

    [Fact]
    public void GetValue_saves_value_in_LastValue()
    {
        var component = new IncrementingTestSnowflakeComponent<long>(lengthInBits: 10, startValue: 1);

        Assert.Equal(0, component.LastValue);

        var value = component.GetValue(new(component));

        Assert.Equal(1, value);
        Assert.Equal(1, component.LastValue);

        value = component.GetValue(new(component));

        Assert.Equal(2, value);
        Assert.Equal(2, component.LastValue);
    }

    [Fact]
    public void Owner_throws_when_set_to_null()
    {
        var component = new IncrementingTestSnowflakeComponent<long>(lengthInBits: 10);

        Assert.Throws<ArgumentNullException>("value", () => component.Owner = null);
    }

    [Fact]
    public void Owner_throws_when_set_to_different_non_null_generator()
    {
        var gen1 = new SnowflakeGeneratorBuilder<long>().AddConstant(1, 1).Build();
        var gen2 = new SnowflakeGeneratorBuilder<long>().AddConstant(1, 1).Build();

        var component = new IncrementingTestSnowflakeComponent<long>(lengthInBits: 10);
        Assert.Null(component.Owner);

        component.Owner = gen1;
        Assert.Same(gen1, component.Owner);

        component.Owner = gen1;
        Assert.Same(gen1, component.Owner);

        Assert.Throws<InvalidOperationException>(() => component.Owner = gen2);
    }

    public sealed class IncrementingTestSnowflakeComponent<T> : SnowflakeComponent<T>
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        private T _value;

        public IncrementingTestSnowflakeComponent(
            int lengthInBits, T startValue = default, bool allowTruncation = false)
            : base(lengthInBits)
        {
            AllowTruncation = allowTruncation;

            _value = startValue;
        }

        public override T CalculateValue(SnowflakeGenerationContext<T> ctx) => _value++;
    }
}
