using System.Diagnostics;

namespace Snowflakes.Components;

/// <summary>Produces a timestamp to be placed in a snowflake.</summary>
/// <remarks>
///     When a new snowflake is requested within the same unit of time, this component will block
///     the current thread until the next unit of time is reached.
/// </remarks>
/// <inheritdoc cref="TimestampSnowflakeComponent.TimestampSnowflakeComponent" />
public sealed class BlockingTimestampSnowflakeComponent(
    int lengthInBits,
    DateTimeOffset epoch,
    long ticksPerUnit = TimeSpan.TicksPerMillisecond,
    TimeProvider? timeProvider = null)
    : TimestampSnowflakeComponent(lengthInBits, epoch, ticksPerUnit, timeProvider)
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly TimeSpan _unitDuration = TimeSpan.FromTicks(ticksPerUnit);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private long? _lastValue;

    /// <inheritdoc />
    protected override long CalculateValue(SnowflakeGenerationContext ctx)
    {
        for (var i = 0; true; i++)
        {
            var result = base.CalculateValue(ctx);
            if (result != _lastValue)
            {
                _lastValue = result;
                return result;
            }

            if (i == 3)
                throw new InvalidOperationException("The time provider is producing the same value.");

            Thread.Sleep(_unitDuration);
        }
    }
}
