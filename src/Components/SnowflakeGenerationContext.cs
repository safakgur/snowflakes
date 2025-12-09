using System.Diagnostics;
using System.Numerics;

namespace Snowflakes.Components;

/// <summary>
///     Provides information about snowflake generation and the <see cref="SnowflakeGenerator{T}" />
///     instance executing it.
/// </summary>
/// <typeparam name="T">The snowflake type.</typeparam>
public sealed class SnowflakeGenerationContext<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly SnowflakeComponent<T>[] _components;

    internal SnowflakeGenerationContext(params SnowflakeComponent<T>[] components) => _components = components;

    /// <summary>
    ///     Gets all the configured components that are getting used to
    ///     generate the current snowflake.
    /// </summary>
    public ReadOnlySpan<SnowflakeComponent<T>> Components => _components;
}
