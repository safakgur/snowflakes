using System.Numerics;

namespace Snowflakes.Components;

/// <summary>Provides a fixed value to be placed in a snowflake.</summary>
/// <typeparam name="T">The snowflake type.</typeparam>
public sealed class ConstantSnowflakeComponent<T> : SnowflakeComponent<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConstantSnowflakeComponent{T}" /> class
    ///     that provides the specified value.
    /// </summary>
    /// <param name="lengthInBits">
    ///     <inheritdoc cref="SnowflakeComponent{T}.SnowflakeComponent" path="/param[@name='lengthInBits']" />
    /// </param>
    /// <param name="value">The value that will be provided by this component.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <inheritdoc cref="SnowflakeComponent{T}.SnowflakeComponent" path="/exception[@cref='ArgumentOutOfRangeException']"/>
    ///     -or-
    ///     <paramref name="value"/> is negative.
    /// </exception>
    public ConstantSnowflakeComponent(int lengthInBits, T value)
        : base(lengthInBits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);

        AllowTruncation = true;
        Value = value;
    }

    /// <summary>Gets the value that will be provided by this component.</summary>
    /// <remarks>
    ///     Note that <see cref="SnowflakeComponent{T}.GetValue" /> masks this value to get only the
    ///     lowest <see cref="SnowflakeComponent{T}.LengthInBits" /> number of bits. Any higher bits,
    ///     i.e., bits to the left, are discarded.
    /// </remarks>
    public T Value { get; }

    /// <inheritdoc />
    public override T CalculateValue(SnowflakeGenerationContext<T> ctx) => Value;
}
