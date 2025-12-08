using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public sealed class SnowflakeComponentTests
{
    public static TheoryData<int, bool> LengthInBits_IsValid_Data { get; } = new()
    {
        { -1, false },
        { 0, false },
        { 1, true },
        { 63, true },
        { 64, false },
        { 0, false },
        { 1, true  },
        { 63, true },
        { 64, false }
    };

    [Theory]
    [MemberData(nameof(LengthInBits_IsValid_Data))]
    public void Ctor_validates_and_sets_lengthInBits(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new IncrementingTestSnowflakeComponent(lengthInBits);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new IncrementingTestSnowflakeComponent(lengthInBits));
        }
    }

    [Fact]
    public void GetValue_validates_ctx()
    {
        var component = new IncrementingTestSnowflakeComponent(lengthInBits: 10);

        Assert.Throws<ArgumentNullException>("ctx", () => component.GetValue(null!));
    }

    [Theory]
    [InlineData(4, 0b_1101, 0b_1101)]
    [InlineData(3, 0b_1101, 0b_0101)]
    [InlineData(2, 0b_1011, 0b_0011)]
    [InlineData(1, 0b_1011, 0b_0001)]
    public void GetValue_masks_value(int lengthInBits, long originalValue, long maskedValue)
    {
        var component = new IncrementingTestSnowflakeComponent(
            lengthInBits, originalValue, allowTruncation: true);

        var value = component.GetValue(new(component));

        Assert.Equal(maskedValue, value);
    }

    [Fact]
    public void GetValue_throws_when_calculated_value_is_out_of_range()
    {
        var component = new IncrementingTestSnowflakeComponent(
            4, 0b_1110, allowTruncation: false);

        var ctx = new SnowflakeGenerationContext<long>(component);

        Assert.Equal(0b_1110, component.GetValue(ctx));
        Assert.Equal(0b_1111, component.GetValue(ctx));
        Assert.Throws<OverflowException>(() => component.GetValue(ctx));
    }

    [Fact]
    public void GetValue_saves_value_in_LastValue()
    {
        var component = new IncrementingTestSnowflakeComponent(lengthInBits: 10, startValue: 1);

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
        var component = new IncrementingTestSnowflakeComponent(lengthInBits: 10);

        Assert.Throws<ArgumentNullException>("value", () => component.Owner = null);
    }

    [Fact]
    public void Owner_throws_when_set_to_different_non_null_generator()
    {
        var gen1 = new SnowflakeGeneratorBuilder<long>().AddConstant(1, 1).Build();
        var gen2 = new SnowflakeGeneratorBuilder<long>().AddConstant(1, 1).Build();

        var component = new IncrementingTestSnowflakeComponent(lengthInBits: 10);
        Assert.Null(component.Owner);

        component.Owner = gen1;
        Assert.Same(gen1, component.Owner);

        component.Owner = gen1;
        Assert.Same(gen1, component.Owner);

        Assert.Throws<InvalidOperationException>(() => component.Owner = gen2);
    }

    private sealed class IncrementingTestSnowflakeComponent : SnowflakeComponent<long>
    {
        private long _startValue;

        public IncrementingTestSnowflakeComponent(
            int lengthInBits, long startValue = 0L, bool allowTruncation = false)
            : base(lengthInBits)
        {
            AllowTruncation = allowTruncation;

            _startValue = startValue;
        }

        protected override long CalculateValue(SnowflakeGenerationContext<long> ctx) => _startValue++;
    }

}
