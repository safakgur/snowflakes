using System.Diagnostics;

namespace Snowflakes.Components;

/// <summary>
///     Provides information about snowflake generation and the <see cref="SnowflakeGenerator" />
///     instance executing it.
/// </summary>
public sealed class SnowflakeGenerationContext
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly SnowflakeComponent[] _components;

    internal SnowflakeGenerationContext(SnowflakeComponent[] components) => _components = components;

    /// <summary>
    ///     Gets all the configured components that are getting used to
    ///     generate the current snowflake.
    /// </summary>
    public ReadOnlySpan<SnowflakeComponent> Components => _components;
}
