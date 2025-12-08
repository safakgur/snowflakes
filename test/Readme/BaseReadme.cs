using Snowflakes.Tests.Testing;

namespace Snowflakes.Tests.Readme;

public abstract class BaseReadme
{
    protected static readonly SnowflakeGenerator<long> TestSnowflakeGen =
        SnowflakeGenerator.CreateBuilder().AddConstant(1, 1).Build();

    protected static readonly DateTimeOffset TestEpoch =
        TestTimeProvider.Frozen.GetUtcNow().AddDays(-10);
}
