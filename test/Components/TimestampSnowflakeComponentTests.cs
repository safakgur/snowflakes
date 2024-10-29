using NSubstitute;
using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public class TimestampSnowflakeComponentTests
{
    [Theory]
    [MemberData(nameof(SnowflakeComponentTests.LengthInBits_IsValid_Data), MemberType = typeof(SnowflakeComponentTests))]
    public void Ctor_validates_lengthInBits(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = Construct(lengthInBits, epoch: default);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                Construct(lengthInBits, epoch: default));
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void Ctor_validates_epoch(long ticksSinceEpoch, bool isValid)
    {
        var epoch = new DateTimeOffset(2024, 8, 29, 22, 53, 00, TimeSpan.Zero);
        var now = epoch.AddTicks(ticksSinceEpoch);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(now);

        if (isValid)
        {
            var component = Construct(lengthInBits: 11, epoch, timeProvider: testTimeProvider);
            Assert.Equal(epoch, component.Epoch);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(epoch), () =>
                Construct(lengthInBits: 11, epoch, timeProvider: testTimeProvider));
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Ctor_validates_ticksPerUnit(long ticksPerUnit, bool isValid)
    {
        if (isValid)
        {
            var component = Construct(lengthInBits: 10, epoch: default, ticksPerUnit);
            Assert.Equal(ticksPerUnit, component.TicksPerUnit);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(ticksPerUnit), () =>
                Construct(lengthInBits: 10, epoch: default, ticksPerUnit));
        }
    }

    [Fact]
    public void Ctor_sets_TimeProvider_correctly()
    {
        var component = Construct(lengthInBits: 10, epoch: default);
        Assert.Same(TimeProvider.System, component.TimeProvider);

        var testTimeProvider = Substitute.For<TimeProvider>();
        component = Construct(
            lengthInBits: 10, epoch: default, timeProvider: testTimeProvider);

        Assert.Same(testTimeProvider, component.TimeProvider);
    }

    [Theory]
    [InlineData("13:10:00", "13:10:02", TimeSpan.TicksPerSecond, 2)]
    [InlineData("13:10:00", "13:10:02", TimeSpan.TicksPerMillisecond, 2000)]
    public void GetValue_returns_correctly_calculated_timestamp(
        string epochTimeOfDayString,
        string nowTimeOfDayString,
        long ticksPerUnit,
        long expectedValue)
    {
        var epoch = GetTime(epochTimeOfDayString);
        var now = GetTime(nowTimeOfDayString);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(now);

        var component = Construct(lengthInBits: 11, epoch, ticksPerUnit, testTimeProvider);
        var value = component.GetValue(new([component]));

        Assert.Equal(expectedValue, value);

        static DateTimeOffset GetTime(string timeOfDayString) =>
            DateTimeOffset.Parse($"2024-08-29T{timeOfDayString}+00:00");
    }

    [Fact]
    public void GetValue_throws_when_calculated_value_is_out_of_range()
    {
        var epoch = new DateTimeOffset(2024, 10, 27, 21, 58, 00, TimeSpan.Zero);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(epoch.AddSeconds(1));

        var component = Construct(lengthInBits: 2, epoch, TimeSpan.TicksPerSecond, testTimeProvider);
        var ctx = new SnowflakeGenerationContext([component]);

        testTimeProvider.GetUtcNow().Returns(epoch.AddSeconds(3));
        _ = component.GetValue(ctx);

        testTimeProvider.GetUtcNow().Returns(epoch.AddSeconds(4));
        Assert.Throws<OverflowException>(() => component.GetValue(ctx));
    }

    protected virtual TimestampSnowflakeComponent Construct(
        int lengthInBits,
        DateTimeOffset epoch,
        long ticksPerUnit = TimeSpan.TicksPerMillisecond,
        TimeProvider? timeProvider = null) =>
        new(lengthInBits, epoch, ticksPerUnit, timeProvider);
}
