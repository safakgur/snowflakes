using System.Numerics;

namespace Snowflakes.Components;

/// <summary>Produces a timestamp to be placed in a snowflake.</summary>
/// <typeparam name="T">The snowflake type.</typeparam>
public class TimestampSnowflakeComponent<T> : SnowflakeComponent<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TimestampSnowflakeComponent{T}" /> class.
    /// </summary>
    /// <param name="lengthInBits">
    ///     <inheritdoc cref="SnowflakeComponent{T}.SnowflakeComponent" path="/param[@name='lengthInBits']" />
    /// </param>
    /// <param name="epoch">
    ///     <para>The start of time for the produced timestamps.</para>
    ///     <para>
    ///         For example, if the epoch is 2024-08-26T00:00:00Z, the timestamp precision is in
    ///         seconds, and a timestamp is produced at 2024-08-26T00:00:02Z, the value of the
    ///         timestamp is 2 as it has been 2 seconds since the epoch.
    ///     </para>
    /// </param>
    /// <param name="ticksPerUnit">
    ///     <para>
    ///         The number of ticks per unit of time. This determines the precision of the
    ///         timestamp.
    ///     </para>
    ///     <para>
    ///         For example, if this value is set to <see cref="TimeSpan.TicksPerMillisecond" />,
    ///         which is the default, produced timestamps will have millisecond precision.
    ///     </para>
    /// </param>
    /// <param name="timeProvider">
    ///     <para>
    ///         An optional provider for retrieving the current time.
    ///         If not specified, <see cref="TimeProvider.System" /> time provider will be used.
    ///     </para>
    ///     <para>Supplying this can be useful for testing or for using a custom time source.</para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <inheritdoc cref="SnowflakeComponent{T}.SnowflakeComponent" path="/exception[@cref='ArgumentOutOfRangeException']"/>
    ///     -or-
    ///     <paramref name="epoch" /> is after the current time.
    ///     -or-
    ///     <paramref name="ticksPerUnit"/> is less than one.
    /// </exception>
    public TimestampSnowflakeComponent(
        int lengthInBits,
        DateTimeOffset epoch,
        long ticksPerUnit = TimeSpan.TicksPerMillisecond,
        TimeProvider? timeProvider = null)
        : base(lengthInBits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ticksPerUnit, 1);

        TimeProvider = timeProvider ?? TimeProvider.System;

        if (epoch > TimeProvider.GetUtcNow())
            throw new ArgumentOutOfRangeException(
                nameof(epoch), epoch, "Epoch must be before the current time.");


        Epoch = epoch;
        TicksPerUnit = ticksPerUnit;
    }

    /// <summary>Gets the object that provides the current time for producing timestamps.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets the start of time for the produced timestamps.</summary>
    public DateTimeOffset Epoch { get; }

    /// <summary>
    ///     Gets the number of ticks per unit of time.
    ///     This determines the precision of the timestamp
    /// </summary>
    public double TicksPerUnit { get; }

    /// <inheritdoc />
    protected override T CalculateValue(SnowflakeGenerationContext<T> ctx)
    {
        var ticksSinceEpoch = TimeProvider.GetUtcNow().Ticks - Epoch.UtcTicks;

        return T.CreateChecked(ticksSinceEpoch / TicksPerUnit); // TODO: REVISE
    }
}
