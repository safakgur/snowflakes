namespace Snowflakes.Tests.Testing;

internal sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public static TimeProvider Frozen { get; } = new TestTimeProvider(DateTimeOffset.UtcNow);

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void SetUtcNow(DateTimeOffset now) => _utcNow = now;
}
