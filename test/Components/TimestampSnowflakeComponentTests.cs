using NSubstitute;
using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public sealed class TimestampSnowflakeComponentTests
{
    [Theory]
    [MemberData(nameof(SnowflakeComponentTests.LengthInBits_IsValid_Data), MemberType = typeof(SnowflakeComponentTests))]
    public void Ctor_validates_lengthInBits(int lengthInBits, bool isValid)
    {
        if (isValid)
        {
            var component = new TimestampSnowflakeComponent(lengthInBits, epoch: default);
            Assert.Equal(lengthInBits, component.LengthInBits);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(lengthInBits), () =>
                new TimestampSnowflakeComponent(lengthInBits, epoch: default));
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
            var component = new TimestampSnowflakeComponent(
                lengthInBits: 11, epoch, timeProvider: testTimeProvider);

            Assert.Equal(epoch, component.Epoch);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(epoch), () =>
                new TimestampSnowflakeComponent(
                    lengthInBits: 11, epoch, timeProvider: testTimeProvider));
        }
    }

    [Theory]
    [InlineData(0.9, false)]
    [InlineData(1.0, true)]
    public void Ctor_validates_ticksPerUnit(double ticksPerUnit, bool isValid)
    {
        if (isValid)
        {
            var component = new TimestampSnowflakeComponent(lengthInBits: 10, epoch: default, ticksPerUnit);
            Assert.Equal(ticksPerUnit, component.TicksPerUnit);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(ticksPerUnit), () =>
                new TimestampSnowflakeComponent(lengthInBits: 10, epoch: default, ticksPerUnit));
        }
    }

    [Fact]
    public void Ctor_sets_TimeProvider_correctly()
    {
        var component = new TimestampSnowflakeComponent(lengthInBits: 10, epoch: default);
        Assert.Same(TimeProvider.System, component.TimeProvider);

        var testTimeProvider = Substitute.For<TimeProvider>();
        component = new TimestampSnowflakeComponent(
            lengthInBits: 10, epoch: default, timeProvider: testTimeProvider);

        Assert.Same(testTimeProvider, component.TimeProvider);
    }

    [Theory]
    [InlineData("13:10:00", "13:10:02", TimeSpan.TicksPerSecond, 2)]
    [InlineData("13:10:00", "13:10:02", TimeSpan.TicksPerMillisecond, 2000)]
    public void GetValue_returns_correctly_calculated_timestamp(
        string epochTimeOfDayString,
        string nowTimeOfDayString,
        double ticksPerUnit,
        long expectedValue)
    {
        var epoch = GetTime(epochTimeOfDayString);
        var now = GetTime(nowTimeOfDayString);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(now);

        var component = new TimestampSnowflakeComponent(
            lengthInBits: 11, epoch, ticksPerUnit, testTimeProvider);

        var value = component.GetValue(new([component]));

        Assert.Equal(expectedValue, value);

        static DateTimeOffset GetTime(string timeOfDayString) =>
            DateTimeOffset.Parse($"2024-08-29T{timeOfDayString}+00:00");
    }
}
