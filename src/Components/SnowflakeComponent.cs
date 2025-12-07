using System.Diagnostics;
using System.Numerics;

namespace Snowflakes.Components;

/// <summary>
///     Provides a base class for components that produce parts that make up snowflakes.
/// </summary>
/// <typeparam name="T">The snowflake type.</typeparam>
/// <remarks>
///     Snowflake components are used by <seealso cref="SnowflakeGenerator{T}" />.
///     <list type="bullet">
///         <item>
///             A component instance should only be supplied to a single generator as components
///             may store state that should not be shared by different generators. An attempt to
///             add the same component to multiple generators will result in an exception.
///         </item>
///         <item>
///             The generator ensures the components are executed in the correct order, which can
///             be leveraged to write relational components.
///         </item>
///         <item>
///             The generator ensures that no component is executed concurrently, so derived
///             components do not need to implement thread safety themselves.
///         </item>
///     </list>
/// </remarks>
public abstract class SnowflakeComponent<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    /// <summary>
    ///     Represents the maximum number of bits that can be stored by an instance of
    ///     type <typeparamref name="T" /> for snowflake generation.
    /// </summary>
    /// <remarks>
    ///     This value is based on the maximum value of the type, e.g., for <see cref="ulong" />, it
    ///     is 64 whereas for <see cref="long" />, it is 63, because the sign bit will not be used.
    /// </remarks>
    protected internal static readonly int MaxLengthInBits = int.CreateChecked(T.PopCount(T.MaxValue));

    /// <summary>
    ///     Represents the number of bytes required to store an instance of type
    ///     <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    ///     This value is based on the total number of bits of the type, including the sign bit.
    ///     It should be used to determine buffer sizes when working with this snowflake type.
    /// </remarks>
    protected internal static readonly int MaxLengthInBytes = int.CreateChecked(T.PopCount(T.AllBitsSet)) / 8;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly T _mask;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private SnowflakeGenerator<T>? _owner;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnowflakeComponent{T}" /> class.
    /// </summary>
    /// <param name="lengthInBits">
    ///     <para>The number of bits this component will produce.</para>
    ///     <para>
    ///         Only the lowest <paramref name="lengthInBits" /> number of bits of the generated
    ///         values are used. Any higher bits, i.e., bits to the left, are discarded.
    ///     </para>
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="lengthInBits" /> is less than 1 or greater than 63.
    /// </exception>
    public SnowflakeComponent(int lengthInBits)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lengthInBits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lengthInBits, MaxLengthInBits);

        _mask = (T.One << lengthInBits) - T.One;

        LengthInBits = lengthInBits;
    }

    /// <summary>Gets the number of bits this component will produce.</summary>
    public int LengthInBits { get; }

    /// <summary>Gets the last value produced by this component.</summary>
    /// <remarks>Calls to <see cref="GetValue" /> update this value.</remarks>
    public T LastValue { get; private set; }

    /// <summary>
    ///     Gets the execution order of this component relative to the other components that
    ///     <seealso cref="SnowflakeGenerator{T}" /> use. If all components have the default order,
    ///     which is 0, they are executed in the order they'll be placed on a snowflake.
    /// </summary>
    /// <remarks>
    ///     Note that this value does not control where the value of each component goes in the
    ///     generated snowflake. It only controls the order each component will be executed.
    ///     So that related components (like a sequence component that needs to read the last value
    ///     produced by a timestamp component) are executed in the correct order.
    /// </remarks>
    public int ExecutionOrder { get; init; }

    /// <summary>Gets a value indicating whether truncation of higher-order bits is allowed.</summary>
    /// <remarks>
    ///     If set to false (default), <see cref="GetValue" /> will throw when the calculated value
    ///     exceeds the number of bits specified by <see cref="LengthInBits" />. If set to true,
    ///     the higher-order bits will be truncated without throwing an exception.
    /// </remarks>
    protected bool AllowTruncation { get; init; }

    /// <summary>Gets or sets the snowflake generator that owns this component.</summary>
    /// <exception cref="ArgumentNullException">Value is null.</exception>
    /// <exception cref="InvalidOperationException">Component already has an owner.</exception>
    internal SnowflakeGenerator<T>? Owner
    {
        get => _owner;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            var original = Interlocked.CompareExchange(ref _owner, value, null);

            if (original is not null && original != value)
                throw new InvalidOperationException("A snowflake component cannot be used by multiple generators.");
        }
    }

    /// <summary>Produces the value that will be placed in a snowflake.</summary>
    /// <param name="ctx">
    ///     Provides information about the current operation and the
    ///     <see cref="SnowflakeGenerator{T}" /> instance executing it.
    /// </param>
    /// <returns>
    ///     The value generated by this component, masked to have only the lowest
    ///     <see cref="LengthInBits" /> number of bits set.
    /// </returns>
    /// <remarks>
    ///     This method will throw if the calculated value exceeds the number of bits specified by
    ///     <see cref="LengthInBits" /> and <see cref="AllowTruncation" /> is false.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="ctx" /> is null.
    /// </exception>
    /// <exception cref="OverflowException">
    ///     The component produced a value that exceeds the number of bits specified by
    ///     <see cref="LengthInBits" />, and <see cref="AllowTruncation" /> was false.
    /// </exception>
    public T GetValue(SnowflakeGenerationContext<T> ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var calculatedValue = CalculateValue(ctx);
        var maskedValue = calculatedValue & _mask;

        if (maskedValue != calculatedValue && !AllowTruncation)
            throw new OverflowException();

        LastValue = maskedValue;

        return maskedValue;
    }

    /// <summary>
    ///     When overridden in a derived class, produces the original value that will be masked,
    ///     saved, and placed in a snowflake.
    /// </summary>
    /// <param name="ctx"><inheritdoc cref="GetValue" path="/param[@name='ctx']" /></param>
    /// <returns>
    ///     The original value generated by this component. It can include more than
    ///     <see cref="LengthInBits" /> number of bits set, which will be ignored by
    ///     the caller.
    /// </returns>
    /// <remarks>
    ///     This method is called by <see cref="GetValue" />, which masks it to have only the lowest
    ///     <see cref="LengthInBits" /> number of bits set, and updates <see cref="LastValue" />
    ///     with it.
    /// </remarks>
    protected abstract T CalculateValue(SnowflakeGenerationContext<T> ctx);
}
