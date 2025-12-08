namespace Snowflakes.Tests.Readme;

public sealed class ReadmeBasics : BaseReadme
{
    [Fact]
    public void X_implementation()
    {
        // CONTENT-START

        // X's epoch - 2010-11-04T01:42:54.657Z
        var epoch = DateTimeOffset.FromUnixTimeMilliseconds(1288834974657);

        // Set the instance ID, e.g., the ordinal index of a K8 pod.
        var instanceId = 0;

        // Create the generator.
        var snowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(41, epoch, TimeSpan.TicksPerMillisecond)
            .AddConstant(10, instanceId)
            .AddSequenceForTimestamp(12)
            .Build();

        // CONTENT-END

        _ = snowflakeGen;
    }

    [Fact]
    public void Sony_implementation()
    {
        // CONTENT-START

        // Choose an epoch, e.g., when your system came online. Epoch can't be in the future.
        var epoch = new DateTimeOffset(2024, 8, 30, 0, 0, 0, TimeSpan.Zero);

        // Set the instance ID, e.g., the ordinal index of a K8 pod.
        var instanceId = 0;

        // Create the generator.
        var snowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(39, epoch, TimeSpan.TicksPerMillisecond * 10) // 10 ms increments
            .AddSequenceForTimestamp(8)
            .AddConstant(16, instanceId)
            .Build();

        // CONTENT-END

        _ = snowflakeGen;
    }

    [Fact]
    public void Generating_snowflakes()
    {
        var snowflakeGen = TestSnowflakeGen;

        // CONTENT-START

        var snowflake1 = snowflakeGen.NewSnowflake();
        var snowflake2 = snowflakeGen.NewSnowflake();

        // CONTENT-END

        _ = (snowflake1, snowflake2);
    }

    [Fact]
    public void Encoding_snowflakes()
    {
        var snowflakeGen = TestSnowflakeGen;

        // CONTENT-START

        // There are base 36, 62, and 64 encoders, all URI-safe.
        var encoder = SnowflakeEncoder.Base62Ordinal;

        var snowflake = snowflakeGen.NewSnowflake(); // 139611368062976
        var encodedSnowflake = encoder.Encode(snowflake); // "ddw3cbIG"
        var decodedSnowflake = encoder.Decode(encodedSnowflake); // 139611368062976

        // CONTENT-END

        _ = decodedSnowflake;
    }
}
