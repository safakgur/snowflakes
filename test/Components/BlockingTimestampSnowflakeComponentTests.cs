using System.Diagnostics;
using NSubstitute;
using Snowflakes.Components;

namespace Snowflakes.Tests.Components;

public sealed class BlockingTimestampSnowflakeComponentTests : TimestampSnowflakeComponentTests
{
    [Fact]
    public void GetValue_blocks_and_retries_when_it_produces_same_timestamp()
    {
        var epoch = new DateTimeOffset(2024, 10, 28, 19, 12, 00, TimeSpan.Zero);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(epoch.AddSeconds(1));

        var component = Construct(lengthInBits: 10, epoch, TimeSpan.TicksPerSecond, testTimeProvider);
        var ctx = new SnowflakeGenerationContext<long>(component);

        // First call will never block because it will produce a new timestamp (null -> 1)
        var watchStart = Stopwatch.GetTimestamp();
        component.GetValue(ctx);
        var watchElapsed = Stopwatch.GetElapsedTime(watchStart);
        Assert.True(watchElapsed.TotalSeconds < 1.0);

        // So, we have a delay where we wait for a few seconds and change the current time (1 -> 2)
        using var timer = new Timer(
            callback: _ => testTimeProvider.GetUtcNow().Returns(epoch.AddSeconds(2)),
            state: null,
            dueTime: TimeSpan.FromSeconds(2.5),
            period: Timeout.InfiniteTimeSpan);

        // Second call will block because it will produce the same timestamp (1 -> 1)
        // That is until the timer kicks in and changes the current time (1 -> 2)
        watchStart = Stopwatch.GetTimestamp();
        component.GetValue(ctx);
        watchElapsed = Stopwatch.GetElapsedTime(watchStart);
        Assert.True(watchElapsed.TotalSeconds > 1.0);

        // Timestamp should change after a few iterations.
        // We throw if not, so GetValue doesn't wait forever due to a problematic time provider.
        Assert.Throws<InvalidOperationException>(() => component.GetValue(ctx));
    }

    protected override TimestampSnowflakeComponent<long> Construct(
        int lengthInBits,
        DateTimeOffset epoch,
        long ticksPerUnit = TimeSpan.TicksPerMillisecond,
        TimeProvider? timeProvider = null) =>
        new BlockingTimestampSnowflakeComponent<long>(
            lengthInBits, epoch, ticksPerUnit, timeProvider);
}
