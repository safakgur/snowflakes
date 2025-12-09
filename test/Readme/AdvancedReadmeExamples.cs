using NSubstitute;
using Snowflakes.Components;
using Snowflakes.Tests.Testing;

namespace Snowflakes.Tests.Readme;

public sealed class AdvancedReadmeExamples : BaseReadmeExamples
{
    [Fact]
    public void Custom_size_snowflakes()
    {
        // CONTENT-START

        // No generic type argument - defaults to a builder to 64-bit signed snowflakes.
        SnowflakeGenerator.CreateBuilder();

        // Same as above, but the type is specified explicitly.
        SnowflakeGenerator.CreateBuilder<long>();

        // Same size, but unsigned, so we get to use one extra bit.
        SnowflakeGenerator.CreateBuilder<ulong>();

        // Builders to generate 32-bit signed and unsigned snowflakes - half the default size.
        SnowflakeGenerator.CreateBuilder<int>();
        SnowflakeGenerator.CreateBuilder<uint>();

        // Builders to generate 128-bit signed and unsigned snowflakes - double the default size.
        SnowflakeGenerator.CreateBuilder<Int128>();
        SnowflakeGenerator.CreateBuilder<UInt128>();

        // + sbyte, byte, short, ushort, and any other binary integer implementation.

        // CONTENT-END
    }

    [Fact]
    public void Custom_size_snowflake_encoding()
    {
        var encodedSnowflake = "0Ab";

        // CONTENT-START

        // No generic type argument - defaults to decoding into a 64-bit signed integer.
        SnowflakeEncoder.Base62Ordinal.Decode(encodedSnowflake);

        // Same as above, but the type is specified explicitly.
        SnowflakeEncoder.Base62Ordinal.Decode<long>(encodedSnowflake);

        // CONTENT-END
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
        var timeProvider = TestTimeProvider.Frozen;
        var testEpoch = timeProvider.GetUtcNow().AddDays(-10);
        var testSnowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(32, testEpoch, timeProvider: timeProvider)
            .Add(testComponent)
            .Build();

        // CONTENT-END

        _ = testSnowflakeGen.NewSnowflake();
    }
}
