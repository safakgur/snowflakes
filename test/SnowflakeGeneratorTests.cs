using NSubstitute;
using Snowflakes.Components;

namespace Snowflakes.Tests;

public sealed class SnowflakeGeneratorTests
{
    [Fact]
    public void Ctor_validates_components()
    {
        var component53Bit = Substitute.For<SnowflakeComponent>(53);
        var component10Bit = Substitute.For<SnowflakeComponent>(10);
        var component1Bit = Substitute.For<SnowflakeComponent>(1);

        // Null
        Assert.Throws<ArgumentNullException>("components", () => new SnowflakeGenerator(null!));

        // Empty
        Assert.Throws<ArgumentException>("components", () => new SnowflakeGenerator([]));

        // Null item
        Assert.Throws<ArgumentException>("components", () => new SnowflakeGenerator([null!]));

        // Dupe item
        Assert.Throws<ArgumentException>("components", () =>
            new SnowflakeGenerator([component1Bit, component1Bit]));

        // Length > 63-bit
        Assert.Throws<ArgumentException>("components", () =>
            new SnowflakeGenerator([component53Bit, component10Bit, component1Bit]));

        // Success
        var gen = new SnowflakeGenerator([component53Bit, component10Bit]);

        Assert.Equal([component53Bit, component10Bit], gen.Components.ToArray());
    }

    [Fact]
    public async Task NewSnowflake_synchronizes_access()
    {
        var timeProvider = TimeProvider.System;

        var countingComponent = new CountingTestSnowflakeComponent();
        var blockingComponent = new BlockingTestSnowflakeComponent();
        var gen = new SnowflakeGenerator([countingComponent, blockingComponent]);

        // Get approx. duration for a single run.
        var ts = timeProvider.GetTimestamp();
        blockingComponent.AllowOne();
        await Task.Run(gen.NewSnowflake);
        var safeDuration = timeProvider.GetElapsedTime(ts) * 4;
        countingComponent.ResetCount();

        // Start both runs.
        var tasks = new[]
        {
            Task.Run(gen.NewSnowflake),
            Task.Run(gen.NewSnowflake)
        };

        // Wait enough time for both runs to complete execute until the blocking component.
        await Task.Delay(safeDuration);

        // We started two tasks and the counting component gets executed before the blocking
        // component, so the count being 1 instead of 2 would prove that the generator also
        // has locking to ensure snowflakes are generated sequentially.
        Assert.Equal(1, countingComponent.Count);

        // Let go of both threads.
        blockingComponent.AllowOne();
        blockingComponent.AllowOne();
        await Task.WhenAll(tasks);

        // Now we have 2
        Assert.Equal(2, countingComponent.Count);
    }

    [Fact]
    public void NewSnowflake_executes_components_with_default_ExecutionOrder_in_correct_order()
    {
        LastExecutionTestSnowflakeComponent c1 = new(), c2 = new();

        var gen = new SnowflakeGenerator([c1, c2]);

        gen.NewSnowflake();

        Assert.True(c1.LastExecutionTimestamp < c2.LastExecutionTimestamp);
    }

    [Fact]
    public void NewSnowflake_executes_components_with_custom_ExecutionOrder_in_correct_order()
    {
        LastExecutionTestSnowflakeComponent c1 = new() { ExecutionOrder = 1 }, c2 = new();

        var gen = new SnowflakeGenerator([c1, c2]);

        gen.NewSnowflake();

        Assert.True(c1.LastExecutionTimestamp > c2.LastExecutionTimestamp);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-2, -1, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(-1, -2, 1)]
    [InlineData(0, -1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 1, 1)]
    public void NewSnowflake_executes_components_in_correct_order(
        int component1ExecutioOrder,
        int component2ExecutioOrder,
        int expectedFirstExecutedComponentIndex)
    {
        LastExecutionTestSnowflakeComponent[] components = [
            new() { ExecutionOrder = component1ExecutioOrder },
            new() { ExecutionOrder = component2ExecutioOrder }
        ];

        var gen = new SnowflakeGenerator(components);

        gen.NewSnowflake();

        var expectedFirstTs = components[expectedFirstExecutedComponentIndex].LastExecutionTimestamp;
        var expectedLastTs = components[expectedFirstExecutedComponentIndex == 0 ? 1 : 0].LastExecutionTimestamp;

        Assert.True(expectedFirstTs < expectedLastTs);
    }

    [Fact]
    public void NewSnowflake_produces_correct_value_Wikipedia_example()
    {
        // This tests uses the example on https://en.wikipedia.org/wiki/Snowflake_ID
        // about the tweet (or whatever X calls it these days) 1541815603606036480.

        // Twitter's (now X) snowflake epoch
        var xEpoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657);

        // The time Wikipedia tweeted
        var tweetTime = new DateTimeOffset(2022, 6, 28, 16, 7, 40, 105, TimeSpan.Zero);

        var testTimeProvider = Substitute.For<TimeProvider>();
        testTimeProvider.GetUtcNow().Returns(tweetTime);

        var gen = new SnowflakeGenerator([
            new TimestampSnowflakeComponent(41, xEpoch, TimeSpan.TicksPerMillisecond, testTimeProvider),
            new ConstantSnowflakeComponent(10, 0b_01_0111_1010L),
            new SequenceSnowflakeComponent(12, refComponentIndex: 0)]);

        // Expected tweet ID as it was the first tweet processed by the machine at that millisecond
        Assert.Equal(1541815603606036480L, gen.NewSnowflake());

        // Expected tweet ID if it were the second tweet processed by the machine at that millisecond
        Assert.Equal(1541815603606036481L, gen.NewSnowflake());
    }

    public sealed class CountingTestSnowflakeComponent()
        : SnowflakeComponent(lengthInBits: 1)
    {
        public int Count { get; private set; }

        protected override long CalculateValue(SnowflakeGenerationContext ctx)
        {
            Count++;

            return 1L;
        }

        public void ResetCount() => Count = 0;
    }

    public sealed class BlockingTestSnowflakeComponent()
        : SnowflakeComponent(lengthInBits: 1)
    {
        private readonly AutoResetEvent _event = new(initialState: false);

        public void AllowOne() => _event.Set();

        protected override long CalculateValue(SnowflakeGenerationContext ctx)
        {
            _event.WaitOne();

            return 1L;
        }
    }

    public sealed class LastExecutionTestSnowflakeComponent()
        : SnowflakeComponent(lengthInBits: 1)
    {
        public long LastExecutionTimestamp { get; private set; }

        protected override long CalculateValue(SnowflakeGenerationContext ctx)
        {
            var timeProvider = TimeProvider.System;

            LastExecutionTimestamp = timeProvider.GetTimestamp();

            // Wait until calling GetTimestamp again would produce a different value.
            Thread.Sleep(TimeSpan.FromSeconds(1.0 / timeProvider.TimestampFrequency));

            return 1L;
        }
    }
}
