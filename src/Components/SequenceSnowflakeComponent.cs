namespace Snowflakes.Components;

/// <summary>
///     <para>Provides a sequence number to be placed in a snowflake.</para>
///     <para>
///         It checks the value produced by a referenced component to determine if it has changed
///         since the last call. If the referenced component's value is the same, the sequence
///         number is incremented; otherwise, the sequence number is reset to 0.
///     </para>
/// </summary>
public sealed class SequenceSnowflakeComponent : SnowflakeComponent
{
    private long? _refComponentValue;
    private long _seq = -1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceSnowflakeComponent" /> class.
    /// </summary>
    /// <param name="lengthInBits">
    ///     <inheritdoc cref="SnowflakeComponent.SnowflakeComponent" path="/param[@name='lengthInBits']" />
    /// </param>
    /// <param name="refComponentIndex">
    ///     <para>
    ///         The zero-based index of the component added to <see cref="SnowflakeGenerator" />
    ///         that this component will check the <see cref="SnowflakeComponent.LastValue" />
    ///         property of.
    ///     </para>
    ///     <para>
    ///         The sequence produced provided starts from 0 and is incremented every time until
    ///         the value of the reference component changes, at which point it resets to 0.
    ///     </para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <inheritdoc cref="SnowflakeComponent.SnowflakeComponent" path="/exception[@cref='ArgumentOutOfRangeException']"/>
    ///     -or-
    ///     <paramref name="refComponentIndex"/> is negative.
    /// </exception>
    public SequenceSnowflakeComponent(int lengthInBits, int refComponentIndex)
        : base(lengthInBits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(refComponentIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(refComponentIndex, MaxLengthInBits - lengthInBits);

        ReferenceComponentIndex = refComponentIndex;
    }

    /// <summary>Gets the zero-based index of the reference component.</summary>
    public int ReferenceComponentIndex { get; }

    /// <inheritdoc />
    protected override long CalculateValue(SnowflakeGenerationContext ctx)
    {
        var refComponentLastValue = ctx.Components[ReferenceComponentIndex].LastValue;
        if (refComponentLastValue == _refComponentValue)
            return checked(++_seq);

        _seq = 0;
        _refComponentValue = refComponentLastValue;

        return _seq;
    }
}
