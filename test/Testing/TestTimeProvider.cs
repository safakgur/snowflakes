namespace Snowflakes.Tests.Testing;

internal sealed class TestTimeProvider : TimeProvider
{
    private static readonly DateTimeOffset s_now = DateTimeOffset.UtcNow;

    private TestTimeProvider() { }

    public static TimeProvider Instance { get; } = new TestTimeProvider();

    public override DateTimeOffset GetUtcNow() => s_now;
}
