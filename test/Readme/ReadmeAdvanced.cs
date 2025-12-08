using NSubstitute;
using Snowflakes.Components;
using Snowflakes.Tests.Testing;

namespace Snowflakes.Tests.Readme;

public sealed class ReadmeAdvanced : BaseReadme
{
    [Fact]
    public void Custom_size_snowflakes()
    {
        // CONTENT-START

        // No generic type argument - defaults to a builder to generate 64-bit signed snowflakes.
        var snowflakeGenBuilder64 = SnowflakeGenerator.CreateBuilder();

        // Same as above, but the type is specified explicitly.
        var snowflakeGenBuilder64Explicit = SnowflakeGenerator.CreateBuilder<long>();

        // Same size, but unsigned, so we get to use one extra bit.
        var snowflakeGenBuilderU64 = SnowflakeGenerator.CreateBuilder<ulong>();

        // Builders to generate 32-bit signed and unsigned snowflakes - half the default size.
        var snowflakeGenBuilder32 = SnowflakeGenerator.CreateBuilder<int>();
        var snowflakeGenBuilderU32 = SnowflakeGenerator.CreateBuilder<uint>();

        // Builders to generate 128-bit signed and unsigned snowflakes - double the default size, as big as UUIDs.
        var snowflakeGenBuilder128 = SnowflakeGenerator.CreateBuilder<Int128>();
        var snowflakeGenBuilderU128 = SnowflakeGenerator.CreateBuilder<UInt128>();

        // + sbyte, byte, short, ushort, and any other binary integer implementation you can find...

        // CONTENT-END

        _ = (snowflakeGenBuilder64,
             snowflakeGenBuilder64Explicit,
             snowflakeGenBuilderU64,
             snowflakeGenBuilder32,
             snowflakeGenBuilderU32,
             snowflakeGenBuilder128,
             snowflakeGenBuilderU128);
    }

    [Fact]
    public void Blocking_timestamp_generation()
    {
        var epoch = TestEpoch;
        var instanceId = 0;

        // CONTENT-START

        var snowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddBlockingTimestamp(44, epoch, TimeSpan.TicksPerMillisecond / 2)
            .AddConstant(19, instanceId)
            .Build();

        // CONTENT-END

        _ = snowflakeGen;
    }

    [Fact]
    public void Testing_snowflake_generation_with_constant_fake()
    {
        // CONTENT-START

        var testSnowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddConstant(63, 123L)
            .Build();

        // `testSnowflakeGen.NewSnowflake()` will always return 123.

        // CONTENT-END

        Assert.Equal(123L, testSnowflakeGen.NewSnowflake());
    }

    [Fact]
    public void Testing_snowflake_generation_with_mocked_component()
    {
        // CONTENT-START

        var random = new Random();

        // NSubstitute example
        var testComponent = Substitute.For<SnowflakeComponent<long>>(31);
        testComponent
            .CalculateValue(Arg.Any<SnowflakeGenerationContext<long>>())
            .Returns(call =>
            {
                // Context can be used to access other components of the generator.
                // var ctx = call.Arg<SnowflakeGenerationContext<long>>();
                // var timestampLastValue = ctx.Components[0].LastValue;

                // Return any value for tests.
                return random.Next();
            });

        // Assuming we have a test time provider
        var testEpoch = TestTimeProvider.Instance.GetUtcNow().AddDays(-10);
        var testSnowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(32, testEpoch)
            .Add(testComponent)
            .Build();

        // CONTENT-END

        _ = testSnowflakeGen.NewSnowflake();
    }
}
