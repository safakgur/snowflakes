using System.Numerics;
using System.Security.Cryptography;
using Snowflakes.Components;
using static Snowflakes.Tests.Readme.ReadmeCustomComponent;

namespace Snowflakes.Tests.Readme;

public sealed partial class ReadmeCustomComponent : BaseReadme
{
    // Custom_components_component
    // CONTENT-START

    public sealed class RandomSnowflakeComponent<T> : SnowflakeComponent<T>
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        public RandomSnowflakeComponent(int lengthInBits) : base(lengthInBits)
        {
            AllowTruncation = true;
        }

        public override T CalculateValue(SnowflakeGenerationContext<T> ctx)
        {
            Span<byte> buffer = stackalloc byte[MaxLengthInBytes];
            while (true)
            {
                RandomNumberGenerator.Fill(buffer);
                if (T.TryReadLittleEndian(buffer, IsUnsigned, out var value))
                    return value;
            }
        }
    }

    // CONTENT-END

    [Fact]
    public void Custom_component_usage()
    {
        var epoch = TestEpoch;

        // CONTENT-START

        var snowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(30, epoch)
            .Add(new RandomSnowflakeComponent<long>(33)) // Here we add our custom component
            .Build();

        // High 30 bits have milliseconds elapsed since `epoch` while low 33 bits are random.
        // Similar to a version 7 UUID, albeit smaller.
        var snowflake = snowflakeGen.NewSnowflake();

        // CONTENT-END

        _ = snowflake;
    }
}

// Custom_component_helper
// CONTENT-START

public static class SnowflakeGeneratorBuilderExtensions
{
    public static SnowflakeGeneratorBuilder<T> AddRandom<T>(
        this SnowflakeGeneratorBuilder<T> builder, int lengthInBits)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Add(new RandomSnowflakeComponent<T>(lengthInBits));
    }
}

// CONTENT-END

public sealed partial class ReadmeCustomComponent : BaseReadme
{
    [Fact]
    public void Custom_component_extension()
    {
        var epoch = TestEpoch;

        // CONTENT-START

        var snowflakeGen = SnowflakeGenerator.CreateBuilder()
            .AddTimestamp(30, epoch)
            .AddRandom(33) // Extension method
            .Build();

        // CONTENT-END

        _ = snowflakeGen;
    }
}
