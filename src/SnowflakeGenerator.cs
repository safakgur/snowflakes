using System.Diagnostics;
using System.Numerics;
using Snowflakes.Components;
using Lock =
#if NET9_0_OR_GREATER
    System.Threading.Lock;
#else
    System.Object;
#endif

namespace Snowflakes;

/// <summary>Generates 64-bit Snowflake IDs, also known as snowflakes.</summary>
public static class SnowflakeGenerator
{
    /// <summary>Creates a new generator for snowflakes of the specified integer type.</summary>
    /// <typeparam name="T">The snowflake type.</typeparam>
    /// <inheritdoc cref="SnowflakeGenerator{T}.SnowflakeGenerator" />
    public static SnowflakeGenerator<T> Create<T>(params IEnumerable<SnowflakeComponent<T>> components)
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        return new(components);
    }

    /// <summary>Creates a new generator for signed, 64-bit snowflakes.</summary>
    /// <inheritdoc cref="Create{T}" />
    public static SnowflakeGenerator<long> Create(params IEnumerable<SnowflakeComponent<long>> components)
    {
        return Create<long>(components);
    }

    /// <summary>Creates a new generator builder for snowflakes of the specified integer type.</summary>
    /// <typeparam name="T">The snowflake type.</typeparam>
    /// <inheritdoc cref="SnowflakeGeneratorBuilder{T}.SnowflakeGeneratorBuilder" />
    public static SnowflakeGeneratorBuilder<T> CreateBuilder<T>()
        where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
    {
        return new();
    }

    /// <summary>Creates a new generator builder for signed, 64-bit snowflakes.</summary>
    /// <inheritdoc cref="CreateBuilder{T}" />
    public static SnowflakeGeneratorBuilder<long> CreateBuilder()
    {
        return CreateBuilder<long>();
    }
}

/// <summary>Generates 64-bit Snowflake IDs, also known as snowflakes.</summary>
/// <typeparam name="T">The snowflake type.</typeparam>
public sealed class SnowflakeGenerator<T>
    where T : struct, IBinaryInteger<T>, IMinMaxValue<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly Lock _syncRoot = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly (SnowflakeComponent<T> Component, int BitsToShiftLeft)[] _componentsInExecutionOrder;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly SnowflakeGenerationContext<T> _context;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SnowflakeGenerator{T}" /> class with the
    ///     specified components.
    /// </summary>
    /// <param name="components">
    ///     <para>
    ///         The components to produce the parts that will make up the generated snowflakes.
    ///     </para>
    ///     <para>
    ///         The order of the components determine their place in the snowflake, with the bits
    ///         produced by the first component taking the highest (leftmost) location and the bits
    ///         produced by the last component taking the lowest (rightmost) location.
    ///     </para>
    ///     <para>
    ///         Note that an instance of a snowflake component must only be used by a single
    ///         generator, as it may store state that should not be shared.
    ///     </para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="components" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="components" /> is empty, contains a null item, contains duplicate items,
    ///     or the sum of its items' lengths exceeds 63 bits.
    /// </exception>
    internal SnowflakeGenerator(params IEnumerable<SnowflakeComponent<T>> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var inInsertionOrder = components.ToArray();

        if (inInsertionOrder.Length == 0)
            throw new ArgumentException(
                "At least one component is required.", nameof(components));

        _componentsInExecutionOrder = new (SnowflakeComponent<T> Component, int)[inInsertionOrder.Length];

        var set = new HashSet<SnowflakeComponent<T>>();
        var totalLengthInBits = 0;
        var needsSort = false;
        for (var i = inInsertionOrder.Length - 1; i >= 0; i--)
        {
            var component = inInsertionOrder[i]
                ?? throw new ArgumentException(
                    "Component collection cannot contain null items.", nameof(components));

            if (!set.Add(component))
                throw new ArgumentException(
                    "Component collection cannot contain duplicate items.", nameof(components));

            try
            {
                component.Owner = this;
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentException(
                    "Component collection cannot contain another generator's components.", nameof(components), ex);
            }

            _componentsInExecutionOrder[i] = (component, totalLengthInBits);

            totalLengthInBits += component.LengthInBits;

            if (component.ExecutionOrder != 0)
                needsSort = true;
        }

        if (totalLengthInBits > SnowflakeComponent<T>.MaxLengthInBits)
            throw new ArgumentException(
                $"Total number of bits produced by the components cannot exceed {SnowflakeComponent<T>.MaxLengthInBits}.",
                nameof(components));

        if (needsSort)
            Array.Sort(_componentsInExecutionOrder,
                static (a, b) => a.Component.ExecutionOrder.CompareTo(b.Component.ExecutionOrder));

        _context = new(inInsertionOrder);
    }

    /// <summary>
    ///     Gets the components to produce the parts that make up the generated snowflakes.
    /// </summary>
    public ReadOnlySpan<SnowflakeComponent<T>> Components => _context.Components;

    /// <summary>Generates a snowflake with the configured components.</summary>
    /// <returns>A non-negative 64-bit integer, sortable based on the configured components.</returns>
    /// <exception cref="OverflowException">
    ///     One of the components produced a value that exceeded the maximum number of bits it was
    ///     configured to produce.
    /// </exception>
    public T NewSnowflake()
    {
        var result = T.Zero;

        lock (_syncRoot)
            foreach (var (component, bitsToShiftLeft) in _componentsInExecutionOrder)
                result |= component.GetValue(_context) << bitsToShiftLeft;

        return result;
    }
}
